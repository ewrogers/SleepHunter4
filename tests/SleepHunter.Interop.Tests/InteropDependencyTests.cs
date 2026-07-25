using System.Reflection;

namespace SleepHunter.Interop.Tests;

public sealed class InteropDependencyTests
{
    [Test]
    public void ShouldRemainIndependentOfDesktopApplicationAssemblies()
    {
        var references = Assembly.Load("SleepHunter.Interop")
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(references, Does.Not.Contain("SleepHunter"));
            Assert.That(references, Does.Not.Contain("SleepHunter.Updater"));
            Assert.That(references, Does.Not.Contain("PresentationCore"));
            Assert.That(references, Does.Not.Contain("PresentationFramework"));
        });
    }
}
