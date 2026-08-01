using System.Buffers.Binary;
using System.Text;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Tests.Snapshots;

public sealed class ClientAbilityParserTests
{
    [Test]
    public void ShouldParseCompactSkillsAndApplyMetadata()
    {
        var snapshot = new byte[
            ClientAbilityParser.CompactSkillRecordSize *
            ClientAbilityParser.CompactRecordCount];
        WriteCompactSkill(
            snapshot,
            slot: 1,
            "Assail (Lev:3/100)");
        var healthCondition = new HealthCondition(
            minimumPercentExclusive: 10,
            maximumPercentInclusive: 80);
        var catalog = new AbilitySnapshotCatalog(
            [
                new SkillSnapshotMetadata(
                    "Assail",
                    manaCost: 5,
                    TimeSpan.FromSeconds(2),
                    isAssail: true,
                    opensDialog: true,
                    requiresDisarm: true,
                    healthCondition)
            ],
            []);

        var skillbook = ClientAbilityParser.ParseCompactSkills(
            snapshot,
            ClientAbilityParser.CompactRecordCount,
            catalog);

        Assert.That(
            skillbook.Skills,
            Is.EqualTo(
                new[]
                {
                    new SkillSnapshot(
                        "Assail",
                        slot: 1,
                        currentLevel: 3,
                        maximumLevel: 100,
                        manaCost: 5,
                        TimeSpan.FromSeconds(2),
                        isAssail: true,
                        opensDialog: true,
                        requiresDisarm: true,
                        healthCondition)
                }));
    }

    [Test]
    public void ShouldParseCompactSpellsAndUseMetadataCastLines()
    {
        var snapshot = new byte[
            ClientAbilityParser.CompactSpellRecordSize *
            ClientAbilityParser.CompactRecordCount];
        WriteCompactSpell(
            snapshot,
            slot: 73,
            "ard cradh (Lev:7/100)");
        var catalog = new AbilitySnapshotCatalog(
            [],
            [
                new SpellSnapshotMetadata(
                    "ard cradh",
                    castLines: 3,
                    manaCost: 500,
                    TimeSpan.FromSeconds(4),
                    opensDialog: true)
            ]);

        var spellbook = ClientAbilityParser.ParseCompactSpells(
            snapshot,
            ClientAbilityParser.CompactRecordCount,
            catalog);

        Assert.That(
            spellbook.Spells,
            Is.EqualTo(
                new[]
                {
                    new SpellSnapshot(
                        "ard cradh",
                        slot: 73,
                        currentLevel: 7,
                        maximumLevel: 100,
                        castLines: 3,
                        manaCost: 500,
                        TimeSpan.FromSeconds(4),
                        opensDialog: true)
                }));
    }

    [Test]
    public void ShouldPreferPaneSpellCastLinesAndPreserveActionDelay()
    {
        var snapshot = new byte[
            ClientAbilityParser.SpellPaneSnapshotSize];
        snapshot[0] = 73;
        BinaryPrimitives.WriteUInt16LittleEndian(
            snapshot.AsSpan(0x02),
            222);
        snapshot[0x04] = 1;
        Encoding.ASCII.GetBytes("ard cradh").CopyTo(snapshot.AsSpan(0x05));
        Encoding.ASCII.GetBytes("Which target?").CopyTo(
            snapshot.AsSpan(0x85));
        snapshot[0x105] = 4;
        snapshot[0x107] = 1;
        var catalog = new AbilitySnapshotCatalog(
            [],
            [
                new SpellSnapshotMetadata(
                    "ard cradh",
                    castLines: 3,
                    manaCost: 500,
                    TimeSpan.FromSeconds(4))
            ]);

        var record = ClientAbilityParser.ParseSpellPane(snapshot);
        var spell = ClientAbilityParser.CreateSpell(record, catalog);

        Assert.That(
            spell,
            Is.EqualTo(
                new SpellSnapshot(
                    "ard cradh",
                    slot: 73,
                    currentLevel: 0,
                    maximumLevel: 0,
                    castLines: 4,
                    manaCost: 500,
                    TimeSpan.FromSeconds(4),
                    isActionDelayed: true,
                    icon: 222,
                    argumentType:
                        SpellArgumentType.TextInput,
                    prompt: "Which target?")));
    }

    [Test]
    public void ShouldIgnoreNonAsciiBytesInSpellPrompts()
    {
        var compact = new byte[
            ClientAbilityParser.CompactSpellRecordSize];
        WriteCompactSpell(
            compact,
            slot: 1,
            "ard cradh",
            SpellArgumentType.TextInput);
        WritePromptWithNonAsciiBytes(compact.AsSpan(0x105));

        var pane = new byte[ClientAbilityParser.SpellPaneSnapshotSize];
        pane[0] = 1;
        pane[0x04] = (byte)SpellArgumentType.TextInput;
        Encoding.ASCII.GetBytes("ard cradh").CopyTo(pane.AsSpan(0x05));
        WritePromptWithNonAsciiBytes(pane.AsSpan(0x85));

        var compactSpell = ClientAbilityParser.ParseCompactSpells(
            compact,
            recordCount: 1,
            AbilitySnapshotCatalog.Empty).Spells.Single();
        var paneRecord = ClientAbilityParser.ParseSpellPane(pane);

        Assert.Multiple(() =>
        {
            Assert.That(compactSpell.Prompt, Is.EqualTo("Which target?"));
            Assert.That(paneRecord.Prompt, Is.EqualTo("Which target?"));
        });
    }

