using SleepHunter.Models;
using SleepHunter.Services.Clients;
using SleepHunter.Settings;

namespace SleepHunter.Tests.Services.Clients;

public sealed class ClientSessionRegistryTests
{
    [Test]
    public void ShouldPublishSessionLifecycleAndPreserveLaunchOrder()
    {
        var registry = new ClientSessionRegistry(
            new ClientLayoutManager());
        var newer = CreateSession(
            1002,
            new DateTime(
                2026,
                7,
                27,
                12,
                0,
                0,
                DateTimeKind.Utc));
        var older = CreateSession(
            1001,
            new DateTime(
                2026,
                7,
                27,
                11,
                0,
                0,
                DateTimeKind.Utc));
        var added = new List<ClientSession>();
        ClientSession? removed = null;
        registry.SessionAdded +=
            (_, e) => added.Add(e.Session);
        registry.SessionRemoved +=
            (_, e) => removed = e.Session;

        registry.Add(newer);
        registry.Add(older);
        registry.Add(older);
        var wasRemoved = registry.Remove(newer.Process.ProcessId);

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.EqualTo(new[] { newer, older }));
            Assert.That(removed, Is.SameAs(newer));
            Assert.That(wasRemoved, Is.True);
            Assert.That(registry.Sessions, Is.EqualTo(new[] { older }));
        });
    }

    [Test]
    public void ShouldRejectDifferentSessionForExistingProcess()
    {
        var registry = new ClientSessionRegistry(
            new ClientLayoutManager());
        var original = CreateSession(1001, DateTime.UtcNow);
        var replacement = CreateSession(1001, DateTime.UtcNow);
        registry.Add(original);

        var exception = Assert.Throws<InvalidOperationException>(
            () => registry.Add(replacement));

        Assert.Multiple(() =>
        {
            Assert.That(
                exception?.Message,
                Does.Contain("changed session ownership"));
            Assert.That(
                registry.Sessions.Single(),
                Is.SameAs(original));
        });
    }

    private static ClientSession CreateSession(
        int processId,
        DateTime creationTime) =>
        new(
            new ClientProcess
            {
                ProcessId = processId,
                WindowHandle = new nint(1),
                WindowTitle = "Test",
                CreationTime = creationTime
            });
}
