using System.Windows.Input;
using SleepHunter.Services.Hotkeys;

namespace SleepHunter.Tests.Services.Hotkeys;

public sealed class HotkeyInputParserTests
{
    [Test]
    public void ShouldAssignControlNumber()
    {
        var input = HotkeyInputParser.Parse(
            Key.D1,
            Key.None,
            ModifierKeys.Control);

        Assert.Multiple(() =>
        {
            Assert.That(
                input.Kind,
                Is.EqualTo(HotkeyInputKind.Assign));
            Assert.That(
                input.Hotkey?.Key,
                Is.EqualTo(Key.D1));
            Assert.That(
                input.Hotkey?.Modifiers,
                Is.EqualTo(ModifierKeys.Control));
        });
    }

    [Test]
    public void ShouldAssignAnUnmodifiedFunctionKey()
    {
        var input = HotkeyInputParser.Parse(
            Key.F8,
            Key.None,
            ModifierKeys.None);

        Assert.Multiple(() =>
        {
            Assert.That(
                input.Kind,
                Is.EqualTo(HotkeyInputKind.Assign));
            Assert.That(input.Hotkey?.Key, Is.EqualTo(Key.F8));
            Assert.That(
                input.Hotkey?.Modifiers,
                Is.EqualTo(ModifierKeys.None));
        });
    }

    [Test]
    public void ShouldResolveAnAltSystemKey()
    {
        var input = HotkeyInputParser.Parse(
            Key.System,
            Key.D2,
            ModifierKeys.Alt);

        Assert.Multiple(() =>
        {
            Assert.That(
                input.Kind,
                Is.EqualTo(HotkeyInputKind.Assign));
            Assert.That(input.Hotkey?.Key, Is.EqualTo(Key.D2));
            Assert.That(
                input.Hotkey?.Modifiers,
                Is.EqualTo(ModifierKeys.Alt));
        });
    }

    [TestCase(Key.Escape)]
    [TestCase(Key.Delete)]
    [TestCase(Key.Back)]
    public void ShouldClearWithAnUnmodifiedClearKey(
        Key key)
    {
        var input = HotkeyInputParser.Parse(
            key,
            Key.None,
            ModifierKeys.None);

        Assert.Multiple(() =>
        {
            Assert.That(
                input.Kind,
                Is.EqualTo(HotkeyInputKind.Clear));
            Assert.That(input.Hotkey, Is.Null);
        });
    }

    [Test]
    public void ShouldIgnoreAnUnmodifiedNumber()
    {
        var input = HotkeyInputParser.Parse(
            Key.D1,
            Key.None,
            ModifierKeys.None);

        Assert.That(
            input.Kind,
            Is.EqualTo(HotkeyInputKind.Ignore));
    }

    [TestCase(Key.LeftCtrl)]
    [TestCase(Key.RightAlt)]
    [TestCase(Key.LeftShift)]
    [TestCase(Key.LWin)]
    public void ShouldIgnoreModifierKeys(
        Key key)
    {
        var input = HotkeyInputParser.Parse(
            key,
            Key.None,
            ModifierKeys.Control);

        Assert.That(
            input.Kind,
            Is.EqualTo(HotkeyInputKind.Ignore));
    }
}
