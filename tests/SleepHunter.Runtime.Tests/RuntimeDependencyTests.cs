using System.Reflection;

namespace SleepHunter.Runtime.Tests;

public sealed class RuntimeDependencyTests
{
    [Test]
    public void ShouldRemainIndependentOfPlatformAndApplicationAssemblies()
    {
        var references = Assembly.Load("SleepHunter.Runtime")
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(references, Does.Not.Contain("SleepHunter"));
            Assert.That(references, Does.Not.Contain("SleepHunter.Interop"));
            Assert.That(references, Does.Not.Contain("SleepHunter.Persistence"));
            Assert.That(references, Does.Not.Contain("PresentationCore"));
            Assert.That(references, Does.Not.Contain("PresentationFramework"));
            Assert.That(references, Does.Not.Contain("WindowsBase"));
        });
    }
}
