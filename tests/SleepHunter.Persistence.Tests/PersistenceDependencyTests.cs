using System.Reflection;

namespace SleepHunter.Persistence.Tests;

public sealed class PersistenceDependencyTests
{
    [Test]
    public void ShouldDependOnlyOnRuntimeAndFrameworkAssemblies()
    {
        var references = Assembly.Load("SleepHunter.Persistence")
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(references, Does.Contain("SleepHunter.Runtime"));
            Assert.That(references, Does.Not.Contain("SleepHunter"));
            Assert.That(references, Does.Not.Contain("SleepHunter.Interop"));
            Assert.That(references, Does.Not.Contain("SleepHunter.Updater"));
            Assert.That(references, Does.Not.Contain("PresentationCore"));
            Assert.That(references, Does.Not.Contain("PresentationFramework"));
            Assert.That(references, Does.Not.Contain("WindowsBase"));
        });
    }
}
