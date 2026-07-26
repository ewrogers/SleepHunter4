using System.Windows.Input;
using SleepHunter.Macro;
using SleepHunter.Models;
using SleepHunter.Services.Hotkeys;
using SleepHunter.Tests.Support;

namespace SleepHunter.Tests.Services.Hotkeys;

public sealed class HotkeyAssignmentServiceTests
{
    [Test]
    public void ShouldAssignTheFirstGesture()
    {
        using var player = CreatePlayer("Target");
        var registrations =
            new StubRegistrationService();
        var requested = new Hotkey(
            ModifierKeys.Control,
            Key.D1);
        var service = new HotkeyAssignmentService(
            registrations,
            new TestLogger());

        var result = service.Assign(
            player,
            requested,
            [player]);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Status,
                Is.EqualTo(
                    HotkeyAssignmentStatus.Assigned));
            Assert.That(
                player.Hotkey,
                Is.SameAs(requested));
            Assert.That(
                registrations.Find(
                    Key.D1,
                    ModifierKeys.Control),
                Is.SameAs(requested));
            Assert.That(
                registrations.Operations,
                Is.EqualTo(
                    new[]
                    {
                        "register:Control+1"
                    }));
        });
    }

    [Test]
    public void ShouldRegisterNewGestureBeforeReleasingPreviousOne()
    {
        using var player = CreatePlayer("Target");
        var previous = new Hotkey(
            ModifierKeys.Control,
            Key.F5);
        player.Hotkey = previous;
        var registrations = new StubRegistrationService();
        registrations.Seed(previous);
        var requested = new Hotkey(
            ModifierKeys.Control,
            Key.F6);
        var service = new HotkeyAssignmentService(
            registrations,
            new TestLogger());

        var result = service.Assign(
            player,
            requested,
            [player]);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Status,
                Is.EqualTo(
                    HotkeyAssignmentStatus.Assigned));
            Assert.That(player.Hotkey, Is.SameAs(requested));
            Assert.That(
                registrations.Find(
                    Key.F5,
                    ModifierKeys.Control),
                Is.Null);
            Assert.That(
                registrations.Find(
                    Key.F6,
                    ModifierKeys.Control),
                Is.SameAs(requested));
            Assert.That(
                registrations.Operations,
                Is.EqualTo(
                    new[]
                    {
                        "register:Control+F6",
                        "unregister:Control+F5"
                    }));
        });
    }

    [Test]
    public void ShouldTransferGestureFromAnotherPlayer()
    {
        using var target = CreatePlayer("Target");
        using var conflict = CreatePlayer("Conflict");
        var targetPrevious = new Hotkey(
            ModifierKeys.Alt,
            Key.F4);
        var conflicting = new Hotkey(
            ModifierKeys.Control,
            Key.F7);
        target.Hotkey = targetPrevious;
        conflict.Hotkey = conflicting;
        var registrations = new StubRegistrationService();
        registrations.Seed(targetPrevious);
        registrations.Seed(conflicting);
        var requested = new Hotkey(
            ModifierKeys.Control,
            Key.F7);
        var service = new HotkeyAssignmentService(
            registrations,
            new TestLogger());

        var result = service.Assign(
            target,
            requested,
            [target, conflict]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(target.Hotkey, Is.SameAs(requested));
            Assert.That(conflict.Hotkey, Is.Null);
            Assert.That(
                registrations.Find(
                    Key.F7,
                    ModifierKeys.Control),
                Is.SameAs(requested));
            Assert.That(
                registrations.Find(
                    Key.F4,
                    ModifierKeys.Alt),
                Is.Null);
            Assert.That(
                registrations.Operations,
                Is.EqualTo(
                    new[]
                    {
                        "unregister:Control+F7",
                        "register:Control+F7",
                        "unregister:Alt+F4"
                    }));
        });
    }

    [Test]
    public void ShouldRestoreConflictingRegistrationWhenTransferFails()
    {
        using var target = CreatePlayer("Target");
        using var conflict = CreatePlayer("Conflict");
        var targetPrevious = new Hotkey(
            ModifierKeys.Alt,
            Key.F4);
        var conflicting = new Hotkey(
            ModifierKeys.Control,
            Key.F7);
        target.Hotkey = targetPrevious;
        conflict.Hotkey = conflicting;
        var registrations = new StubRegistrationService();
        registrations.Seed(targetPrevious);
        registrations.Seed(conflicting);
        registrations.RegisterResults.Enqueue(false);
        registrations.RegisterResults.Enqueue(true);
        var service = new HotkeyAssignmentService(
            registrations,
            new TestLogger());

        var result = service.Assign(
            target,
            new Hotkey(
                ModifierKeys.Control,
                Key.F7),
            [target, conflict]);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Status,
                Is.EqualTo(
                    HotkeyAssignmentStatus
                        .RegistrationFailed));
            Assert.That(target.Hotkey, Is.SameAs(targetPrevious));
            Assert.That(conflict.Hotkey, Is.SameAs(conflicting));
            Assert.That(
                registrations.Find(
                    Key.F7,
                    ModifierKeys.Control),
                Is.SameAs(conflicting));
            Assert.That(
                registrations.Find(
                    Key.F4,
                    ModifierKeys.Alt),
                Is.SameAs(targetPrevious));
        });
    }

    [Test]
    public void ShouldLeavePreviousGestureWhenExternalRegistrationFails()
    {
        using var player = CreatePlayer("Target");
        var previous = new Hotkey(
            ModifierKeys.Control,
            Key.F5);
        player.Hotkey = previous;
        var registrations = new StubRegistrationService();
        registrations.Seed(previous);
        registrations.RegisterResults.Enqueue(false);
        var service = new HotkeyAssignmentService(
            registrations,
            new TestLogger());

        var result = service.Assign(
            player,
            new Hotkey(
                ModifierKeys.Control,
                Key.F8),
            [player]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(player.Hotkey, Is.SameAs(previous));
            Assert.That(
                registrations.Find(
                    Key.F5,
                    ModifierKeys.Control),
                Is.SameAs(previous));
            Assert.That(
                registrations.Find(
                    Key.F8,
                    ModifierKeys.Control),
                Is.Null);
        });
    }

    [Test]
    public void ShouldClearAssignedGesture()
    {
        using var player = CreatePlayer("Target");
        var assigned = new Hotkey(
            ModifierKeys.Shift,
            Key.F9);
        player.Hotkey = assigned;
        var registrations = new StubRegistrationService();
        registrations.Seed(assigned);
        var service = new HotkeyAssignmentService(
            registrations,
            new TestLogger());

        var result = service.Clear(player);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Status,
                Is.EqualTo(HotkeyAssignmentStatus.Cleared));
            Assert.That(player.Hotkey, Is.Null);
            Assert.That(
                registrations.Find(
                    Key.F9,
                    ModifierKeys.Shift),
                Is.Null);
        });
    }

    [Test]
    public void ShouldLeaveMatchingActiveGestureUnchanged()
    {
        using var player = CreatePlayer("Target");
        var assigned = new Hotkey(
            ModifierKeys.Control,
            Key.F10);
        player.Hotkey = assigned;
        var registrations = new StubRegistrationService();
        registrations.Seed(assigned);
        var service = new HotkeyAssignmentService(
            registrations,
            new TestLogger());

        var result = service.Assign(
            player,
            new Hotkey(
                ModifierKeys.Control,
                Key.F10),
            [player]);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Status,
                Is.EqualTo(
                    HotkeyAssignmentStatus.Unchanged));
            Assert.That(player.Hotkey, Is.SameAs(assigned));
            Assert.That(registrations.Operations, Is.Empty);
        });
    }

    [Test]
    public void ShouldRollBackWhenPreviousGestureCannotBeReleased()
    {
        using var player = CreatePlayer("Target");
        var previous = new Hotkey(
            ModifierKeys.Control,
            Key.F5);
        player.Hotkey = previous;
        var registrations = new StubRegistrationService();
        registrations.Seed(previous);
        registrations.UnregisterResults.Enqueue(false);
        registrations.UnregisterResults.Enqueue(true);
        var requested = new Hotkey(
            ModifierKeys.Control,
            Key.F6);
        var service = new HotkeyAssignmentService(
            registrations,
            new TestLogger());

        var result = service.Assign(
            player,
            requested,
            [player]);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Status,
                Is.EqualTo(
                    HotkeyAssignmentStatus
                        .RegistrationFailed));
            Assert.That(player.Hotkey, Is.SameAs(previous));
            Assert.That(
                registrations.Find(
                    Key.F5,
                    ModifierKeys.Control),
                Is.SameAs(previous));
            Assert.That(
                registrations.Find(
                    Key.F6,
                    ModifierKeys.Control),
                Is.Null);
            Assert.That(
                registrations.Operations,
                Is.EqualTo(
                    new[]
                    {
                        "register:Control+F6",
                        "unregister:Control+F5",
                        "unregister:Control+F6"
                    }));
        });
    }

    [Test]
    public void ShouldKeepAssignmentWhenClearCannotReleaseRegistration()
    {
        using var player = CreatePlayer("Target");
        var assigned = new Hotkey(
            ModifierKeys.Shift,
            Key.F9);
        player.Hotkey = assigned;
        var registrations = new StubRegistrationService();
        registrations.Seed(assigned);
        registrations.UnregisterResults.Enqueue(false);
        var service = new HotkeyAssignmentService(
            registrations,
            new TestLogger());

        var result = service.Clear(player);

        Assert.Multiple(() =>
        {
            Assert.That(
                result.Status,
                Is.EqualTo(
                    HotkeyAssignmentStatus
                        .RegistrationFailed));
            Assert.That(player.Hotkey, Is.SameAs(assigned));
            Assert.That(
                registrations.Find(
                    Key.F9,
                    ModifierKeys.Shift),
                Is.SameAs(assigned));
        });
    }

    private static Player CreatePlayer(string name) =>
        new(
            new ClientProcess
            {
                ProcessId = Environment.ProcessId,
                WindowHandle = new nint(1),
                WindowTitle = "Test Window"
            })
        {
            Name = name,
            IsLoggedIn = true
        };

    private sealed class StubRegistrationService :
        IHotkeyRegistrationService
    {
        private readonly Dictionary<
            (Key Key, ModifierKeys Modifiers),
            Hotkey> registrations = [];

        public Queue<bool> RegisterResults { get; } = [];

        public Queue<bool> UnregisterResults { get; } = [];

        public List<string> Operations { get; } = [];

        public Hotkey Find(
            Key key,
            ModifierKeys modifiers) =>
            registrations.GetValueOrDefault(
                (key, modifiers))!;

        public bool Register(Hotkey hotkey)
        {
            Operations.Add($"register:{hotkey}");
            var succeeds = RegisterResults.Count == 0 ||
                           RegisterResults.Dequeue();
            if (succeeds)
            {
                registrations[
                    (hotkey.Key, hotkey.Modifiers)] =
                    hotkey;
            }

            return succeeds;
        }

        public bool Unregister(Hotkey hotkey)
        {
            Operations.Add($"unregister:{hotkey}");
            var succeeds =
                UnregisterResults.Count == 0 ||
                UnregisterResults.Dequeue();
            if (!succeeds)
                return false;

            var key = (hotkey.Key, hotkey.Modifiers);
            return registrations.TryGetValue(
                       key,
                       out var registered) &&
                   ReferenceEquals(registered, hotkey) &&
                   registrations.Remove(key);
        }

        public void Seed(Hotkey hotkey) =>
            registrations[
                (hotkey.Key, hotkey.Modifiers)] =
                hotkey;
    }
}
