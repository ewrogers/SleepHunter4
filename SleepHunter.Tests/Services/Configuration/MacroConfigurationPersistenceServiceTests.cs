using System.Collections.Immutable;
using System.Windows.Input;
using SleepHunter.Macro;
using SleepHunter.Models;
using SleepHunter.Persistence.Configuration;
using SleepHunter.Persistence.Serialization;
using SleepHunter.Services.Configuration;
using SleepHunter.Services.Hotkeys;
using SleepHunter.Tests.Support;

namespace SleepHunter.Tests.Services.Configuration;

public sealed class MacroConfigurationPersistenceServiceTests
{
    private string testDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        testDirectory = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            nameof(MacroConfigurationPersistenceServiceTests),
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
    public async Task ShouldApplyConfigurationAndReplaceHotkey()
    {
        using var player = CreatePlayer("Destination");
        player.Hotkey = new Hotkey(
            ModifierKeys.Control,
            Key.F5);
        var previousHotkey = player.Hotkey;
        var editable = new PlayerMacroConfiguration(player);
        var loaded = CreateLoadResult(
            description: "Loaded configuration",
            hotkey: new HotkeyConfiguration(
                nameof(Key.F6),
                HotkeyModifiers.Shift));
        var reader = new StubReader(loaded);
        var hotkeys = new StubHotkeyRegistrationService();
        var service = CreateService(
            reader,
            new StubWriter(),
            hotkeys);

        var result = await service.LoadAsync(
            editable,
            Path.Combine(testDirectory, "Loaded.sh4x"));

        Assert.Multiple(() =>
        {
            Assert.That(
                editable.Description,
                Is.EqualTo("Loaded configuration"));
            Assert.That(
                hotkeys.Unregistered,
                Is.EqualTo(new[] { previousHotkey }));
            Assert.That(
                hotkeys.Registered,
                Is.EqualTo(new[] { player.Hotkey }));
            Assert.That(player.Hotkey?.Key, Is.EqualTo(Key.F6));
            Assert.That(
                result.HotkeyRegistrationFailed,
                Is.False);
        });
    }

