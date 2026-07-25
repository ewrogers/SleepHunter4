using SleepHunter.Macro;
using SleepHunter.Models;

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
        var addedSpells = new List<SpellQueueItem>();
        var removedSpells = new List<SpellQueueItem>();
        var addedFlowers = new List<FlowerQueueItem>();
        var removedFlowers = new List<FlowerQueueItem>();
        configuration.SpellAdded +=
            (_, args) => addedSpells.Add(args.Spell);
        configuration.SpellRemoved +=
            (_, args) => removedSpells.Add(args.Spell);
        configuration.FlowerTargetAdded +=
            (_, args) => addedFlowers.Add(args.Flower);
        configuration.FlowerTargetRemoved +=
            (_, args) => removedFlowers.Add(args.Flower);

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
                configuration.IsSpellInQueue(" FIRST "),
                Is.True);
            Assert.That(
                addedSpells,
                Is.EqualTo(new[] { firstSpell, secondSpell }));
            Assert.That(
                addedFlowers,
                Is.EqualTo(new[] { flower }));
            Assert.That(
                configuration.RemoveFromSpellQueue(
                    new SpellQueueItem { Name = "missing" }),
                Is.False);
            Assert.That(removedSpells, Is.Empty);
        });

        configuration.ClearSpellQueue();
        configuration.ClearFlowerQueue();

        Assert.Multiple(() =>
        {
            Assert.That(configuration.QueuedSpells, Is.Empty);
            Assert.That(configuration.FlowerTargets, Is.Empty);
            Assert.That(
                removedSpells,
                Is.EqualTo(new[] { secondSpell, firstSpell }));
            Assert.That(
                removedFlowers,
                Is.EqualTo(new[] { flower }));
        });
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
}