    [Test]
    public void ShouldParsePaneSuffixLevel()
    {
        var snapshot = new byte[
            ClientAbilityParser.SkillPaneSnapshotSize];
        BinaryPrimitives.WriteUInt16LittleEndian(snapshot, 111);
        Encoding.ASCII.GetBytes("Assail 3").CopyTo(snapshot.AsSpan(0x02));
        snapshot[0x182] = 37;
        BinaryPrimitives.WriteUInt32LittleEndian(
            snapshot.AsSpan(0x184),
            15);
        BinaryPrimitives.WriteUInt32LittleEndian(
            snapshot.AsSpan(0x188),
            1000);
        BinaryPrimitives.WriteUInt32LittleEndian(
            snapshot.AsSpan(0x18C),
            2000);
        snapshot[0x190] = 1;
        snapshot[0x192] = 1;
        BinaryPrimitives.WriteInt32LittleEndian(
            snapshot.AsSpan(0x1AC, 4),
            3);
        BinaryPrimitives.WriteInt32LittleEndian(
            snapshot.AsSpan(0x1B4, 4),
            6);

        var record = ClientAbilityParser.ParseSkillPane(snapshot);
        var skill = ClientAbilityParser.CreateSkill(
            record,
            AbilitySnapshotCatalog.Empty);

        Assert.That(
            skill,
            Is.EqualTo(
                new SkillSnapshot(
                    "Assail",
                    slot: 37,
                    currentLevel: 3,
                    maximumLevel: 0,
                    manaCost: 0,
                    TimeSpan.Zero,
                    isActionDelayed: true,
                    icon: 111,
                    cooldownProgress: 15,
                    cooldownStartedAt: 1000,
                    cooldownEndsAt: 2000,
                    isCooldownVisualActive: true)));
    }

    [Test]
    public void ShouldRejectInvalidAbilitySnapshots()
    {
        var duplicateNames = new byte[
            ClientAbilityParser.CompactSkillRecordSize * 2];
        WriteCompactSkill(duplicateNames, slot: 1, "Assail");
        WriteCompactSkill(duplicateNames, slot: 2, "Assail");
        var invalidEncoding = new byte[
            ClientAbilityParser.CompactSkillRecordSize];
        BinaryPrimitives.WriteInt16LittleEndian(invalidEncoding, 1);
        invalidEncoding[4] = 0xFF;
        var invalidSpellName = new byte[
            ClientAbilityParser.CompactSpellRecordSize];
        WriteCompactSpell(invalidSpellName, slot: 1, "ard cradh");
        invalidSpellName[5] = 0xFF;

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidDataException>(
                () => ClientAbilityParser.ParseCompactSkills(
                    new byte[1],
                    recordCount: 1,
                    AbilitySnapshotCatalog.Empty));
            Assert.Throws<InvalidDataException>(
                () => ClientAbilityParser.ParseCompactSkills(
                    duplicateNames,
                    recordCount: 2,
                    AbilitySnapshotCatalog.Empty));
            Assert.Throws<InvalidDataException>(
                () => ClientAbilityParser.ParseCompactSkills(
                    invalidEncoding,
                    recordCount: 1,
                    AbilitySnapshotCatalog.Empty));
            Assert.Throws<InvalidDataException>(
                () => ClientAbilityParser.ParseCompactSpells(
                    invalidSpellName,
                    recordCount: 1,
                    AbilitySnapshotCatalog.Empty));
        });
    }

    [Test]
    public void ShouldRequireUniqueCatalogNames()
    {
        Assert.Throws<ArgumentException>(
            () => _ = new AbilitySnapshotCatalog(
                [
                    new SkillSnapshotMetadata(
                        "Assail",
                        manaCost: 0,
                        TimeSpan.Zero),
                    new SkillSnapshotMetadata(
                        "assail",
                        manaCost: 0,
                        TimeSpan.Zero)
                ],
                []));
    }

    private static void WriteCompactSkill(
        Span<byte> snapshot,
        int slot,
        string name)
    {
        var record = snapshot.Slice(
            (slot - 1) * ClientAbilityParser.CompactSkillRecordSize,
            ClientAbilityParser.CompactSkillRecordSize);
        BinaryPrimitives.WriteInt16LittleEndian(record, 1);
        Encoding.ASCII.GetBytes(name).CopyTo(record[4..]);
    }

    private static void WriteCompactSpell(
        Span<byte> snapshot,
        int slot,
        string name,
        SpellArgumentType argumentType = SpellArgumentType.Unknown)
    {
        var record = snapshot.Slice(
            (slot - 1) * ClientAbilityParser.CompactSpellRecordSize,
            ClientAbilityParser.CompactSpellRecordSize);
        BinaryPrimitives.WriteInt16LittleEndian(record, 1);
        record[4] = (byte)argumentType;
        Encoding.ASCII.GetBytes(name).CopyTo(record[5..]);
    }

    private static void WritePromptWithNonAsciiBytes(Span<byte> prompt)
    {
        Encoding.ASCII.GetBytes("Which ").CopyTo(prompt);
        prompt[6] = 0x80;
        Encoding.ASCII.GetBytes("target").CopyTo(prompt[7..]);
        prompt[13] = 0xFF;
        prompt[14] = (byte)'?';
    }
}