    [Test]
    public async Task ShouldClearImportedHotkeyWhenRegistrationFails()
    {
        using var player = CreatePlayer("Destination");
        var editable = new PlayerMacroConfiguration(player);
        var loaded = CreateLoadResult(
            hotkey: new HotkeyConfiguration(
                nameof(Key.F7),
                HotkeyModifiers.Control));
        var hotkeys = new StubHotkeyRegistrationService
        {
            RegistrationSucceeds = false
        };
        var service = CreateService(
            new StubReader(loaded),
            new StubWriter(),
            hotkeys);

        var result = await service.LoadAsync(
            editable,
            Path.Combine(testDirectory, "Loaded.sh4x"));

        Assert.Multiple(() =>
        {
            Assert.That(player.Hotkey, Is.Null);
            Assert.That(
                result.HotkeyRegistrationFailed,
                Is.True);
            Assert.That(hotkeys.Registered, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task ShouldSaveMappedSnapshot()
    {
        using var player = CreatePlayer("Saver");
        var editable = new PlayerMacroConfiguration(player)
        {
            Description = "Save me"
        };
        var writer = new StubWriter();
        var service = CreateService(
            new StubReader(CreateLoadResult()),
            writer,
            new StubHotkeyRegistrationService());
        var filePath = Path.Combine(testDirectory, "Saved.sh4x");

        await service.SaveAsync(editable, filePath);

        Assert.Multiple(() =>
        {
            Assert.That(writer.Paths, Is.EqualTo(new[] { filePath }));
            Assert.That(writer.Configurations, Has.Count.EqualTo(1));
            Assert.That(
                writer.Configurations.Single().Name,
                Is.EqualTo("Saver"));
            Assert.That(
                writer.Configurations.Single().Description,
                Is.EqualTo("Save me"));
        });
    }

    [Test]
    public async Task ShouldMigrateLegacyAutosaveToCurrentPath()
    {
        using var player = CreatePlayer("Legacy");
        var editable = new PlayerMacroConfiguration(player);
        var legacyPath = Path.Combine(
            testDirectory,
            "autosave",
            $"Legacy-Autosave{MacroConfigurationSerializer.LegacyFileExtension}");
        Directory.CreateDirectory(
            Path.GetDirectoryName(legacyPath)!);
        await File.WriteAllTextAsync(legacyPath, "<legacy />");
        var writer = new StubWriter();
        var service = CreateService(
            new StubReader(
                CreateLoadResult(
                    format: MacroConfigurationFormat.LegacyV4)),
            writer,
            new StubHotkeyRegistrationService());

        var result = await service.AutoLoadAsync(editable);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.SourcePath, Is.EqualTo(legacyPath));
            Assert.That(result.MigratedLegacyFile, Is.True);
            Assert.That(
                writer.Paths.Single(),
                Is.EqualTo(
                    Path.Combine(
                        testDirectory,
                        "autosave",
                        $"Legacy-Autosave{MacroConfigurationSerializer.CurrentFileExtension}")));
        });
    }

    [Test]
    public void ShouldDeleteUnreadableAutosave()
    {
        using var player = CreatePlayer("Broken");
        var editable = new PlayerMacroConfiguration(player);
        var currentPath = Path.Combine(
            testDirectory,
            "autosave",
            $"Broken-Autosave{MacroConfigurationSerializer.CurrentFileExtension}");
        Directory.CreateDirectory(
            Path.GetDirectoryName(currentPath)!);
        File.WriteAllText(currentPath, "broken");
        var reader = new StubReader(
            new InvalidDataException("Unreadable"));
        var service = CreateService(
            reader,
            new StubWriter(),
            new StubHotkeyRegistrationService());

        Assert.That(
            async () => await service.AutoLoadAsync(editable),
            Throws.TypeOf<InvalidDataException>());
        Assert.That(File.Exists(currentPath), Is.False);
    }

    [Test]
    public void ShouldKeepReadableAutosaveWhenApplyingItFails()
    {
        using var player = CreatePlayer("Readable");
        var editable = new PlayerMacroConfiguration(player);
        var currentPath = Path.Combine(
            testDirectory,
            "autosave",
            $"Readable-Autosave{MacroConfigurationSerializer.CurrentFileExtension}");
        Directory.CreateDirectory(
            Path.GetDirectoryName(currentPath)!);
        File.WriteAllText(currentPath, "{}");
        var service = CreateService(
            new StubReader(CreateLoadResult()),
            new StubWriter(),
            new StubHotkeyRegistrationService(),
            new ThrowingMapper());

        Assert.That(
            async () => await service.AutoLoadAsync(editable),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(File.Exists(currentPath), Is.True);
    }

    private MacroConfigurationPersistenceService CreateService(
        IMacroConfigurationReader reader,
        IMacroConfigurationWriter writer,
        IHotkeyRegistrationService hotkeys,
        IPlayerMacroConfigurationMapper? mapper = null) =>
        new(
            reader,
            writer,
            mapper ?? new PlayerMacroConfigurationMapper(),
            hotkeys,
            new TestLogger(),
            testDirectory);

    private static MacroConfigurationLoadResult CreateLoadResult(
        string? description = null,
        HotkeyConfiguration? hotkey = null,
        MacroConfigurationFormat format =
            MacroConfigurationFormat.Current) =>
        new(
            new MacroConfiguration(
                name: "Loaded",
                description: description,
                hotkey: hotkey),
            format,
            format == MacroConfigurationFormat.Current
                ? MacroConfigurationSerializer.CurrentVersion
                : "4.11",
            ImmutableArray<MacroConfigurationWarning>.Empty);

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

    private sealed class StubReader : IMacroConfigurationReader
    {
        private readonly Exception? exception;
        private readonly MacroConfigurationLoadResult? result;

        public StubReader(MacroConfigurationLoadResult result)
        {
            this.result = result;
        }

        public StubReader(Exception exception)
        {
            this.exception = exception;
        }

        public Task<MacroConfigurationLoadResult> LoadAsync(
            string filePath,
            CancellationToken cancellationToken = default) =>
            exception is null
                ? Task.FromResult(result!)
                : Task.FromException<MacroConfigurationLoadResult>(
                    exception);
    }

    private sealed class StubWriter : IMacroConfigurationWriter
    {
        public List<MacroConfiguration> Configurations { get; } = [];

        public List<string> Paths { get; } = [];

        public Task SaveAsync(
            MacroConfiguration configuration,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            Configurations.Add(configuration);
            Paths.Add(filePath);
            return Task.CompletedTask;
        }
    }

    private sealed class StubHotkeyRegistrationService :
        IHotkeyRegistrationService
    {
        public bool RegistrationSucceeds { get; init; } = true;

        public List<Hotkey> Registered { get; } = [];

        public List<Hotkey> Unregistered { get; } = [];

        public Hotkey Find(
            Key key,
            ModifierKeys modifiers) =>
            null!;

        public bool Register(Hotkey hotkey)
        {
            Registered.Add(hotkey);
            return RegistrationSucceeds;
        }

        public bool Unregister(Hotkey hotkey)
        {
            Unregistered.Add(hotkey);
            return true;
        }
    }

    private sealed class ThrowingMapper :
        IPlayerMacroConfigurationMapper
    {
        public MacroConfiguration CreateSnapshot(
            PlayerMacroConfiguration source) =>
            throw new NotSupportedException();

        public void Apply(
            PlayerMacroConfiguration destination,
            MacroConfigurationLoadResult loaded) =>
            throw new InvalidOperationException("Apply failed");
    }
}
