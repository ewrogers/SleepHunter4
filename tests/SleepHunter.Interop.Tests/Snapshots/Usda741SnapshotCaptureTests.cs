using System.Collections.Immutable;
using System.Text;
using SleepHunter.Interop.Mappings;
using SleepHunter.Interop.Memory;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Interop.Tests.Memory;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Interop.Tests.Snapshots;

public sealed class Usda741SnapshotCaptureTests
{
    private const ulong SessionRootAddress = 0x1000;
    private const ulong SessionLinkAddress = 0x2000;
    private const ulong SessionAddress = 0x3000;
    private const ulong PlayerRootAddress = 0x1100;
    private const ulong PlayerAddress = 0x4000;
    private const ulong MapNameRootAddress = 0x1200;
    private const ulong CharacterNameAddress = 0x5000;
    private const ulong MapNameAddress = 0x5100;
    private const ulong LevelAddress = PlayerAddress + 0x10;
    private const ulong AbilityLevelAddress = PlayerAddress + 0x11;
    private const ulong CharacterClassAddress = PlayerAddress + 0x12;
    private const ulong CharacterIdAddress = PlayerAddress + 0x14;
    private const ulong CurrentHealthAddress = PlayerAddress + 0x20;
    private const ulong MaximumHealthAddress = PlayerAddress + 0x24;
    private const ulong CurrentManaAddress = PlayerAddress + 0x28;
    private const ulong MaximumManaAddress = PlayerAddress + 0x2C;
    private const ulong ActivePanelAddress = PlayerAddress + 0x30;
    private const ulong MapNumberAddress = PlayerAddress + 0x40;
    private const ulong MapXAddress = PlayerAddress + 0x44;
    private const ulong MapYAddress = PlayerAddress + 0x48;

