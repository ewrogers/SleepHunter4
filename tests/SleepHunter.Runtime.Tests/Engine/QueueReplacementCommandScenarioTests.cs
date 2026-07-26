using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Tests.Scenarios;

namespace SleepHunter.Runtime.Tests.Engine;

public sealed class QueueReplacementCommandScenarioTests
{
    [Test]
    public void ShouldApplyQueuesAndAutomationInOneRevision()
    {
        var spell = Spell(1, "spell", SpellTarget.Self);
        var skill = Skill(1, "skill");
        var flower = Flower(
            1,
            SpellTarget.Self,
            TimeSpan.FromSeconds(1));
        var configuration = new AutomationConfiguration(
            spellsEnabled: true,
            skillsEnabled: true,
            floweringEnabled: true);
        var command = new ApplyAutomationSetupCommand(
            new ReplaceQueuesCommand(
                [spell],
                SpellQueueRotation.RoundRobin,
                [skill],
                [flower]),
            configuration);
        var scenario = new MacroScenario();
        var startingRevision = scenario.State.Revision;

        var applied = scenario.Send(command);
        var repeated = scenario.Send(command);

        Assert.Multiple(() =>
        {
            Assert.That(
                applied.State.Revision,
                Is.EqualTo(startingRevision + 1));
            Assert.That(
                applied.State.SpellQueue.Entries,
                Is.EqualTo(new[] { spell }));
            Assert.That(
                applied.State.SpellQueue.Rotation,
                Is.EqualTo(SpellQueueRotation.RoundRobin));
            Assert.That(
                applied.State.SkillQueue.Entries,
                Is.EqualTo(new[] { skill }));
            Assert.That(
                applied.State.FlowerQueue.Entries,
                Is.EqualTo(new[] { flower }));
            Assert.That(
                applied.State.Automation,
                Is.EqualTo(configuration));
            Assert.That(repeated.State, Is.SameAs(applied.State));
            Assert.That(repeated.PublishedView, Is.Null);
        });
    }

    [Test]
    public void ShouldReplaceAllQueuesAtomically()
    {
        var oldSpell = Spell(1, "old spell", SpellTarget.Self);
        var oldSkill = Skill(1, "old skill");
        var oldFlower = Flower(
            1,
            SpellTarget.Self,
            TimeSpan.FromSeconds(1));
        var nextSpell = Spell(
            2,
            "next spell",
            SpellTarget.RelativeArea(
                x: 0,
                y: 0,
                innerRadius: 0,
                outerRadius: 1));
        var nextSkill = Skill(2, "next skill");
        var nextFlower = Flower(
            2,
            SpellTarget.RelativeArea(
                x: 1,
                y: 0,
                innerRadius: 0,
                outerRadius: 1),
            TimeSpan.FromSeconds(2));
        var scenario = new MacroScenario();

        scenario.Send(new AddSpellQueueEntryCommand(oldSpell));
        scenario.Send(new AddSkillQueueEntryCommand(oldSkill));
        scenario.Send(new AddFlowerQueueEntryCommand(oldFlower));
        scenario.AdvanceBy(TimeSpan.FromMilliseconds(500));
        var startingRevision = scenario.State.Revision;
        var command = new ReplaceQueuesCommand(
            [nextSpell],
            SpellQueueRotation.RoundRobin,
            [nextSkill],
            [nextFlower]);

        var replaced = scenario.Send(command);
        var repeated = scenario.Send(command);

        Assert.Multiple(() =>
        {
            Assert.That(
                replaced.State.Revision,
                Is.EqualTo(startingRevision + 1));
            Assert.That(
                replaced.State.SpellQueue.Entries,
                Is.EqualTo(new[] { nextSpell }));
            Assert.That(
                replaced.State.SpellQueue.Rotation,
                Is.EqualTo(SpellQueueRotation.RoundRobin));
            Assert.That(replaced.State.SpellQueue.Cursor, Is.Zero);
            Assert.That(
                replaced.State.SkillQueue.Entries,
                Is.EqualTo(new[] { nextSkill }));
            Assert.That(replaced.State.SkillQueue.Cursor, Is.Zero);
            Assert.That(
                replaced.State.FlowerQueue.Entries,
                Is.EqualTo(new[] { nextFlower }));
            Assert.That(replaced.State.FlowerQueue.Cursor, Is.Zero);
            Assert.That(
                replaced.State.FlowerSchedules.GetReadyAt(oldFlower.Id),
                Is.Null);
            Assert.That(
                replaced.State.FlowerSchedules.GetReadyAt(nextFlower.Id),
                Is.EqualTo(
                    scenario.CurrentTime.Add(TimeSpan.FromSeconds(2))));
            Assert.That(
                replaced.State.SpellTargetRotations.Count,
                Is.EqualTo(1));
            Assert.That(
                replaced.State.FlowerTargetRotations.Count,
                Is.EqualTo(1));
            Assert.That(repeated.State, Is.SameAs(replaced.State));
            Assert.That(repeated.PublishedView, Is.Null);
        });
    }

