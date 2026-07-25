using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Snapshots;

public sealed class ClientRosterSnapshotTests
{
    [Test]
    public void ShouldExposeEmptyRosterWithoutAnObservation()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ClientRosterSnapshot.Empty.HasObservation, Is.False);
            Assert.That(ClientRosterSnapshot.Empty.Sequence, Is.Null);
            Assert.That(ClientRosterSnapshot.Empty.CapturedAt, Is.Null);
            Assert.That(ClientRosterSnapshot.Empty.Clients, Is.Empty);
        });
    }

    [Test]
    public void ShouldRejectDuplicateClientIdentifiersOrCharacterNames()
    {
        var duplicateClient = new[]
        {
            Entry("client", "First"),
            Entry("client", "Second")
        };
        var duplicateName = new[]
        {
            Entry("first", "Character"),
            Entry("second", "character")
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                () => Roster(duplicateClient),
                Throws.ArgumentException);
            Assert.That(
                () => Roster(duplicateName),
                Throws.ArgumentException);
        });
    }

    [Test]
    public void ShouldRejectNullRosterEntries()
    {
        Assert.That(
            () => Roster([Entry("first", "First"), null!]),
            Throws.ArgumentException);
    }

    [Test]
    public void ShouldRequirePositiveRosterSequence()
    {
        Assert.That(
            () => new ClientRosterSequence(0),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    private static ClientRosterSnapshot Roster(
        IEnumerable<ClientRosterEntry> clients) =>
        new(
            new ClientRosterSequence(1),
            MacroTimestamp.Zero,
            clients);

    private static ClientRosterEntry Entry(
        string clientId,
        string characterName) =>
        new(
            new ClientIdentity(clientId, "test"),
            characterName,
            ClientPresence.InWorld,
            isMacroRunning: true,
            isWaitingForMana: false,
            location: null,
            vitals: null);
}
