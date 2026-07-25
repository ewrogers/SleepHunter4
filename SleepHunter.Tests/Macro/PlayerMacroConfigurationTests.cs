using System.Collections.Specialized;
using SleepHunter.Macro;
using SleepHunter.Models;
using SleepHunter.Runtime.Automation.Skills;

namespace SleepHunter.Tests.Macro;

public sealed class PlayerMacroConfigurationTests
{
    [Test]
    public void ShouldOwnEditableQueuesWithoutExecutionState()
    {
        using var player = CreatePlayer();
        var configuration = new PlayerMacroConfiguration(player);
        var firstSpell = new SpellQueueItem { Name = "first" };
        var secondSpell = new SpellQueueItem { Name = "second" };
        var flower = new FlowerQueueItem
        {
            Target = new SpellTarget
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
                    new SpellQueueItem { Name = "missing" }),
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
        using var player = CreatePlayer();
        var configuration = new PlayerMacroConfiguration(player);
        var firstSpell = new SpellQueueItem { Name = "first" };
        var secondSpell = new SpellQueueItem { Name = "second" };
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
                    new SpellQueueItem { Name = "missing" },
                    firstSpell),
                Is.False);
        });
    }

    [Test]
    public void ShouldOwnStableSkillConfigurationAndExplicitActiveState()
    {
        using var player = CreatePlayer();
        var configuration = new PlayerMacroConfiguration(player);

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
            Assert.That(
                player.Skillbook.IsActive("Assail"),
                Is.True);
        });
        Assert.That(
            configuration.ToggleSkill("ASSAIL"),
            Is.False);
        Assert.That(configuration.Skills, Is.Empty);
        Assert.That(
            player.Skillbook.IsActive("Assail"),
            Is.False);

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
        Assert.That(
            player.Skillbook.IsActive("Unknown"),
            Is.True);

        configuration.ClearSkills();

        Assert.Multiple(() =>
        {
            Assert.That(configuration.Skills, Is.Empty);
            Assert.That(
                player.Skillbook.IsActive("Unknown"),
                Is.Null);
        });
    }

    [Test]
    public void ShouldHonorExplicitSkillbookActiveState()
    {
        using var player = CreatePlayer();

        player.Skillbook.ToggleActive("Assail", isActive: true);
        player.Skillbook.ToggleActive("Assail", isActive: true);

        Assert.That(player.Skillbook.IsActive("Assail"), Is.True);

        player.Skillbook.ToggleActive("Assail", isActive: false);

        Assert.That(player.Skillbook.IsActive("Assail"), Is.False);
    }

    [Test]
    public void ShouldManageOneConfigurationPerPlayerProcess()
    {
        using var firstPlayer = CreatePlayer();
        using var replacementPlayer = CreatePlayer();
        var manager = new PlayerMacroConfigurationManager();

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

    private static FlowerQueueItem Flower() =>
        new()
        {
            Target = new SpellTarget
            {
                Mode = SpellTargetMode.Self
            }
        };
}
