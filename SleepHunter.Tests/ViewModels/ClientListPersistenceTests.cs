using System.Collections.Immutable;
using SleepHunter.Macro;
using SleepHunter.Models;
using SleepHunter.Persistence.Configuration;
using SleepHunter.Persistence.Serialization;
using SleepHunter.Services.Configuration;
using SleepHunter.Tests.Support;
using SleepHunter.ViewModels;

namespace SleepHunter.Tests.ViewModels;

public sealed class ClientListPersistenceTests
{
    [Test]
    public async Task ShouldLoadThroughToolkitCommandAndShowImportedQueue()
    {
        using var player = CreatePlayer();
        var editable = new PlayerMacroConfiguration(player);
        var persistence = new StubPersistenceService
        {
            OnLoad = configuration =>
            {
                configuration.AddToSpellQueue(
                    new SpellQueueItem
                    {
                        Name = "Loaded Spell"
                    });
            }
        };
        var interaction = new StubInteraction
        {
            LoadPath = @"C:\Macros\Loaded.sh4x"
        };
        using var clients = CreateViewModel(
            editable,
            persistence,
            interaction);
        clients.Refresh([player], _ => null);
        clients.SelectedClient = clients.Clients.Single();

        await clients.MacroPersistence
            .LoadMacroCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(
                persistence.LoadPaths,
                Is.EqualTo(new[] { interaction.LoadPath }));
            Assert.That(
                persistence.LoadConfigurations.Single(),
                Is.SameAs(editable));
            Assert.That(
                clients.MacroPersistence.IsSpellQueueVisible,
                Is.True);
            Assert.That(
                clients.MacroPersistence.MacroContentColumnSpan,
                Is.EqualTo(1));
            Assert.That(
                clients.MacroPersistence.LastError,
                Is.Null);
        });
    }

    [Test]
    public async Task ShouldSaveThroughToolkitCommand()
    {
        using var player = CreatePlayer();
        var editable = new PlayerMacroConfiguration(player);
        var persistence = new StubPersistenceService();
        var interaction = new StubInteraction
        {
            SavePath = @"C:\Macros\Saved.sh4x"
        };
        using var clients = CreateViewModel(
            editable,
            persistence,
            interaction);
        clients.Refresh([player], _ => null);
        clients.SelectedClient = clients.Clients.Single();

        await clients.MacroPersistence
            .SaveMacroCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(
                persistence.SavePaths,
                Is.EqualTo(new[] { interaction.SavePath }));
            Assert.That(
                persistence.SaveConfigurations.Single(),
                Is.SameAs(editable));
            Assert.That(
                clients.MacroPersistence.LastError,
                Is.Null);
        });
    }

    [Test]
    public async Task ShouldReportPersistenceFailureWithoutEscapingCommand()
    {
        using var player = CreatePlayer();
        var editable = new PlayerMacroConfiguration(player);
        var failure = new IOException("Failed");
        var persistence = new StubPersistenceService
        {
            LoadException = failure
        };
        var interaction = new StubInteraction
        {
            LoadPath = @"C:\Macros\Broken.sh4x"
        };
        using var clients = CreateViewModel(
            editable,
            persistence,
            interaction);
        clients.Refresh([player], _ => null);
        clients.SelectedClient = clients.Clients.Single();

        await clients.MacroPersistence
            .LoadMacroCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(
                clients.MacroPersistence.LastError,
                Is.SameAs(failure));
            Assert.That(interaction.Messages, Has.Count.EqualTo(1));
            Assert.That(
                interaction.Messages.Single().Title,
                Is.EqualTo("Failed to Load Macro"));
            Assert.That(
                clients.MacroPersistence.IsRunning,
                Is.False);
        });
    }

    private static ClientListViewModel CreateViewModel(
        PlayerMacroConfiguration configuration,
        IMacroConfigurationPersistenceService persistence,
        IMacroConfigurationInteraction interaction) =>
        new(
            (player, runtime) =>
                new ClientListItemViewModel(
                    player,
                    configuration,
                    runtime,
                    configurationMapper: null,
                    setupFactory: null,
                    getSettings: null),
            persistence,
            interaction,
            new TestLogger());

    private static Player CreatePlayer() =>
        new(
            new ClientProcess
            {
                ProcessId = Environment.ProcessId,
                WindowHandle = new nint(1),
                WindowTitle = "Test Window"
            })
        {
            Name = "Test",
            IsLoggedIn = true
        };

    private sealed class StubPersistenceService :
        IMacroConfigurationPersistenceService
    {
        public Action<PlayerMacroConfiguration>? OnLoad { get; init; }

        public Exception? LoadException { get; init; }

        public List<PlayerMacroConfiguration>
            LoadConfigurations
        { get; } = [];

        public List<string> LoadPaths { get; } = [];

        public List<PlayerMacroConfiguration>
            SaveConfigurations
        { get; } = [];

        public List<string> SavePaths { get; } = [];

        public Task<MacroConfigurationApplyResult> LoadAsync(
            PlayerMacroConfiguration configuration,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            if (LoadException is not null)
            {
                return Task.FromException<
                    MacroConfigurationApplyResult>(
                    LoadException);
            }

            LoadConfigurations.Add(configuration);
            LoadPaths.Add(filePath);
            OnLoad?.Invoke(configuration);
            return Task.FromResult(
                new MacroConfigurationApplyResult(
                    CreateLoadResult(),
                    HotkeyRegistrationFailed: false));
        }

        public Task SaveAsync(
            PlayerMacroConfiguration configuration,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            SaveConfigurations.Add(configuration);
            SavePaths.Add(filePath);
            return Task.CompletedTask;
        }

        public Task<MacroConfigurationAutoLoadResult> AutoLoadAsync(
            PlayerMacroConfiguration configuration,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<MacroConfigurationAutoLoadResult>(null!);

        public Task AutoSaveAsync(
            PlayerMacroConfiguration configuration,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        private static MacroConfigurationLoadResult
            CreateLoadResult() =>
            new(
                MacroConfiguration.Empty,
                MacroConfigurationFormat.Current,
                MacroConfigurationSerializer.CurrentVersion,
                ImmutableArray<MacroConfigurationWarning>.Empty);
    }

    private sealed class StubInteraction :
        IMacroConfigurationInteraction
    {
        public string? LoadPath { get; init; }

        public string? SavePath { get; init; }

        public List<Message> Messages { get; } = [];

        public string SelectLoadFile(string characterName) =>
            LoadPath!;

        public string SelectSaveFile(string characterName) =>
            SavePath!;

        public void ShowMessage(
            string title,
            string message,
            string detail) =>
            Messages.Add(new Message(title, message, detail));
    }

    private sealed record Message(
        string Title,
        string Text,
        string Detail);
}
