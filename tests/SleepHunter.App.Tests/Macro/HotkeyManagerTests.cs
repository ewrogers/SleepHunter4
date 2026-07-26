using System.Windows.Input;
using SleepHunter.Macro;

namespace SleepHunter.Tests.Macro;

public sealed class HotkeyManagerTests
{
    [Test]
    public void ShouldRegisterWithAnApplicationHotkeyId()
    {
        var api = new StubWindowHotkeyApi();
        var manager = new HotkeyManager(api);
        var hotkey = new Hotkey(
            ModifierKeys.Control,
            Key.D1);

        var registered = manager.RegisterHotkey(
            new nint(42),
            hotkey);

        Assert.Multiple(() =>
        {
            Assert.That(registered, Is.True);
            Assert.That(hotkey.IsActive, Is.True);
            Assert.That(
                hotkey.Id,
                Is.InRange(
                    1,
                    HotkeyManager
                        .MaximumApplicationHotkeyId));
            Assert.That(
                manager.GetHotkey(
                    Key.D1,
                    ModifierKeys.Control),
                Is.SameAs(hotkey));
            Assert.That(api.RegisteredId, Is.EqualTo(hotkey.Id));
            Assert.That(
                api.RegisteredWindow,
                Is.EqualTo(new nint(42)));
            Assert.That(
                api.RegisteredModifiers,
                Is.EqualTo(ModifierKeys.Control));
            Assert.That(
                api.RegisteredVirtualKey,
                Is.EqualTo(
                    KeyInterop.VirtualKeyFromKey(Key.D1)));
        });
    }

    [Test]
    public void ShouldReleaseTheRegistrationAndResetTheId()
    {
        var api = new StubWindowHotkeyApi();
        var manager = new HotkeyManager(api);
        var hotkey = new Hotkey(
            ModifierKeys.Alt,
            Key.F6);
        manager.RegisterHotkey(
            new nint(7),
            hotkey);
        var registeredId = hotkey.Id;

        var unregistered = manager.UnregisterHotkey(
            new nint(7),
            hotkey);

        Assert.Multiple(() =>
        {
            Assert.That(unregistered, Is.True);
            Assert.That(hotkey.IsActive, Is.False);
            Assert.That(hotkey.Id, Is.EqualTo(-1));
            Assert.That(api.UnregisteredId, Is.EqualTo(registeredId));
            Assert.That(
                manager.GetHotkey(
                    Key.F6,
                    ModifierKeys.Alt),
                Is.Null);
        });
    }

    [Test]
    public void ShouldLeaveARejectedRegistrationInactive()
    {
        var api = new StubWindowHotkeyApi
        {
            RegisterResult = false
        };
        var manager = new HotkeyManager(api);
        var hotkey = new Hotkey(
            ModifierKeys.Shift,
            Key.F8);

        var registered = manager.RegisterHotkey(
            new nint(3),
            hotkey);

        Assert.Multiple(() =>
        {
            Assert.That(registered, Is.False);
            Assert.That(hotkey.IsActive, Is.False);
            Assert.That(hotkey.Id, Is.EqualTo(-1));
            Assert.That(manager.Hotkeys, Is.Empty);
        });
    }

    [Test]
    public void ShouldRetainStateWhenWindowsRejectsUnregister()
    {
        var api = new StubWindowHotkeyApi();
        var manager = new HotkeyManager(api);
        var hotkey = new Hotkey(
            ModifierKeys.Windows,
            Key.F9);
        manager.RegisterHotkey(
            new nint(5),
            hotkey);
        api.UnregisterResult = false;

        var unregistered = manager.UnregisterHotkey(
            new nint(5),
            hotkey);

        Assert.Multiple(() =>
        {
            Assert.That(unregistered, Is.False);
            Assert.That(hotkey.IsActive, Is.True);
            Assert.That(
                manager.GetHotkey(
                    Key.F9,
                    ModifierKeys.Windows),
                Is.SameAs(hotkey));
        });
    }

    private sealed class StubWindowHotkeyApi :
        IWindowHotkeyApi
    {
        public bool RegisterResult { get; set; } = true;

        public bool UnregisterResult { get; set; } = true;

        public nint RegisteredWindow { get; private set; }

        public int RegisteredId { get; private set; } = -1;

        public ModifierKeys RegisteredModifiers { get; private set; }

        public int RegisteredVirtualKey { get; private set; }

        public int UnregisteredId { get; private set; } = -1;

        public bool Register(
            nint windowHandle,
            int hotkeyId,
            ModifierKeys modifiers,
            int virtualKey)
        {
            RegisteredWindow = windowHandle;
            RegisteredId = hotkeyId;
            RegisteredModifiers = modifiers;
            RegisteredVirtualKey = virtualKey;
            return RegisterResult;
        }

        public bool Unregister(
            nint windowHandle,
            int hotkeyId)
        {
            UnregisteredId = hotkeyId;
            return UnregisterResult;
        }
    }
}
