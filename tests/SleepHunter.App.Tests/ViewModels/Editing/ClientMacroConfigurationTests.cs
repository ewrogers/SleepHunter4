using System.Collections.Specialized;
using SleepHunter.Models;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Services.Configuration;
using SleepHunter.ViewModels.Editing;

namespace SleepHunter.Tests.ViewModels.Editing;

public sealed class ClientMacroConfigurationTests
{
    [Test]
    public void ShouldOwnEditableQueuesWithoutExecutionState()
    {
        var player = CreatePlayer();
        var configuration = new ClientMacroConfiguration(player);
        var firstSpell = new SpellQueueItemViewModel { Name = "first" };
        var secondSpell = new SpellQueueItemViewModel { Name = "second" };
        var flower = new FlowerQueueItemViewModel
        {
            Target = new SpellTargetViewModel
            {
                Mode = SpellTargetMode.Self
            }
        };
        var spellChanges =
            new List<NotifyCollectionChangedAction>();
        var flowerChanges =
            new List<NotifyCollectionChangedAction>();
        ((INotifyCollectionChanged)configuration.QueuedSpells)
            .CollectionChanged +=
            (_, args) => spellChanges.Add(args.Action);
        ((INotifyCollectionChanged)configuration.FlowerTargets)
            .CollectionChanged +=
            (_, args) => flowerChanges.Add(args.Action);

        configuration.AddToSpellQueue(firstSpell);
        configuration.AddToSpellQueue(secondSpell, index: 0);
        configuration.AddToFlowerQueue(flower);
        var spellSnapshot = configuration.GetSpellQueueSnapshot();
        var flowerSnapshot = configuration.GetFlowerQueueSnapshot();
        spellSnapshot.Clear();
        flowerSnapshot.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(
                configuration.QueuedSpells,
                Is.EqualTo(new[] { secondSpell, firstSpell }));
            Assert.That(
                configuration.FlowerTargets,
                Is.EqualTo(new[] { flower }));
            Assert.That(
                configuration.QueuedSpells.Select(
                    spell => spell.Id),
                Is.EqualTo(new long[] { 2, 1 }));
            Assert.That(flower.Id, Is.EqualTo(1));
            Assert.That(
                configuration.IsSpellInQueue(" FIRST "),
                Is.True);
            Assert.That(
                spellChanges,
                Is.EqualTo(
                    new[]
                    {
                        NotifyCollectionChangedAction.Add,
                        NotifyCollectionChangedAction.Add
                    }));
            Assert.That(
                flowerChanges,
                Is.EqualTo(
                    new[] { NotifyCollectionChangedAction.Add }));
            Assert.That(
                configuration.RemoveFromSpellQueue(
                    new SpellQueueItemViewModel { Name = "missing" }),
                Is.False);
        });

        configuration.ClearSpellQueue();
        configuration.ClearFlowerQueue();

        Assert.Multiple(() =>
        {
            Assert.That(configuration.QueuedSpells, Is.Empty);
            Assert.That(configuration.FlowerTargets, Is.Empty);
            Assert.That(
                spellChanges.Last(),
                Is.EqualTo(NotifyCollectionChangedAction.Reset));
            Assert.That(
                flowerChanges.Last(),
                Is.EqualTo(NotifyCollectionChangedAction.Reset));
        });
    }

    [Test]
    public void ShouldMoveObservableQueueEntriesByDropTarget()
    {
        var player = CreatePlayer();
        var configuration = new ClientMacroConfiguration(player);
        var firstSpell = new SpellQueueItemViewModel { Name = "first" };
        var secondSpell = new SpellQueueItemViewModel { Name = "second" };
        var firstFlower = Flower();
        var secondFlower = Flower();
        configuration.AddToSpellQueue(firstSpell);
        configuration.AddToSpellQueue(secondSpell);
        configuration.AddToFlowerQueue(firstFlower);
        configuration.AddToFlowerQueue(secondFlower);
        var spellActions =
            new List<NotifyCollectionChangedAction>();
        var flowerActions =
            new List<NotifyCollectionChangedAction>();
        ((INotifyCollectionChanged)configuration.QueuedSpells)
            .CollectionChanged +=
            (_, args) => spellActions.Add(args.Action);
        ((INotifyCollectionChanged)configuration.FlowerTargets)
            .CollectionChanged +=
            (_, args) => flowerActions.Add(args.Action);

        var movedSpell =
            configuration.MoveSpell(firstSpell, secondSpell);
        var movedFlower =
            configuration.MoveFlower(secondFlower, firstFlower);

        Assert.Multiple(() =>
        {
            Assert.That(movedSpell, Is.True);
            Assert.That(
                configuration.QueuedSpells,
                Is.EqualTo(new[] { secondSpell, firstSpell }));
            Assert.That(
                spellActions,
                Is.EqualTo(
                    new[] { NotifyCollectionChangedAction.Move }));
            Assert.That(movedFlower, Is.True);
            Assert.That(
                configuration.FlowerTargets,
                Is.EqualTo(new[] { secondFlower, firstFlower }));
            Assert.That(
                flowerActions,
                Is.EqualTo(
                    new[] { NotifyCollectionChangedAction.Move }));
            Assert.That(
                configuration.MoveSpell(
                    new SpellQueueItemViewModel { Name = "missing" },
                    firstSpell),
                Is.False);
        });
    }

    [Test]
    public void ShouldPublishEditedQueueEntriesAsConfigurationChanges()
    {
        var player = CreatePlayer();
        var configuration = new ClientMacroConfiguration(player);
        var spell = new SpellQueueItemViewModel { Name = "first" };
        var flower = Flower();
        configuration.AddToSpellQueue(spell);
        configuration.AddToFlowerQueue(flower);
        var changedProperties = new List<string>();
        configuration.PropertyChanged +=
            (_, args) =>
            {
                if (args.PropertyName is { } propertyName)
                    changedProperties.Add(propertyName);
            };

        var spellUpdated = configuration.UpdateSpell(
            spell,
            new SpellQueueItemViewModel
            {
                Id = spell.Id,
                Name = "updated",
                TargetLevel = 75
            });
        var flowerUpdated = configuration.UpdateFlower(
            flower,
            new FlowerQueueItemViewModel
            {
                Id = flower.Id,
                Target = new SpellTargetViewModel
                {
                    Mode = SpellTargetMode.Character,
                    CharacterName = "Target"
                },
                Interval = TimeSpan.FromSeconds(5)
            });

        Assert.Multiple(() =>
        {
            Assert.That(spellUpdated, Is.True);
            Assert.That(spell.Name, Is.EqualTo("updated"));
            Assert.That(spell.TargetLevel, Is.EqualTo(75));
            Assert.That(flowerUpdated, Is.True);
            Assert.That(
                flower.Target.CharacterName,
                Is.EqualTo("Target"));
            Assert.That(
                flower.Interval,
                Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.That(
                changedProperties,
                Is.EqualTo(
                    new[]
                    {
                        nameof(
                            ClientMacroConfiguration.QueuedSpells),
                        nameof(
                            ClientMacroConfiguration.FlowerTargets)
                    }));
        });
    }

    [Test]
    public void ShouldOwnStableSkillConfigurationAndExplicitActiveState()
    {
        var player = CreatePlayer();
        var configuration = new ClientMacroConfiguration(player);

        Assert.That(
            configuration.ToggleSkill("Assail"),
            Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(
                configuration.Skills.Single().Name,
                Is.EqualTo("Assail"));
            Assert.That(
                configuration.Skills.Single().Id.Value,
                Is.EqualTo(1));
        });
        Assert.That(
            configuration.ToggleSkill("ASSAIL"),
            Is.False);
        Assert.That(configuration.Skills, Is.Empty);

        configuration.ReplaceSkills(
        [
            new SkillQueueEntry(
                new SkillQueueEntryId(41),
                "Unknown")
        ]);
        var snapshot = configuration.GetSkillQueueSnapshot();
        snapshot.Clear();

        Assert.That(
            configuration.Skills.Single().Id.Value,
            Is.EqualTo(41));

        configuration.ClearSkills();

        Assert.That(configuration.Skills, Is.Empty);
    }

    [Test]
    public void ShouldManageOneConfigurationPerPlayerProcess()
    {
        var firstPlayer = CreatePlayer();
        var replacementPlayer = CreatePlayer();
        var manager = new ClientMacroConfigurationRegistry();

        var first = manager.GetOrCreate(firstPlayer);
        var same = manager.GetOrCreate(firstPlayer);

        Assert.Multiple(() =>
        {
            Assert.That(same, Is.SameAs(first));
            Assert.That(
                manager.Configurations,
                Is.EqualTo(new[] { first }));
            Assert.Throws<InvalidOperationException>(
                () => manager.GetOrCreate(replacementPlayer));
        });

        Assert.That(
            manager.Remove(firstPlayer.Process.ProcessId),
            Is.True);
        var replacement = manager.GetOrCreate(replacementPlayer);

        Assert.Multiple(() =>
        {
            Assert.That(replacement, Is.Not.SameAs(first));
            Assert.That(replacement.Client, Is.SameAs(replacementPlayer));
        });
    }

    private static ClientSession CreatePlayer() =>
        new(
            new ClientProcess
            {
                ProcessId = Environment.ProcessId,
                WindowHandle = new nint(1),
                WindowTitle = "Test Window"
            })
        {
            Name = "Test"
        };

    private static FlowerQueueItemViewModel Flower() =>
        new()
        {
            Target = new SpellTargetViewModel
            {
                Mode = SpellTargetMode.Self
            }
        };
}
