using SleepHunter.Services.Configuration;

namespace SleepHunter.Tests.Services.Configuration;

public sealed class WpfMacroConfigurationInteractionTests
{
    [Test]
    public void ShouldLabelCurrentAndLegacyMacroFileTypes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                WpfMacroConfigurationInteraction.LoadFileFilter,
                Is.EqualTo(
                    "SleepHunter 4 Macro Files (*.sh4x)|*.sh4x|" +
                    "SleepHunter 4 Legacy Files (*.sh4)|*.sh4"));
            Assert.That(
                WpfMacroConfigurationInteraction.SaveFileFilter,
                Is.EqualTo(
                    "SleepHunter 4 Macro Files (*.sh4x)|*.sh4x"));
        });
    }
}
