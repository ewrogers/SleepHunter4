using SleepHunter.Macro;
using SleepHunter.Models;
using SleepHunter.ViewModels;

namespace SleepHunter.Tests.ViewModels;

public sealed class MacroEditorViewModelTests
{
    [Test]
    public void ShouldExposeObservableQueuesAndToolkitCommands()
    {
        using var player = CreatePlayer();
        var configuration = new PlayerMacroConfiguration(player);
        using var viewModel =
            new MacroEditorViewModel(configuration);
        var spell = new SpellQueueItem { Name = "Spell" };
        var flower = Flower();
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged +=
            (_, args) => changedProperties.Add(args.PropertyName);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasSpells, Is.False);
            Assert.That(viewModel.HasFlowers, Is.False);
            Assert.That(
                viewModel.ClearSpellsCommand.CanExecute(null),
                Is.False);
            Assert.That(
                viewModel.ClearFlowersCommand.CanExecute(null),
                Is.False);
        });

        configuration.AddToSpellQueue(spell);
        configuration.AddToFlowerQueue(flower);
        viewModel.SelectedSpell = spell;
        viewModel.SelectedFlower = flower;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasSpells, Is.True);
            Assert.That(viewModel.HasFlowers, Is.True);
            Assert.That(
                viewModel.RemoveSelectedSpellCommand.CanExecute(null),
                Is.True);
            Assert.That(
                viewModel.RemoveSelectedFlowerCommand.CanExecute(null),
                Is.True);
            Assert.That(
                changedProperties,
                Does.Contain(nameof(MacroEditorViewModel.HasSpells)));
            Assert.That(
                changedProperties,
                Does.Contain(nameof(MacroEditorViewModel.HasFlowers)));
        });

        viewModel.RemoveSelectedSpellCommand.Execute(null);
        viewModel.RemoveSelectedFlowerCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(configuration.QueuedSpells, Is.Empty);
            Assert.That(configuration.FlowerTargets, Is.Empty);
            Assert.That(viewModel.SelectedSpell, Is.Null);
            Assert.That(viewModel.SelectedFlower, Is.Null);
            Assert.That(
                viewModel.RemoveSelectedSpellCommand.CanExecute(null),
                Is.False);
            Assert.That(
                viewModel.RemoveSelectedFlowerCommand.CanExecute(null),
                Is.False);
        });
    }

    [Test]
    public void ShouldHonorEditingStateForClearAndMoveCommands()
    {
        using var player = CreatePlayer();
        var configuration = new PlayerMacroConfiguration(player);
        var canEdit = false;
        using var viewModel =
            new MacroEditorViewModel(
                configuration,
                () => canEdit);
        var firstSpell = new SpellQueueItem { Name = "First" };
        var secondSpell = new SpellQueueItem { Name = "Second" };
        var firstFlower = Flower();
        var secondFlower = Flower();
        configuration.AddToSpellQueue(firstSpell);
        configuration.AddToSpellQueue(secondSpell);
        configuration.AddToFlowerQueue(firstFlower);
        configuration.AddToFlowerQueue(secondFlower);
        viewModel.SelectedSpell = firstSpell;
        viewModel.SelectedFlower = firstFlower;

        Assert.Multiple(() =>
        {
            Assert.That(
                viewModel.ClearSpellsCommand.CanExecute(null),
                Is.False);
            Assert.That(
                viewModel.ClearFlowersCommand.CanExecute(null),
                Is.False);
            Assert.That(
                viewModel.MoveSpell(firstSpell, secondSpell),
                Is.False);
            Assert.That(
                viewModel.MoveFlower(firstFlower, secondFlower),
                Is.False);
        });

        canEdit = true;
        viewModel.NotifyEditingStateChanged();

        Assert.Multiple(() =>
        {
            Assert.That(
                viewModel.ClearSpellsCommand.CanExecute(null),
                Is.True);
            Assert.That(
                viewModel.ClearFlowersCommand.CanExecute(null),
                Is.True);
            Assert.That(
                viewModel.MoveSpell(firstSpell, secondSpell),
                Is.True);
            Assert.That(
                viewModel.MoveFlower(firstFlower, secondFlower),
                Is.True);
        });

        viewModel.ClearSpellsCommand.Execute(null);
        viewModel.ClearFlowersCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(configuration.QueuedSpells, Is.Empty);
            Assert.That(configuration.FlowerTargets, Is.Empty);
        });
    }

    private static FlowerQueueItem Flower() =>
        new()
        {
            Target = new SpellTarget
            {
                Mode = SpellTargetMode.Self
            }
        };

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
}