    [Test]
    public void ShouldReplaceEachQueueInOneRevision()
    {
        var oldSpell = Spell(1, "old spell", SpellTarget.Self);
        var oldSkill = Skill(1, "old skill");
        var oldFlower = Flower(
            1,
            SpellTarget.Self,
            TimeSpan.FromSeconds(1));
        var nextSpell = Spell(
            2,
            "next spell",
            SpellTarget.RelativeArea(
                x: 0,
                y: 0,
                innerRadius: 0,
                outerRadius: 1));
        var nextSkill = Skill(2, "next skill");
        var nextFlower = Flower(
            2,
            SpellTarget.RelativeArea(
                x: 1,
                y: 0,
                innerRadius: 0,
                outerRadius: 1),
            TimeSpan.FromSeconds(2));
        var scenario = new MacroScenario();

        scenario.Send(new AddSpellQueueEntryCommand(oldSpell));
        scenario.Send(new AddSkillQueueEntryCommand(oldSkill));
        scenario.Send(new AddFlowerQueueEntryCommand(oldFlower));
        scenario.AdvanceBy(TimeSpan.FromMilliseconds(500));
        var startingRevision = scenario.State.Revision;

        var spells = scenario.Send(
            new ReplaceSpellQueueCommand(
                [nextSpell],
                SpellQueueRotation.RoundRobin));
        var skills = scenario.Send(
            new ReplaceSkillQueueCommand([nextSkill]));
        var flowers = scenario.Send(
            new ReplaceFlowerQueueCommand([nextFlower]));

        Assert.Multiple(() =>
        {
            Assert.That(
                spells.State.Revision,
                Is.EqualTo(startingRevision + 1));
            Assert.That(
                spells.State.SpellQueue.Entries,
                Is.EqualTo(new[] { nextSpell }));
            Assert.That(
                spells.State.SpellQueue.Rotation,
                Is.EqualTo(SpellQueueRotation.RoundRobin));
            Assert.That(spells.State.SpellQueue.Cursor, Is.Zero);
            Assert.That(spells.State.SpellTargetRotations.Count, Is.EqualTo(1));
            Assert.That(
                skills.State.Revision,
                Is.EqualTo(startingRevision + 2));
            Assert.That(
                skills.State.SkillQueue.Entries,
                Is.EqualTo(new[] { nextSkill }));
            Assert.That(skills.State.SkillQueue.Cursor, Is.Zero);
            Assert.That(
                flowers.State.Revision,
                Is.EqualTo(startingRevision + 3));
            Assert.That(
                flowers.State.FlowerQueue.Entries,
                Is.EqualTo(new[] { nextFlower }));
            Assert.That(flowers.State.FlowerQueue.Cursor, Is.Zero);
            Assert.That(
                flowers.State.FlowerSchedules.GetReadyAt(oldFlower.Id),
                Is.Null);
            Assert.That(
                flowers.State.FlowerSchedules.GetReadyAt(nextFlower.Id),
                Is.EqualTo(
                    scenario.CurrentTime.Add(TimeSpan.FromSeconds(2))));
            Assert.That(
                flowers.State.FlowerTargetRotations.Count,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void ShouldIgnoreEquivalentQueueReplacements()
    {
        var spell = Spell(1, "spell", SpellTarget.Self);
        var skill = Skill(1, "skill");
        var flower = Flower(
            1,
            SpellTarget.Self,
            TimeSpan.FromSeconds(1));
        var scenario = new MacroScenario();

        scenario.Send(
            new ReplaceSpellQueueCommand(
                [spell],
                SpellQueueRotation.Priority));
        scenario.Send(new ReplaceSkillQueueCommand([skill]));
        scenario.Send(new ReplaceFlowerQueueCommand([flower]));
        var acceptedState = scenario.State;

        var spells = scenario.Send(
            new ReplaceSpellQueueCommand(
                [spell],
                SpellQueueRotation.Priority));
        var skills = scenario.Send(
            new ReplaceSkillQueueCommand([skill]));
        var flowers = scenario.Send(
            new ReplaceFlowerQueueCommand([flower]));

        Assert.Multiple(() =>
        {
            Assert.That(spells.State, Is.SameAs(acceptedState));
            Assert.That(skills.State, Is.SameAs(acceptedState));
            Assert.That(flowers.State, Is.SameAs(acceptedState));
            Assert.That(spells.PublishedView, Is.Null);
            Assert.That(skills.PublishedView, Is.Null);
            Assert.That(flowers.PublishedView, Is.Null);
        });
    }

    [Test]
    public void ShouldSnapshotReplacementInputsAndRejectInvalidQueues()
    {
        var source = new List<SpellQueueEntry>
        {
            Spell(1, "spell", SpellTarget.Self)
        };
        var command = new ReplaceSpellQueueCommand(
            source,
            SpellQueueRotation.Sequential);
        source.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(command.Queue.Entries, Has.Length.EqualTo(1));
            Assert.That(
                () => new ReplaceSpellQueueCommand(
                    null!,
                    SpellQueueRotation.Priority),
                Throws.ArgumentNullException);
            Assert.That(
                () => new ReplaceSpellQueueCommand(
                    [],
                    (SpellQueueRotation)int.MaxValue),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => new ReplaceSpellQueueCommand(
                    [
                        Spell(1, "first", SpellTarget.Self),
                        Spell(1, "duplicate", SpellTarget.Self)
                    ],
                    SpellQueueRotation.Priority),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => new ReplaceSkillQueueCommand(
                    [
                        Skill(1, "same"),
                        Skill(2, "SAME")
                    ]),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => new ReplaceFlowerQueueCommand([null!]),
                Throws.TypeOf<ArgumentException>());
        });
    }

    private static SpellQueueEntry Spell(
        long id,
        string name,
        SpellTarget target) =>
        new(new SpellQueueEntryId(id), name, target: target);

    private static SkillQueueEntry Skill(long id, string name) =>
        new(new SkillQueueEntryId(id), name);

    private static FlowerQueueEntry Flower(
        long id,
        SpellTarget target,
        TimeSpan interval) =>
        new(new FlowerQueueEntryId(id), target, interval);
}
