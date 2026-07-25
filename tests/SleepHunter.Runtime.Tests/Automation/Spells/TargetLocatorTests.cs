using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Automation.Spells;

public sealed class TargetLocatorTests
{
    private static readonly ClientIdentity SourceClient = new(
        "source",
        "USDA 7.41");

    private static readonly ClientIdentity TargetClient = new(
        "target",
        "USDA 7.41");

    private static readonly MapLocationSnapshot SourceLocation = new(
        100,
        "Mileth",
        50,
        60);

    [Test]
    public void ShouldLeaveTargetsThatDoNotNeedRosterLookupUnchanged()
    {
        var target = SpellTarget.RelativeTile(2, -3);

        var result = TargetLocator.Locate(
            target,
            Snapshot(SourceLocation),
            ClientRosterSnapshot.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(TargetLocationStatus.Resolved));
            Assert.That(result.Target, Is.SameAs(target));
        });
    }

    [Test]
    public void ShouldResolveCharacterToRelativeTileWithOffset()
    {
        var offset = new TargetOffset(4, -5);
        var result = TargetLocator.Locate(
            SpellTarget.Character("aLt", offset),
            Snapshot(SourceLocation),
            Roster(
                Entry(SourceClient, "Caster", SourceLocation),
                Entry(
                    TargetClient,
                    "Alt",
                    new MapLocationSnapshot(100, "Mileth", 53, 58))));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(TargetLocationStatus.Resolved));
            Assert.That(result.Target, Is.Not.Null);
            Assert.That(
                result.Target!.Kind,
                Is.EqualTo(SpellTargetKind.RelativeTile));
            Assert.That(result.Target.X, Is.EqualTo(3));
            Assert.That(result.Target.Y, Is.EqualTo(-2));
            Assert.That(result.Target.Offset, Is.EqualTo(offset));
        });
    }

    [Test]
    public void ShouldRequireObservedRosterAndSourceLocation()
    {
        var target = SpellTarget.Character("Alt");
        var missingRoster = TargetLocator.Locate(
            target,
            Snapshot(SourceLocation),
            ClientRosterSnapshot.Empty);
        var missingSourceLocation = TargetLocator.Locate(
            target,
            Snapshot(location: null),
            Roster(
                Entry(SourceClient, "Caster", SourceLocation),
                Entry(TargetClient, "Alt", SourceLocation)));
        var missingSourceEntry = TargetLocator.Locate(
            target,
            Snapshot(SourceLocation),
            Roster(Entry(TargetClient, "Alt", SourceLocation)));

        Assert.Multiple(() =>
        {
            Assert.That(
                missingRoster.Status,
                Is.EqualTo(TargetLocationStatus.RosterUnavailable));
            Assert.That(
                missingSourceLocation.Status,
                Is.EqualTo(TargetLocationStatus.SourceUnavailable));
            Assert.That(
                missingSourceEntry.Status,
                Is.EqualTo(TargetLocationStatus.SourceUnavailable));
        });
    }

    [Test]
    public void ShouldRejectRosterWhenSourceMovedAfterObservation()
    {
        var result = TargetLocator.Locate(
            SpellTarget.Character("Alt"),
            Snapshot(SourceLocation),
            Roster(
                Entry(
                    SourceClient,
                    "Caster",
                    new MapLocationSnapshot(100, "Mileth", 49, 60)),
                Entry(TargetClient, "Alt", SourceLocation)));

        Assert.That(
            result.Status,
            Is.EqualTo(TargetLocationStatus.SourceChanged));
    }

    [Test]
    public void ShouldRejectUnavailableCharacter()
    {
        var missing = TargetLocator.Locate(
            SpellTarget.Character("Alt"),
            Snapshot(SourceLocation),
            Roster(Entry(SourceClient, "Caster", SourceLocation)));
        var loggedOut = TargetLocator.Locate(
            SpellTarget.Character("Alt"),
            Snapshot(SourceLocation),
            Roster(
                Entry(SourceClient, "Caster", SourceLocation),
                Entry(
                    TargetClient,
                    "Alt",
                    location: null,
                    ClientPresence.LoggedOut)));

        Assert.Multiple(() =>
        {
            Assert.That(
                missing.Status,
                Is.EqualTo(TargetLocationStatus.TargetUnavailable));
            Assert.That(
                loggedOut.Status,
                Is.EqualTo(TargetLocationStatus.TargetUnavailable));
        });
    }

    [Test]
    public void ShouldRejectCharacterOnDifferentMapOrOutsideLocalRange()
    {
        var differentMap = TargetLocator.Locate(
            SpellTarget.Character("Alt"),
            Snapshot(SourceLocation),
            Roster(
                Entry(SourceClient, "Caster", SourceLocation),
                Entry(
                    TargetClient,
                    "Alt",
                    new MapLocationSnapshot(101, "Abel", 50, 60))));
        var outOfRange = TargetLocator.Locate(
            SpellTarget.Character("Alt"),
            Snapshot(SourceLocation),
            Roster(
                Entry(SourceClient, "Caster", SourceLocation),
                Entry(
                    TargetClient,
                    "Alt",
                    new MapLocationSnapshot(100, "Mileth", 61, 60))));

        Assert.Multiple(() =>
        {
            Assert.That(
                differentMap.Status,
                Is.EqualTo(TargetLocationStatus.DifferentMap));
            Assert.That(
                outOfRange.Status,
                Is.EqualTo(TargetLocationStatus.OutOfRange));
        });
    }

    [Test]
    public void ShouldAllowCharacterAtMaximumLocalRange()
    {
        var result = TargetLocator.Locate(
            SpellTarget.Character("Alt"),
            Snapshot(SourceLocation),
            Roster(
                Entry(SourceClient, "Caster", SourceLocation),
                Entry(
                    TargetClient,
                    "Alt",
                    new MapLocationSnapshot(100, "Mileth", 60, 50))));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(TargetLocationStatus.Resolved));
            Assert.That(result.Target!.X, Is.EqualTo(10));
            Assert.That(result.Target.Y, Is.EqualTo(-10));
        });
    }

    private static ClientSnapshot Snapshot(MapLocationSnapshot? location) =>
        new(
            new SnapshotSequence(1),
            MacroTimestamp.Zero,
            MacroTimestamp.Zero,
            SourceClient,
            SnapshotQuality.Complete,
            ClientPresence.InWorld,
            ClientPanel.TemuairSpells,
            location: location);

    private static ClientRosterSnapshot Roster(
        params ClientRosterEntry[] clients) =>
        new(
            new ClientRosterSequence(1),
            MacroTimestamp.Zero,
            clients);

    private static ClientRosterEntry Entry(
        ClientIdentity client,
        string characterName,
        MapLocationSnapshot? location,
        ClientPresence presence = ClientPresence.InWorld) =>
        new(
            client,
            characterName,
            presence,
            isMacroRunning: true,
            isWaitingForMana: false,
            location,
            vitals: null);
}
