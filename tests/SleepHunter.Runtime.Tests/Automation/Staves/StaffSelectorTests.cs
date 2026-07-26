using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Tests.Automation.Staves;

public sealed class StaffSelectorTests
{
    [Test]
    public void ShouldExcludeForeignClassStaff()
    {
        var priestStaff = Candidate(
            "priest",
            CharacterClass.Priest,
            castLines: 0);
        var wizardStaff = Candidate(
            "wizard",
            CharacterClass.Wizard,
            castLines: 1);
        var neutralStaff = Candidate(
            "neutral",
            requiredClass: null,
            castLines: 2);
        var request = Request(
            CharacterClass.Wizard,
            baseCastLines: 4,
            inventory:
            [
                new InventoryItemSnapshot(1, priestStaff.Name),
                new InventoryItemSnapshot(2, wizardStaff.Name),
                new InventoryItemSnapshot(3, neutralStaff.Name)
            ],
            candidates: [priestStaff, wizardStaff, neutralStaff]);

        var selection = StaffSelector.Select(request);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Action, Is.EqualTo(StaffSelectionAction.Equip));
            Assert.That(selection.Staff, Is.EqualTo(wizardStaff));
            Assert.That(selection.InventorySlot, Is.EqualTo(2));
            Assert.That(selection.CastLines, Is.EqualTo(1));
        });
    }

    [Test]
    public void ShouldAllowOnlyNeutralStaffWhenCharacterClassIsUnknown()
    {
        var priestStaff = Candidate(
            "priest",
            CharacterClass.Priest,
            castLines: 0);
        var wizardStaff = Candidate(
            "wizard",
            CharacterClass.Wizard,
            castLines: 1);
        var neutralStaff = Candidate(
            "neutral",
            requiredClass: null,
            castLines: 2);
        var request = Request(
            CharacterClass.Unknown,
            baseCastLines: 4,
            inventory:
            [
                new InventoryItemSnapshot(1, priestStaff.Name),
                new InventoryItemSnapshot(2, wizardStaff.Name),
                new InventoryItemSnapshot(3, neutralStaff.Name)
            ],
            candidates: [priestStaff, wizardStaff, neutralStaff]);

        var selection = StaffSelector.Select(request);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Action, Is.EqualTo(StaffSelectionAction.Equip));
            Assert.That(selection.Staff, Is.EqualTo(neutralStaff));
            Assert.That(selection.InventorySlot, Is.EqualTo(3));
            Assert.That(selection.CastLines, Is.EqualTo(2));
        });
    }

    [Test]
    public void ShouldNotSelectClassSpecificStaffWhenCharacterClassIsUnknown()
    {
        var priestStaff = Candidate(
            "priest",
            CharacterClass.Priest,
            castLines: 0);
        var wizardStaff = Candidate(
            "wizard",
            CharacterClass.Wizard,
            castLines: 1);
        var request = Request(
            CharacterClass.Unknown,
            baseCastLines: 4,
            inventory:
            [
                new InventoryItemSnapshot(1, priestStaff.Name),
                new InventoryItemSnapshot(2, wizardStaff.Name)
            ],
            candidates: [priestStaff, wizardStaff]);

        var selection = StaffSelector.Select(request);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Action, Is.EqualTo(StaffSelectionAction.None));
            Assert.That(
                selection.Reason,
                Is.EqualTo(StaffSelectionReason.NoEligibleStaff));
            Assert.That(selection.Staff, Is.Null);
            Assert.That(selection.InventorySlot, Is.Null);
            Assert.That(selection.CastLines, Is.EqualTo(4));
        });
    }

    [Test]
    public void ShouldEnforceLevelAndAbilityRequirements()
    {
        var levelLocked = Candidate(
            "level-locked",
            CharacterClass.Wizard,
            castLines: 0,
            requiredLevel: 51);
        var abilityLocked = Candidate(
            "ability-locked",
            CharacterClass.Wizard,
            castLines: 0,
            requiredAbilityLevel: 11);
        var eligible = Candidate(
            "eligible",
            CharacterClass.Wizard,
            castLines: 2,
            requiredLevel: 50,
            requiredAbilityLevel: 10);
        var request = Request(
            CharacterClass.Wizard,
            baseCastLines: 4,
            inventory:
            [
                new InventoryItemSnapshot(1, levelLocked.Name),
                new InventoryItemSnapshot(2, abilityLocked.Name),
                new InventoryItemSnapshot(3, eligible.Name)
            ],
            candidates: [levelLocked, abilityLocked, eligible],
            level: 50,
            abilityLevel: 10);

        var selection = StaffSelector.Select(request);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Staff, Is.EqualTo(eligible));
            Assert.That(selection.InventorySlot, Is.EqualTo(3));
        });
    }

    [Test]
    public void ShouldUseAbilityRequirementInsteadOfNormalLevelForAbStaff()
    {
        var abilityStaff = Candidate(
            "ability-staff",
            CharacterClass.Wizard,
            castLines: 1,
            requiredLevel: 99,
            requiredAbilityLevel: 10);
        var request = Request(
            CharacterClass.Wizard,
            baseCastLines: 4,
            inventory:
            [
                new InventoryItemSnapshot(1, abilityStaff.Name)
            ],
            candidates: [abilityStaff],
            level: 50,
            abilityLevel: 10);

        var selection = StaffSelector.Select(request);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Action, Is.EqualTo(StaffSelectionAction.Equip));
            Assert.That(selection.Staff, Is.EqualTo(abilityStaff));
        });
    }

    [Test]
    public void ShouldChooseHighestLevelStaffForEqualImprovement()
    {
        var lower = Candidate(
            "lower",
            CharacterClass.Wizard,
            castLines: 1,
            requiredLevel: 30);
        var higher = Candidate(
            "higher",
            CharacterClass.Wizard,
            castLines: 1,
            requiredLevel: 50);
        var request = Request(
            CharacterClass.Wizard,
            baseCastLines: 4,
            inventory:
            [
                new InventoryItemSnapshot(1, lower.Name),
                new InventoryItemSnapshot(2, higher.Name)
            ],
            candidates: [lower, higher],
            level: 50);

        var selection = StaffSelector.Select(request);

        Assert.That(selection.Staff, Is.EqualTo(higher));
    }

    [Test]
    public void ShouldChooseHighestAbilityStaffForEqualImprovement()
    {
        var regular = Candidate(
            "regular",
            CharacterClass.Priest,
            castLines: 1,
            requiredLevel: 99);
        var lowerAbility = Candidate(
            "lower-ability",
            CharacterClass.Priest,
            castLines: 1,
            requiredAbilityLevel: 10);
        var higherAbility = Candidate(
            "higher-ability",
            CharacterClass.Priest,
            castLines: 1,
            requiredAbilityLevel: 50);
        var request = Request(
            CharacterClass.Priest,
            baseCastLines: 4,
            inventory:
            [
                new InventoryItemSnapshot(1, regular.Name),
                new InventoryItemSnapshot(2, lowerAbility.Name),
                new InventoryItemSnapshot(3, higherAbility.Name)
            ],
            candidates: [regular, lowerAbility, higherAbility],
            level: 99,
            abilityLevel: 50);

        var selection = StaffSelector.Select(request);

        Assert.That(selection.Staff, Is.EqualTo(higherAbility));
    }

    [Test]
    public void ShouldConsiderOnlyAvailableStaff()
    {
        var unavailable = Candidate(
            "unavailable",
            CharacterClass.Wizard,
            castLines: 0);
        var available = Candidate(
            "available",
            CharacterClass.Wizard,
            castLines: 2);
        var request = Request(
            CharacterClass.Wizard,
            baseCastLines: 4,
            inventory:
            [
                new InventoryItemSnapshot(5, available.Name)
            ],
            candidates: [unavailable, available]);

        var selection = StaffSelector.Select(request);

        Assert.That(selection.Staff, Is.EqualTo(available));
    }

    [Test]
    public void ShouldKeepEquippedStaffWhenItTiesForBest()
    {
        var equipped = Candidate(
            "equipped",
            CharacterClass.Priest,
            castLines: 1,
            requiredLevel: 11);
        var inventory = Candidate(
            "inventory",
            CharacterClass.Priest,
            castLines: 1,
            requiredLevel: 99);
        var request = Request(
            CharacterClass.Priest,
            baseCastLines: 4,
            inventory:
            [
                new InventoryItemSnapshot(1, inventory.Name)
            ],
            candidates: [inventory, equipped],
            equippedWeapon: "EQUIPPED");

        var selection = StaffSelector.Select(request);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Action, Is.EqualTo(StaffSelectionAction.None));
            Assert.That(
                selection.Reason,
                Is.EqualTo(StaffSelectionReason.AlreadyEquipped));
            Assert.That(selection.Staff, Is.EqualTo(equipped));
            Assert.That(selection.InventorySlot, Is.Null);
        });
    }

    [Test]
    public void ShouldPreferBetterLinesOverHigherStaffRequirement()
    {
        var equipped = Candidate(
            "equipped",
            CharacterClass.Wizard,
            castLines: 2,
            requiredAbilityLevel: 50);
        var better = Candidate(
            "better",
            CharacterClass.Wizard,
            castLines: 1,
            requiredAbilityLevel: 10);
        var request = Request(
            CharacterClass.Wizard,
            baseCastLines: 4,
            inventory:
            [
                new InventoryItemSnapshot(1, better.Name)
            ],
            candidates: [equipped, better],
            abilityLevel: 50,
            equippedWeapon: equipped.Name);

        var selection = StaffSelector.Select(request);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Action, Is.EqualTo(StaffSelectionAction.Equip));
            Assert.That(selection.Staff, Is.EqualTo(better));
        });
    }

    [Test]
    public void ShouldUseInventorySlotAsDeterministicTieBreaker()
    {
        var later = Candidate(
            "later",
            CharacterClass.Wizard,
            castLines: 1);
        var earlier = Candidate(
            "earlier",
            CharacterClass.Wizard,
            castLines: 1);
        var request = Request(
            CharacterClass.Wizard,
            baseCastLines: 4,
            inventory:
            [
                new InventoryItemSnapshot(9, later.Name),
                new InventoryItemSnapshot(2, earlier.Name)
            ],
            candidates: [later, earlier]);

        var selection = StaffSelector.Select(request);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Staff, Is.EqualTo(earlier));
            Assert.That(selection.InventorySlot, Is.EqualTo(2));
        });
    }

    [Test]
    public void ShouldPreferBaseCastingOverStaffWithNoImprovement()
    {
        var equal = Candidate(
            "equal",
            CharacterClass.Wizard,
            castLines: 4);
        var request = Request(
            CharacterClass.Wizard,
            baseCastLines: 4,
            inventory:
            [
                new InventoryItemSnapshot(1, equal.Name)
            ],
            candidates: [equal]);

        var selection = StaffSelector.Select(request);

        Assert.Multiple(() =>
        {
            Assert.That(selection.Action, Is.EqualTo(StaffSelectionAction.None));
            Assert.That(
                selection.Reason,
                Is.EqualTo(StaffSelectionReason.BaseCastIsOptimal));
            Assert.That(selection.Staff, Is.Null);
            Assert.That(selection.CastLines, Is.EqualTo(4));
        });
    }

    [Test]
    public void ShouldUnequipEligibleStaffThatMakesCastingWorse()
    {
        var equipped = Candidate(
            "equipped",
            CharacterClass.Wizard,
            castLines: 5);
        var request = Request(
            CharacterClass.Wizard,
            baseCastLines: 4,
            inventory: [],
            candidates: [equipped],
            equippedWeapon: equipped.Name);

        var selection = StaffSelector.Select(request);

        Assert.Multiple(() =>
        {
            Assert.That(
                selection.Action,
                Is.EqualTo(StaffSelectionAction.Unequip));
            Assert.That(
                selection.Reason,
                Is.EqualTo(StaffSelectionReason.BaseCastIsOptimal));
            Assert.That(selection.Staff, Is.Null);
            Assert.That(selection.CastLines, Is.EqualTo(4));
        });
    }

    [Test]
    public void ShouldProduceEqualSelectionsForEqualInputs()
    {
        var first = RunDeterministicSelection();
        var second = RunDeterministicSelection();

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void ShouldValidateStaffAndSnapshotBoundaries()
    {
        var duplicate = Candidate(
            "duplicate",
            CharacterClass.Wizard,
            castLines: 1);
        var duplicateCase = Candidate(
            "DUPLICATE",
            CharacterClass.Wizard,
            castLines: 2);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = Candidate(
                    "unknown",
                    CharacterClass.Unknown,
                    castLines: 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = Candidate(
                    "negative",
                    CharacterClass.Wizard,
                    castLines: -1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new InventoryItemSnapshot(0, "staff"));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new InventoryItemSnapshot(61, "staff"));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new CharacterSnapshot(
                    (CharacterClass)int.MaxValue,
                    level: 1,
                    abilityLevel: 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => _ = new CharacterSnapshot(
                    CharacterClass.Wizard,
                    level: -1,
                    abilityLevel: 1));
            Assert.Throws<ArgumentException>(
                () => _ = new InventorySnapshot(
                [
                    new InventoryItemSnapshot(1, "first"),
                    new InventoryItemSnapshot(1, "second")
                ]));
            Assert.Throws<ArgumentException>(
                () => _ = Request(
                    CharacterClass.Wizard,
                    baseCastLines: 4,
                    inventory: [],
                    candidates: [duplicate, duplicateCase]));
        });
    }

    [Test]
    public void ShouldCompareInventorySnapshotsByValue()
    {
        var first = new InventorySnapshot(
        [
            new InventoryItemSnapshot(2, "second"),
            new InventoryItemSnapshot(1, "first")
        ]);
        var second = new InventorySnapshot(
        [
            new InventoryItemSnapshot(1, "first"),
            new InventoryItemSnapshot(2, "second")
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
            Assert.That(
                first.Items.Select(item => item.Slot),
                Is.EqualTo(Enumerable.Range(1, 2)));
        });
    }

    private static StaffSelection RunDeterministicSelection()
    {
        var staff = Candidate(
            "staff",
            requiredClass: null,
            castLines: 1);
        var request = Request(
            CharacterClass.Unknown,
            baseCastLines: 4,
            inventory:
            [
                new InventoryItemSnapshot(3, staff.Name)
            ],
            candidates: [staff]);

        return StaffSelector.Select(request);
    }

    private static StaffCandidate Candidate(
        string name,
        CharacterClass? requiredClass,
        int castLines,
        int requiredLevel = 0,
        int requiredAbilityLevel = 0) =>
        new(
            name,
            requiredClass,
            requiredLevel,
            requiredAbilityLevel,
            castLines);

    private static StaffSelectionRequest Request(
        CharacterClass characterClass,
        int baseCastLines,
        IEnumerable<InventoryItemSnapshot> inventory,
        IEnumerable<StaffCandidate> candidates,
        int level = 99,
        int abilityLevel = 99,
        string? equippedWeapon = null) =>
        new(
            baseCastLines,
            new CharacterSnapshot(characterClass, level, abilityLevel),
            new InventorySnapshot(inventory),
            new EquipmentSnapshot(equippedWeapon),
            candidates);
}
