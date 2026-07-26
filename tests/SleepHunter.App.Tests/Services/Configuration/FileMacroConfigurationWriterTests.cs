using SleepHunter.Persistence.Configuration;
using SleepHunter.Persistence.Serialization;
using SleepHunter.Services.Configuration;

namespace SleepHunter.Tests.Services.Configuration;

public sealed class FileMacroConfigurationWriterTests
{
    private string testDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        testDirectory = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            nameof(FileMacroConfigurationWriterTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(testDirectory))
            Directory.Delete(testDirectory, recursive: true);
    }

    [Test]
    public async Task ShouldWriteCurrentJsonConfiguration()
    {
        var filePath = Path.Combine(
            testDirectory,
            $"Test{MacroConfigurationSerializer.CurrentFileExtension}");
        var configuration = new MacroConfiguration(
            name: "Test",
            description: "Current JSON");
        var writer = new FileMacroConfigurationWriter();

        await writer.SaveAsync(configuration, filePath);

        var document = await File.ReadAllTextAsync(filePath);
        var loaded = MacroConfigurationSerializer.Load(filePath);
        Assert.Multiple(() =>
        {
            Assert.That(document.TrimStart(), Does.StartWith("{"));
            Assert.That(
                loaded.Format,
                Is.EqualTo(MacroConfigurationFormat.Current));
            Assert.That(
                loaded.Configuration,
                Is.EqualTo(configuration));
        });
    }
}
