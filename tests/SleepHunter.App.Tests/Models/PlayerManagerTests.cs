using SleepHunter.Models;

namespace SleepHunter.Tests.Models;

public sealed class PlayerManagerTests
{
    [Test]
    public void ShouldSortLaunchOrderFromOldestProcessToNewest()
    {
        using var newer = CreatePlayer(
            "Newer",
            new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc));
        using var older = CreatePlayer(
            "Older",
            new DateTime(2026, 7, 25, 11, 0, 0, DateTimeKind.Utc));

        var sorted = PlayerManager.SortPlayers(
                [newer, older],
                PlayerSortOrder.LaunchOrder)
            .ToArray();

        Assert.That(sorted, Is.EqualTo(new[] { older, newer }));
    }

    [Test]
    public void ShouldTreatLegacyLoginTimeAsLaunchOrder()
    {
        Assert.That(
            PlayerSortOrder.LoginTime,
            Is.EqualTo(PlayerSortOrder.LaunchOrder));
    }

    private static Player CreatePlayer(
        string name,
        DateTime creationTime) =>
        new(
            new ClientProcess
            {
                ProcessId = Environment.ProcessId,
                WindowHandle = new nint(1),
                WindowTitle = "Test",
                CreationTime = creationTime
            })
        {
            Name = name,
            IsLoggedIn = true
        };
}
