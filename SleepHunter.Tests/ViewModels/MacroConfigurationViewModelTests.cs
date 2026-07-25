using System.Collections.Immutable;
using SleepHunter.Macro;
using SleepHunter.Persistence.Configuration;
using SleepHunter.Persistence.Serialization;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Commands;
using SleepHunter.Services.Configuration;
using SleepHunter.ViewModels;

namespace SleepHunter.Tests.ViewModels;

public sealed class MacroConfigurationViewModelTests
{
    [Test]
    public async Task ShouldLoadAndApplyAllQueuesWithOneCommand()
    {
        var skill = new SkillQueueEntry(
            new SkillQueueEntryId(1),
            "Assail");
        var spell = new SpellQueueEntry(
            new SpellQueueEntryId(1),
            "ard cradh",
            target: SpellTarget.Self);
        var flower = new FlowerQueueEntry(
            new FlowerQueueEntryId(1),
            SpellTarget.Self,
            interval: TimeSpan.FromMinutes(1));
        var warning = new MacroConfigurationWarning(
            "legacy.test",
            "A scripted migration warning.");
        var loaded = new MacroConfigurationLoadResult(
            new MacroConfiguration(
                spellRotation: null,
                skills: [skill],
                spells: [spell],
                flowers: [flower]),
            MacroConfigurationFormat.LegacyV4,
            "4.11",
            [warning]);
        var reader = new RecordingReader(loaded);
        var commands = new List<MacroCommand>();
        var viewModel = new MacroConfigurationViewModel(
            reader,
            () => SpellQueueRotation.Sequential,
            (command, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                commands.Add(command);
                return ValueTask.CompletedTask;
            });

        await viewModel.LoadCommand.ExecuteAsync("test.sh4");

        var command = commands.Single() as ReplaceQueuesCommand;
        Assert.Multiple(() =>
        {
            Assert.That(reader.FilePaths, Is.EqualTo(new[] { "test.sh4" }));
            Assert.That(command, Is.Not.Null);
            Assert.That(
                command!.SpellQueue.Entries,
                Is.EqualTo(new[] { spell }));
            Assert.That(
                command.SpellQueue.Rotation,
                Is.EqualTo(SpellQueueRotation.Sequential));
            Assert.That(
                command.SkillQueue.Entries,
                Is.EqualTo(new[] { skill }));
            Assert.That(
                command.FlowerQueue.Entries,
                Is.EqualTo(new[] { flower }));
            Assert.That(viewModel.LatestLoad, Is.SameAs(loaded));
            Assert.That(viewModel.LastError, Is.Null);
            Assert.That(viewModel.HasError, Is.False);
            Assert.That(viewModel.HasWarnings, Is.True);
            Assert.That(viewModel.Warnings, Is.EqualTo(new[] { warning }));
        });
    }

    [Test]
    public async Task ShouldKeepPreviousConfigurationWhenAReadFails()
    {
        var successfulLoad = new MacroConfigurationLoadResult(
            MacroConfiguration.Empty,
            MacroConfigurationFormat.Current,
            MacroConfigurationSerializer.CurrentVersion,
            ImmutableArray<MacroConfigurationWarning>.Empty);
        var expectedError = new MacroConfigurationException(
            "The scripted configuration is invalid.");
        var reader = new RecordingReader(
            successfulLoad,
            expectedError);
        var commands = new List<MacroCommand>();
        var viewModel = new MacroConfigurationViewModel(
            reader,
            () => SpellQueueRotation.Priority,
            (command, _) =>
            {
                commands.Add(command);
                return ValueTask.CompletedTask;
            });

        await viewModel.LoadCommand.ExecuteAsync("valid.shmacro");
        await viewModel.LoadCommand.ExecuteAsync("invalid.shmacro");

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.LatestLoad, Is.SameAs(successfulLoad));
            Assert.That(viewModel.LastError, Is.SameAs(expectedError));
            Assert.That(viewModel.HasError, Is.True);
            Assert.That(commands, Has.Count.EqualTo(1));
        });
    }

    [TestCase(SpellRotationMode.Default, SpellQueueRotation.Priority)]
    [TestCase(SpellRotationMode.None, SpellQueueRotation.Priority)]
    [TestCase(SpellRotationMode.Singular, SpellQueueRotation.Sequential)]
    [TestCase(SpellRotationMode.RoundRobin, SpellQueueRotation.RoundRobin)]
    public void ShouldMapLegacySpellRotations(
        SpellRotationMode legacy,
        SpellQueueRotation expected)
    {
        Assert.That(
            LegacySpellQueueRotationMapper.Map(legacy),
            Is.EqualTo(expected));
    }

    private sealed class RecordingReader : IMacroConfigurationReader
    {
        private readonly Queue<object> results;
        private readonly List<string> filePaths = [];

        public RecordingReader(params object[] results)
        {
            this.results = new Queue<object>(results);
        }

        public IReadOnlyList<string> FilePaths => filePaths;

        public Task<MacroConfigurationLoadResult> LoadAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            filePaths.Add(filePath);
            var result = results.Dequeue();
            return result is Exception exception
                ? Task.FromException<MacroConfigurationLoadResult>(exception)
                : Task.FromResult((MacroConfigurationLoadResult)result);
        }
    }
}