    [Test]
    public void ShouldCaptureCompleteInWorldSnapshotAndMetrics()
    {
        var source = CreateMemoryImage();
        var timeProvider = new ManualTimeProvider();
        source.ReadStarting = (_, _) =>
            timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        var capture = CreateCapture(source, timeProvider);

        var result = capture.Capture(new SnapshotSequence(7));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Quality, Is.EqualTo(SnapshotQuality.Complete));
            Assert.That(result.Error, Is.Null);
            Assert.That(result.Snapshot?.Sequence.Value, Is.EqualTo(7));
            Assert.That(
                result.Snapshot?.Presence,
                Is.EqualTo(ClientPresence.InWorld));
            Assert.That(
                result.Snapshot?.ActivePanel,
                Is.EqualTo(ClientPanel.Inventory));
            Assert.That(
                result.Snapshot?.Character,
                Is.EqualTo(
                    new CharacterSnapshot(
                        CharacterClass.Wizard,
                        level: 99,
                        abilityLevel: 50,
                        name: "Aislinn",
                        characterId: 1234)));
            Assert.That(
                result.Snapshot?.Vitals,
                Is.EqualTo(new VitalsSnapshot(1000, 1200, 500, 600)));
            Assert.That(
                result.Snapshot?.Location,
                Is.EqualTo(new MapLocationSnapshot(1, "Mileth", 50, 60)));
            Assert.That(
                result.Metrics.Sections.Select(section => section.Section),
                Is.EqualTo(
                    new[]
                    {
                        SnapshotSection.Presence,
                        SnapshotSection.Character,
                        SnapshotSection.Vitals,
                        SnapshotSection.ClientState,
                        SnapshotSection.Location,
                        SnapshotSection.Coherence
                    }));
            Assert.That(
                result.Metrics.Sections.All(section => section.Succeeded),
                Is.True);
            Assert.That(
                result.Metrics.Reads.FailedReadCount,
                Is.Zero);
            Assert.That(
                result.Metrics.Reads.BytesRead,
                Is.EqualTo(result.Metrics.Reads.RequestedBytes));
            Assert.That(
                result.Metrics.Duration,
                Is.EqualTo(
                    TimeSpan.FromMilliseconds(
                        result.Metrics.Reads.TransportReadCount)));
            Assert.That(
                result.Snapshot?.CaptureStartedAt,
                Is.EqualTo(result.Metrics.CaptureStartedAt));
            Assert.That(
                result.Snapshot?.CaptureCompletedAt,
                Is.EqualTo(result.Metrics.CaptureCompletedAt));
        });
    }

    [TestCase(0x00, CharacterClass.Peasant)]
    [TestCase(0x01, CharacterClass.Warrior)]
    [TestCase(0x02, CharacterClass.Wizard)]
    [TestCase(0x04, CharacterClass.Priest)]
    [TestCase(0x08, CharacterClass.Rogue)]
    [TestCase(0x10, CharacterClass.Monk)]
    public void ShouldTranslateDocumentedCharacterClasses(
        byte rawValue,
        CharacterClass expected)
    {
        var source = CreateMemoryImage();
        source.Write(new MemoryAddress(CharacterClassAddress), rawValue);
        var capture = CreateCapture(source);

        var result = capture.Capture(new SnapshotSequence(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Snapshot?.Character?.Class, Is.EqualTo(expected));
        });
    }

    [Test]
    public void ShouldPublishCompleteLoggedOutSnapshotForNullSession()
    {
        var source = CreateMemoryImage();
        source.WriteUInt32(new MemoryAddress(SessionRootAddress), 0);
        var capture = CreateCapture(source);

        var result = capture.Capture(new SnapshotSequence(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                result.Snapshot?.Presence,
                Is.EqualTo(ClientPresence.LoggedOut));
            Assert.That(
                result.Snapshot?.ActivePanel,
                Is.EqualTo(ClientPanel.Unknown));
            Assert.That(result.Snapshot?.Character, Is.Null);
            Assert.That(result.Snapshot?.Vitals, Is.Null);
            Assert.That(result.Snapshot?.Location, Is.Null);
            Assert.That(result.Metrics.Sections.Length, Is.EqualTo(1));
            Assert.That(
                result.Metrics.Sections[0].Section,
                Is.EqualTo(SnapshotSection.Presence));
            Assert.That(result.Metrics.Sections[0].Succeeded, Is.True);
        });
    }

    [Test]
    public void ShouldRejectPartialMappedReadWithoutPublishingSnapshot()
    {
        var source = CreateMemoryImage();
        source.Clear(new MemoryAddress(CurrentManaAddress), sizeof(uint));
        var capture = CreateCapture(source);

        var result = capture.Capture(new SnapshotSequence(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Snapshot, Is.Null);
            Assert.That(result.Quality, Is.EqualTo(SnapshotQuality.Partial));
            Assert.That(
                result.Error?.Failure,
                Is.EqualTo(SnapshotCaptureFailure.MappingReadFailed));
            Assert.That(
                result.Error?.Section,
                Is.EqualTo(SnapshotSection.Vitals));
            Assert.That(result.Error?.VariableKey, Is.EqualTo("CurrentMana"));
            Assert.That(
                result.Error?.ReadError?.MemoryError?.Failure,
                Is.EqualTo(MemoryReadFailure.TransportFailure));
            Assert.That(result.Metrics.Reads.FailedReadCount, Is.EqualTo(1));
            Assert.That(result.Metrics.Sections[^1].Succeeded, Is.False);
        });
    }

    [Test]
    public void ShouldRejectInvalidDomainValueAsIncoherent()
    {
        var source = CreateMemoryImage();
        source.Write(new MemoryAddress(CharacterClassAddress), 0x03);
        var capture = CreateCapture(source);

        var result = capture.Capture(new SnapshotSequence(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Quality, Is.EqualTo(SnapshotQuality.Incoherent));
            Assert.That(
                result.Error?.Failure,
                Is.EqualTo(SnapshotCaptureFailure.InvalidValue));
            Assert.That(
                result.Error?.Section,
                Is.EqualTo(SnapshotSection.Character));
            Assert.That(
                result.Error?.VariableKey,
                Is.EqualTo("CharacterClass"));
        });
    }

    [Test]
    public void ShouldRejectChangedCharacterOwnership()
    {
        var source = CreateMemoryImage();
        var characterIdReads = 0;
        source.ReadStarting = (address, _) =>
        {
            if (address.Value != CharacterIdAddress)
            {
                return;
            }

            characterIdReads++;
            if (characterIdReads == 2)
            {
                source.WriteUInt32(address, 5678);
            }
        };
        var capture = CreateCapture(source);

        var result = capture.Capture(new SnapshotSequence(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Quality, Is.EqualTo(SnapshotQuality.Incoherent));
            Assert.That(
                result.Error?.Failure,
                Is.EqualTo(SnapshotCaptureFailure.OwnershipChanged));
            Assert.That(
                result.Error?.Section,
                Is.EqualTo(SnapshotSection.Coherence));
            Assert.That(result.Error?.VariableKey, Is.EqualTo("CharacterId"));
            Assert.That(
                result.Metrics.Sections[^1].Section,
                Is.EqualTo(SnapshotSection.Coherence));
            Assert.That(result.Metrics.Sections[^1].Succeeded, Is.False);
        });
    }

    [Test]
    public void ShouldRejectChangedSessionRoot()
    {
        const ulong replacementLinkAddress = 0x2200;
        const ulong replacementSessionAddress = 0x3300;

        var source = CreateMemoryImage();
        source.WriteUInt32(
            new MemoryAddress(replacementLinkAddress),
            (uint)replacementSessionAddress);
        var sessionRootReads = 0;
        source.ReadStarting = (address, _) =>
        {
            if (address.Value != SessionRootAddress)
            {
                return;
            }

            sessionRootReads++;
            if (sessionRootReads == 2)
            {
                source.WriteUInt32(
                    address,
                    (uint)replacementLinkAddress);
            }
        };
        var capture = CreateCapture(source);

        var result = capture.Capture(new SnapshotSequence(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Quality, Is.EqualTo(SnapshotQuality.Incoherent));
            Assert.That(
                result.Error?.Failure,
                Is.EqualTo(SnapshotCaptureFailure.OwnershipChanged));
            Assert.That(
                result.Error?.Section,
                Is.EqualTo(SnapshotSection.Coherence));
            Assert.That(
                result.Error?.VariableKey,
                Is.EqualTo("WorldUserFunc"));
        });
    }

    [Test]
    public void ShouldRejectChangedLocation()
    {
        var source = CreateMemoryImage();
        var mapNumberReads = 0;
        source.ReadStarting = (address, _) =>
        {
            if (address.Value != MapNumberAddress)
            {
                return;
            }

            mapNumberReads++;
            if (mapNumberReads == 2)
            {
                source.WriteUInt32(address, 2);
            }
        };
        var capture = CreateCapture(source);

        var result = capture.Capture(new SnapshotSequence(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Quality, Is.EqualTo(SnapshotQuality.Incoherent));
            Assert.That(
                result.Error?.Failure,
                Is.EqualTo(SnapshotCaptureFailure.StateChanged));
            Assert.That(
                result.Error?.Section,
                Is.EqualTo(SnapshotSection.Coherence));
            Assert.That(result.Error?.VariableKey, Is.EqualTo("MapNumber"));
        });
    }

    [Test]
    public void ShouldRejectUnavailableSessionAsPartialRatherThanLoggedOut()
    {
        var source = CreateMemoryImage();
        source.Clear(new MemoryAddress(SessionRootAddress), sizeof(uint));
        var capture = CreateCapture(source);

        var result = capture.Capture(new SnapshotSequence(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Quality, Is.EqualTo(SnapshotQuality.Partial));
            Assert.That(
                result.Error?.Failure,
                Is.EqualTo(SnapshotCaptureFailure.MappingReadFailed));
            Assert.That(result.Error?.VariableKey, Is.EqualTo("WorldUserFunc"));
        });
    }

    [Test]
    public void ShouldRejectConcurrentCaptureWithoutReadingMemory()
    {
        var source = CreateMemoryImage();
        using var blockingSource = new BlockingMemorySource(source);
        var capture = CreateCapture(blockingSource);
        var firstCapture = Task.Run(
            () => capture.Capture(new SnapshotSequence(1)));

        SnapshotCaptureResult secondResult;
        try
        {
            Assert.That(
                blockingSource.WaitUntilBlocked(TimeSpan.FromSeconds(5)),
                Is.True);
            secondResult = capture.Capture(new SnapshotSequence(2));
        }
        finally
        {
            blockingSource.Release();
        }

        var firstResult = firstCapture.GetAwaiter().GetResult();
        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Succeeded, Is.True);
            Assert.That(secondResult.Succeeded, Is.False);
            Assert.That(
                secondResult.Quality,
                Is.EqualTo(SnapshotQuality.Unknown));
            Assert.That(
                secondResult.Error?.Failure,
                Is.EqualTo(
                    SnapshotCaptureFailure.CaptureAlreadyInProgress));
            Assert.That(
                secondResult.Metrics.Reads.TransportReadCount,
                Is.Zero);
            Assert.That(secondResult.Metrics.Sections, Is.Empty);
        });
    }

    [Test]
    public void ShouldValidateCheckedInUsdaSchema()
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Data",
            "Versions.xml");
        using var stream = File.OpenRead(path);
        var map = ClientMemoryMapLoader.Load(
            stream,
            Usda741SnapshotCapture.SupportedVersion);
        var client = new ClientIdentity(
            "process:1234",
            Usda741SnapshotCapture.SupportedVersion);

        Assert.DoesNotThrow(
            () => _ = new Usda741SnapshotCapture(
                client,
                map,
                new MemoryImageSource(),
                MemoryReadLimits.Client32Bit,
                new MacroClock(new ManualTimeProvider())));
    }

    [Test]
    public void ShouldRejectWrongOrIncompleteSchemaAtComposition()
    {
        var variables = CreateVariables();
        var wrongVersion = new ClientMemoryMap(
            "Other",
            PointerWidth.Bit32,
            variables);
        var incomplete = new ClientMemoryMap(
            Usda741SnapshotCapture.SupportedVersion,
            PointerWidth.Bit32,
            variables.Where(variable => variable.Key != "CurrentMana"));
        var client = new ClientIdentity(
            "process:1234",
            Usda741SnapshotCapture.SupportedVersion);
        var source = new MemoryImageSource();
        var limits = MemoryReadLimits.Client32Bit;
        var clock = new MacroClock(new ManualTimeProvider());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                () => _ = new Usda741SnapshotCapture(
                    client,
                    wrongVersion,
                    source,
                    limits,
                    clock));
            Assert.Throws<ArgumentException>(
                () => _ = new Usda741SnapshotCapture(
                    client,
                    incomplete,
                    source,
                    limits,
                    clock));
        });
    }

    private static Usda741SnapshotCapture CreateCapture(
        IProcessMemorySource source,
        ManualTimeProvider? timeProvider = null)
    {
        var client = new ClientIdentity(
            "process:1234",
            Usda741SnapshotCapture.SupportedVersion);
        return new Usda741SnapshotCapture(
            client,
            new ClientMemoryMap(
                Usda741SnapshotCapture.SupportedVersion,
                PointerWidth.Bit32,
                CreateVariables()),
            source,
            MemoryReadLimits.Client32Bit,
            new MacroClock(timeProvider ?? new ManualTimeProvider()));
    }

    private static MemoryImageSource CreateMemoryImage()
    {
        var source = new MemoryImageSource();
        source.WriteUInt32(
            new MemoryAddress(SessionRootAddress),
            (uint)SessionLinkAddress);
        source.WriteUInt32(
            new MemoryAddress(SessionLinkAddress),
            (uint)SessionAddress);
        source.WriteUInt32(
            new MemoryAddress(PlayerRootAddress),
            (uint)PlayerAddress);
        source.WriteUInt32(
            new MemoryAddress(MapNameRootAddress),
            (uint)MapNameAddress);
        WriteFixedAscii(
            source,
            new MemoryAddress(CharacterNameAddress),
            "Aislinn",
            length: 16);
        WriteFixedAscii(
            source,
            new MemoryAddress(MapNameAddress),
            "Mileth",
            length: 32);
        source.Write(new MemoryAddress(LevelAddress), 99);
        source.Write(new MemoryAddress(AbilityLevelAddress), 50);
        source.Write(new MemoryAddress(CharacterClassAddress), 0x02);
        source.WriteUInt32(new MemoryAddress(CharacterIdAddress), 1234);
        source.WriteUInt32(new MemoryAddress(CurrentHealthAddress), 1000);
        source.WriteUInt32(new MemoryAddress(MaximumHealthAddress), 1200);
        source.WriteUInt32(new MemoryAddress(CurrentManaAddress), 500);
        source.WriteUInt32(new MemoryAddress(MaximumManaAddress), 600);
        source.Write(new MemoryAddress(ActivePanelAddress), 0);
        source.WriteUInt32(new MemoryAddress(MapNumberAddress), 1);
        source.WriteInt32(new MemoryAddress(MapXAddress), 50);
        source.WriteInt32(new MemoryAddress(MapYAddress), 60);
        return source;
    }

    private static MemoryVariableDefinition[] CreateVariables() =>
    [
        new(
            "WorldUserFunc",
            new PointerChain(
                new MemoryAddress(SessionRootAddress),
                ImmutableArray.Create(
                    new PointerOffset(0),
                    new PointerOffset(0))),
            MemoryValueKind.Unsigned32),
        new(
            "CharacterName",
            new PointerChain(new MemoryAddress(CharacterNameAddress)),
            MemoryValueKind.Text,
            maximumLength: 16),
        Dynamic("Level", 0x10, MemoryValueKind.Byte),
        Dynamic("AbilityLevel", 0x11, MemoryValueKind.Byte),
        Dynamic("CharacterClass", 0x12, MemoryValueKind.Byte),
        Dynamic("CharacterId", 0x14, MemoryValueKind.Unsigned32),
        Dynamic("CurrentHealth", 0x20, MemoryValueKind.Unsigned32),
        Dynamic("MaximumHealth", 0x24, MemoryValueKind.Unsigned32),
        Dynamic("CurrentMana", 0x28, MemoryValueKind.Unsigned32),
        Dynamic("MaximumMana", 0x2C, MemoryValueKind.Unsigned32),
        Dynamic("ActivePanel", 0x30, MemoryValueKind.Byte),
        Dynamic("MapNumber", 0x40, MemoryValueKind.Unsigned32),
        Dynamic("MapX", 0x44, MemoryValueKind.Signed32),
        Dynamic("MapY", 0x48, MemoryValueKind.Signed32),
        new(
            "MapName",
            new PointerChain(
                new MemoryAddress(MapNameRootAddress),
                ImmutableArray.Create(new PointerOffset(0))),
            MemoryValueKind.Text,
            maximumLength: 32)
    ];

    private static MemoryVariableDefinition Dynamic(
        string key,
        long offset,
        MemoryValueKind kind) =>
        new(
            key,
            new PointerChain(
                new MemoryAddress(PlayerRootAddress),
                ImmutableArray.Create(new PointerOffset(offset))),
            kind);

    private static void WriteFixedAscii(
        MemoryImageSource source,
        MemoryAddress address,
        string value,
        int length)
    {
        var valueBytes = Encoding.ASCII.GetBytes(value);
        if (valueBytes.Length >= length)
        {
            throw new ArgumentException(
                "The test string must leave room for a terminator.",
                nameof(value));
        }

        var buffer = new byte[length];
        valueBytes.CopyTo(buffer, 0);
        source.Write(address, buffer);
    }

    private sealed class BlockingMemorySource : IProcessMemorySource, IDisposable
    {
        private readonly IProcessMemorySource inner;
        private readonly ManualResetEventSlim readStarted = new();
        private readonly ManualResetEventSlim release = new();
        private int shouldBlock = 1;

        public BlockingMemorySource(IProcessMemorySource inner)
        {
            ArgumentNullException.ThrowIfNull(inner);
            this.inner = inner;
        }

        public MemorySourceReadResult Read(
            MemoryAddress address,
            Span<byte> destination)
        {
            if (Interlocked.Exchange(ref shouldBlock, 0) != 0)
            {
                readStarted.Set();
                if (!release.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException(
                        "The blocked test memory read was not released.");
                }
            }

            return inner.Read(address, destination);
        }

        public bool WaitUntilBlocked(TimeSpan timeout) =>
            readStarted.Wait(timeout);

        public void Release() => release.Set();

        public void Dispose()
        {
            readStarted.Dispose();
            release.Dispose();
        }
    }
}
