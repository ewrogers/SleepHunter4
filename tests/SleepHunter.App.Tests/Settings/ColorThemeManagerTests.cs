using System.Windows.Media;

using SleepHunter.Settings;

namespace SleepHunter.Tests.Settings;

public sealed class ColorThemeManagerTests
{
    [Test]
    public void ShouldUseADarkInsetForBrightAccentColors()
    {
        var inset = ColorThemeManager.CreateAccentInsetColor(
            Color.FromRgb(202, 138, 4));

        Assert.That(
            inset,
            Is.EqualTo(Color.FromArgb(32, 0, 0, 0)));
    }

    [Test]
    public void ShouldUseALightInsetForDarkAccentColors()
    {
        var inset = ColorThemeManager.CreateAccentInsetColor(
            Color.FromRgb(30, 64, 175));

        Assert.That(
            inset,
            Is.EqualTo(Color.FromArgb(32, 255, 255, 255)));
    }

    [Test]
    public void ShouldUseDarkTextForBrightAccentColors()
    {
        var foreground =
            ColorThemeManager.CreateAccentForegroundColor(
                Color.FromRgb(202, 138, 4));

        Assert.That(
            foreground,
            Is.EqualTo(Color.FromRgb(0, 0, 0)));
    }

    [Test]
    public void ShouldUseLightTextForDarkAccentColors()
    {
        var foreground =
            ColorThemeManager.CreateAccentForegroundColor(
                Color.FromRgb(30, 64, 175));

        Assert.That(
            foreground,
            Is.EqualTo(Color.FromRgb(255, 255, 255)));
    }
}
