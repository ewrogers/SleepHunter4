using SleepHunter.Models;
using SleepHunter.ViewModels;

namespace SleepHunter.Tests.ViewModels;

public sealed class ClientListSortingTests
{
    [Test]
    public void ShouldSortLaunchOrderFromOldestProcessToNewest()
    {
        var newer = CreateSession(
            "Newer",
            new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc));
        var older = CreateSession(
            "Older",
            new DateTime(2026, 7, 25, 11, 0, 0, DateTimeKind.Utc));

        using var newerItem =
            new ClientListItemViewModel(newer);
        using var olderItem =
            new ClientListItemViewModel(older);

        var sorted = ClientListViewModel.SortClients(
                [newerItem, olderItem],
                ClientSortOrder.LaunchOrder)
            .ToArray();

        Assert.That(
            sorted,
            Is.EqualTo(new[] { olderItem, newerItem }));
    }

    [Test]
    public void ShouldTreatLegacyLoginTimeAsLaunchOrder()
    {
        Assert.That(
            ClientSortOrder.LoginTime,
            Is.EqualTo(ClientSortOrder.LaunchOrder));
    }

    private static ClientSession CreateSession(
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
            Name = name
        };
}
