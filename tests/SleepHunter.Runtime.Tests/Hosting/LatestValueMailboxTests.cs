using SleepHunter.Runtime.Hosting;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Hosting;

public sealed class LatestValueMailboxTests
{
    [Test]
    public void ShouldCoalesceQueuedSnapshotsToTheNewestValue()
    {
        var mailbox = new LatestValueMailbox<ClientSnapshot>();
        var client = new ClientIdentity("client", "test");

        mailbox.TryWrite(CreateSnapshot(client, 1));
        mailbox.TryWrite(CreateSnapshot(client, 2));
        mailbox.TryWrite(CreateSnapshot(client, 3));

        var didRead = mailbox.TryReadLatest(out var snapshot);
        var didReadAgain = mailbox.TryReadLatest(out _);

        Assert.Multiple(() =>
        {
            Assert.That(didRead, Is.True);
            Assert.That(snapshot.Sequence.Value, Is.EqualTo(3));
            Assert.That(didReadAgain, Is.False);
        });
    }

    private static ClientSnapshot CreateSnapshot(
        ClientIdentity client,
        long sequence) =>
        new(
            new SnapshotSequence(sequence),
            MacroTimestamp.Zero,
            MacroTimestamp.Zero,
            client,
            SnapshotQuality.Complete,
            ClientPresence.InWorld);
}
