using System.Buffers.Binary;
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

public sealed class ClientSnapshotCaptureTests
{
    private const ulong SessionRootAddress = 0x1000;
    private const ulong SessionLinkAddress = 0x2000;
    private const ulong SessionAddress = 0x3000;
    private const ulong PlayerRootAddress = 0x1100;
    private const ulong PlayerAddress = 0x4000;
    private const ulong MapNameRootAddress = 0x1200;
    private const ulong InventoryRootAddress = 0x1300;
    private const ulong EquipmentSnapshotRootAddress = 0x1400;
    private const ulong EquipmentRootAddress = 0x1500;
    private const ulong SkillbookRootAddress = 0x1600;
    private const ulong SpellbookRootAddress = 0x1700;
    private const ulong SkillbookPanesRootAddress = 0x1800;
    private const ulong SpellbookPanesRootAddress = 0x1900;
    private const ulong SkillbookPaneCapacityAddress = 0x1A00;
    private const ulong SpellbookPaneCapacityAddress = 0x1A04;
    private const ulong CharacterNameAddress = 0x5000;
    private const ulong MapNameAddress = 0x5100;
    private const ulong InventoryAddress = 0x6000;
    private const ulong EquipmentSnapshotAddress = 0xA000;
    private const ulong EquipmentAddress = 0xB000;
    private const ulong SkillbookAddress = 0xC000;
    private const ulong SpellbookAddress = 0x18000;
    private const ulong SkillbookPaneTableAddress = 0x24000;
    private const ulong SpellbookPaneTableAddress = 0x24200;
    private const ulong SkillPaneAddress = 0x25000;
    private const ulong SpellPaneAddress = 0x26000;
    private const ulong LevelAddress = PlayerAddress + 0x10;
    private const ulong AbilityLevelAddress = PlayerAddress + 0x11;
    private const ulong CharacterClassAddress = PlayerAddress + 0x12;
    private const ulong CharacterIdAddress = PlayerAddress + 0x14;
    private const ulong CurrentHealthAddress = PlayerAddress + 0x20;
    private const ulong MaximumHealthAddress = PlayerAddress + 0x24;
    private const ulong CurrentManaAddress = PlayerAddress + 0x28;
    private const ulong MaximumManaAddress = PlayerAddress + 0x2C;
    private const ulong ActivePanelAddress = PlayerAddress + 0x30;
    private const ulong InventoryExpandedAddress = PlayerAddress + 0x31;
    private const ulong UserChattingAddress = PlayerAddress + 0x32;
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

        var result = capture.Capture(
            new SnapshotSequence(7),
            SnapshotCaptureSections.All);

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
            Assert.That(result.Snapshot?.IsInventoryExpanded, Is.True);
            Assert.That(result.Snapshot?.IsUserChatting, Is.False);
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
                result.Snapshot?.Inventory,
                Is.EqualTo(
                    new InventorySnapshot(
                    [
                        new InventoryItemSnapshot(1, "Holy Diana"),
                        new InventoryItemSnapshot(3, "Gnarl")
                    ])));
            Assert.That(
                result.Snapshot?.Equipment,
                Is.EqualTo(
                    new EquipmentSnapshot(
                        "Holy Diana",
                        "Dragon Shield")));
            Assert.That(
                result.Snapshot?.Skillbook,
                Is.EqualTo(
                    new SkillbookSnapshot(
                    [
                        new SkillSnapshot(
                            "Assail",
                            slot: 1,
                            currentLevel: 3,
                            maximumLevel: 100,
                            manaCost: 0,
                            TimeSpan.Zero,
                            isAssail: true,
                            isActionDelayed: true)
                    ])));
            Assert.That(
                result.Snapshot?.Spellbook,
                Is.EqualTo(
                    new SpellbookSnapshot(
                    [
                        new SpellSnapshot(
                            "ard cradh",
                            slot: 73,
                            currentLevel: 7,
                            maximumLevel: 100,
                            castLines: 4,
                            manaCost: 500,
                            TimeSpan.Zero,
                            isActionDelayed: true)
                    ])));
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
                        SnapshotSection.Inventory,
                        SnapshotSection.Equipment,
                        SnapshotSection.Skillbook,
                        SnapshotSection.Spellbook,
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

    [Test]
    public void ShouldCaptureUserChattingState()
    {
        var source = CreateMemoryImage();
        source.Write(new MemoryAddress(UserChattingAddress), 1);
        var capture = CreateCapture(source);

        var result = capture.Capture(new SnapshotSequence(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Snapshot?.IsUserChatting, Is.True);
        });
    }

    [Test]
    public void ShouldRejectUserChattingStateChangedDuringCapture()
    {
        var source = CreateMemoryImage();
        var userChattingReads = 0;
        source.ReadStarting = (address, _) =>
        {
            if (address.Value != UserChattingAddress)
            {
                return;
            }

            userChattingReads++;
            if (userChattingReads == 2)
            {
                source.Write(address, 1);
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
            Assert.That(
                result.Error?.VariableKey,
                Is.EqualTo("UserChatting"));
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
            Assert.That(result.Snapshot?.IsInventoryExpanded, Is.False);
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
    public void ShouldPublishMapTransitionOnlyAfterNameAndNumberAgree()
    {
        var source = CreateMemoryImage();
        var capture = CreateCapture(source);

        var initial = capture.Capture(new SnapshotSequence(1));
        source.WriteUInt32(new MemoryAddress(MapNumberAddress), 2);
        source.WriteInt32(new MemoryAddress(MapXAddress), 10);
        source.WriteInt32(new MemoryAddress(MapYAddress), 20);
        var coordinateFirst = capture.Capture(
            new SnapshotSequence(2));
        WriteFixedAscii(
            source,
            new MemoryAddress(MapNameAddress),
            "Abel",
            length: 32);
        var completeTransition = capture.Capture(
            new SnapshotSequence(3));

        Assert.Multiple(() =>
        {
            Assert.That(initial.Succeeded, Is.True);
            Assert.That(coordinateFirst.Succeeded, Is.False);
            Assert.That(
                coordinateFirst.Quality,
                Is.EqualTo(SnapshotQuality.Incoherent));
            Assert.That(
                coordinateFirst.Error?.Failure,
                Is.EqualTo(
                    SnapshotCaptureFailure.LocationTransition));
            Assert.That(
                coordinateFirst.Error?.VariableKey,
                Is.EqualTo("MapNumber"));
            Assert.That(
                coordinateFirst.Metrics.Sections.Single(
                    section =>
                        section.Section ==
                        SnapshotSection.Coherence).Succeeded,
                Is.False);
            Assert.That(completeTransition.Succeeded, Is.True);
            Assert.That(
                completeTransition.Snapshot?.Location,
                Is.EqualTo(
                    new MapLocationSnapshot(2, "Abel", 10, 20)));
        });
    }

    [Test]
    public void ShouldAllowTwoMapsWithTheSameDisplayName()
    {
        var source = CreateMemoryImage();
        var capture = CreateCapture(source);

        var initial = capture.Capture(new SnapshotSequence(1));
        source.WriteUInt32(new MemoryAddress(MapNumberAddress), 2);
        source.WriteInt32(new MemoryAddress(MapXAddress), 10);
        source.WriteInt32(new MemoryAddress(MapYAddress), 20);
        var firstObservation = capture.Capture(
            new SnapshotSequence(2));
        var confirmedObservation = capture.Capture(
            new SnapshotSequence(3));

        Assert.Multiple(() =>
        {
            Assert.That(initial.Succeeded, Is.True);
            Assert.That(firstObservation.Succeeded, Is.False);
            Assert.That(
                firstObservation.Error?.Failure,
                Is.EqualTo(
                    SnapshotCaptureFailure.LocationTransition));
            Assert.That(confirmedObservation.Succeeded, Is.True);
            Assert.That(
                confirmedObservation.Snapshot?.Location,
                Is.EqualTo(
                    new MapLocationSnapshot(
                        2,
                        "Mileth",
                        10,
                        20)));
        });
    }

    [Test]
    public void ShouldTreatMissingMapNameAsTransitionAfterStableMap()
    {
        var source = CreateMemoryImage();
        var capture = CreateCapture(source);

        var initial = capture.Capture(new SnapshotSequence(1));
        source.Clear(new MemoryAddress(MapNameAddress), 32);
        var transition = capture.Capture(
            new SnapshotSequence(2));

        Assert.Multiple(() =>
        {
            Assert.That(initial.Succeeded, Is.True);
            Assert.That(transition.Succeeded, Is.False);
            Assert.That(
                transition.Quality,
                Is.EqualTo(SnapshotQuality.Incoherent));
            Assert.That(
                transition.Error?.Failure,
                Is.EqualTo(
                    SnapshotCaptureFailure.LocationTransition));
            Assert.That(
                transition.Error?.VariableKey,
                Is.EqualTo("MapName"));
            Assert.That(transition.Error?.ReadError, Is.Not.Null);
        });
    }

    [Test]
    public void ShouldReportMissingMapNameBeforeAnyStableMap()
    {
        var source = CreateMemoryImage();
        source.Clear(new MemoryAddress(MapNameAddress), 32);
        var capture = CreateCapture(source);

        var result = capture.Capture(new SnapshotSequence(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Error?.Failure,
                Is.EqualTo(
                    SnapshotCaptureFailure.MappingReadFailed));
            Assert.That(
                result.Error?.VariableKey,
                Is.EqualTo("MapName"));
        });
    }

    [Test]
    public void ShouldRejectChangedInventoryDisplayMode()
    {
        var source = CreateMemoryImage();
        var inventoryExpandedReads = 0;
        source.ReadStarting = (address, _) =>
        {
            if (address.Value != InventoryExpandedAddress)
            {
                return;
            }

            inventoryExpandedReads++;
            if (inventoryExpandedReads == 2)
            {
                source.Write(address, 0);
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
            Assert.That(
                result.Error?.VariableKey,
                Is.EqualTo("InventoryExpanded"));
        });
    }

    [Test]
    public void ShouldRejectMissingInventoryDisplayMode()
    {
        var source = CreateMemoryImage();
        source.Clear(new MemoryAddress(InventoryExpandedAddress), 1);
        var capture = CreateCapture(source);

        var result = capture.Capture(new SnapshotSequence(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Quality, Is.EqualTo(SnapshotQuality.Partial));
            Assert.That(
                result.Error?.Failure,
                Is.EqualTo(SnapshotCaptureFailure.MappingReadFailed));
            Assert.That(
                result.Error?.Section,
                Is.EqualTo(SnapshotSection.ClientState));
            Assert.That(
                result.Error?.VariableKey,
                Is.EqualTo("InventoryExpanded"));
        });
    }

    [Test]
    public void ShouldOmitUnrequestedCollections()
    {
        var source = CreateMemoryImage();
        source.Clear(
            new MemoryAddress(InventoryRootAddress),
            sizeof(uint));
        source.Clear(
            new MemoryAddress(EquipmentSnapshotRootAddress),
            sizeof(uint));
        source.Clear(
            new MemoryAddress(EquipmentRootAddress),
            sizeof(uint));
        var capture = CreateCapture(source);

        var result = capture.Capture(new SnapshotSequence(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Snapshot?.Inventory, Is.Null);
            Assert.That(result.Snapshot?.Equipment, Is.Null);
            Assert.That(result.Snapshot?.Skillbook, Is.Null);
            Assert.That(result.Snapshot?.Spellbook, Is.Null);
            Assert.That(
                result.Metrics.Sections.Any(
                    section =>
                        section.Section is
                            SnapshotSection.Inventory or
                            SnapshotSection.Equipment or
                            SnapshotSection.Skillbook or
                            SnapshotSection.Spellbook),
                Is.False);
        });
    }

    [Test]
    public void ShouldUseCompactAbilityFallbackWhenPaneTableChanges()
    {
        var source = CreateMemoryImage();
        var skillPointerReads = 0;
        source.ReadStarting = (address, _) =>
        {
            if (address.Value != SkillbookPaneTableAddress)
            {
                return;
            }

            skillPointerReads++;
            if (skillPointerReads == 2)
            {
                source.WriteUInt32(address, 0);
            }
        };
        var capture = CreateCapture(source);

        var result = capture.Capture(
            new SnapshotSequence(1),
            SnapshotCaptureSections.Skillbook);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                result.Snapshot?.Skillbook?.Skills.Length,
                Is.EqualTo(1));
            Assert.That(
                result.Snapshot?.Skillbook?.Skills[0].Name,
                Is.EqualTo("Assail"));
            Assert.That(
                result.Snapshot?.Skillbook?.Skills[0].IsActionDelayed,
                Is.False);
            Assert.That(skillPointerReads, Is.EqualTo(2));
        });
    }

    [Test]
    public void ShouldRejectChangedInventoryRoot()
    {
        const ulong replacementInventoryAddress = 0xC000;

        var source = CreateMemoryImage();
        var inventoryRootReads = 0;
        source.ReadStarting = (address, _) =>
        {
            if (address.Value != InventoryRootAddress)
            {
                return;
            }

            inventoryRootReads++;
            if (inventoryRootReads == 2)
            {
                source.WriteUInt32(
                    address,
                    (uint)replacementInventoryAddress);
            }
        };
        var capture = CreateCapture(source);

        var result = capture.Capture(
            new SnapshotSequence(1),
            SnapshotCaptureSections.Inventory);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Quality, Is.EqualTo(SnapshotQuality.Incoherent));
            Assert.That(
                result.Error?.Failure,
                Is.EqualTo(SnapshotCaptureFailure.StateChanged));
            Assert.That(
                result.Error?.Section,
                Is.EqualTo(SnapshotSection.Inventory));
            Assert.That(result.Error?.VariableKey, Is.EqualTo("Inventory"));
        });
    }

    [Test]
    public void ShouldUseCompactEquipmentFallback()
    {
        var source = CreateMemoryImage();
        source.Clear(
            new MemoryAddress(EquipmentSnapshotAddress),
            ClientEquipmentParser.RichSnapshotSize);
        var capture = CreateCapture(source);

        var result = capture.Capture(
            new SnapshotSequence(1),
            SnapshotCaptureSections.Equipment);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(
                result.Snapshot?.Equipment,
                Is.EqualTo(
                    new EquipmentSnapshot(
                        "Holy Diana",
                        "Dragon Shield")));
            Assert.That(
                result.Metrics.Reads.FailedReadCount,
                Is.EqualTo(1));
            Assert.That(
                result.Metrics.Sections.Single(
                    section => section.Section == SnapshotSection.Equipment)
                    .Succeeded,
                Is.True);
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
    public void ShouldValidateCheckedInClientSchema()
    {
        var path = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Data",
            "ClientLayout.xml");
        using var stream = File.OpenRead(path);
        var map = ClientMemoryMapLoader.Load(stream);
        var client = new ClientIdentity("process:1234");

        Assert.DoesNotThrow(
            () => _ = new ClientSnapshotCapture(
                client,
                map,
                new MemoryImageSource(),
                MemoryReadLimits.Client32Bit,
                new MacroClock(new ManualTimeProvider())));
    }

    [Test]
    public void ShouldRejectIncompleteOrInvalidSchemaAtComposition()
    {
        var variables = CreateVariables();
        var incomplete = new ClientMemoryMap(
            PointerWidth.Bit32,
            variables.Where(variable => variable.Key != "CurrentMana"));
        var invalidLayout = new ClientMemoryMap(
            PointerWidth.Bit32,
            variables.Select(
                variable => variable.Key == "Inventory"
                    ? new MemoryVariableDefinition(
                        variable.Key,
                        variable.Address,
                        variable.ValueKind,
                        variable.MaximumLength,
                        recordSize: variable.RecordSize - 1,
                        capacity: variable.Capacity,
                        search: variable.Search)
                    : variable));
        var invalidAbilityLayout = new ClientMemoryMap(
            PointerWidth.Bit32,
            variables.Select(
                variable => variable.Key == "Spellbook"
                    ? new MemoryVariableDefinition(
                        variable.Key,
                        variable.Address,
                        variable.ValueKind,
                        variable.MaximumLength,
                        recordSize: variable.RecordSize - 1,
                        capacity: variable.Capacity,
                        search: variable.Search)
                    : variable));
        var client = new ClientIdentity("process:1234");
        var source = new MemoryImageSource();
        var limits = MemoryReadLimits.Client32Bit;
        var clock = new MacroClock(new ManualTimeProvider());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(
                () => _ = new ClientSnapshotCapture(
                    client,
                    incomplete,
                    source,
                    limits,
                    clock));
            Assert.Throws<ArgumentException>(
                () => _ = new ClientSnapshotCapture(
                    client,
                    invalidLayout,
                    source,
                    limits,
                    clock));
            Assert.Throws<ArgumentException>(
                () => _ = new ClientSnapshotCapture(
                    client,
                    invalidAbilityLayout,
                    source,
                    limits,
                    clock));
        });
    }

    [Test]
    public void ShouldRejectUnsupportedSectionSelection()
    {
        var capture = CreateCapture(CreateMemoryImage());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => capture.Capture(
                new SnapshotSequence(1),
                (SnapshotCaptureSections)(1 << 10)));
    }

    private static ClientSnapshotCapture CreateCapture(
        IProcessMemorySource source,
        ManualTimeProvider? timeProvider = null)
    {
        var client = new ClientIdentity("process:1234");
        return new ClientSnapshotCapture(
            client,
            new ClientMemoryMap(
                PointerWidth.Bit32,
                CreateVariables()),
            source,
            MemoryReadLimits.Client32Bit,
            new MacroClock(timeProvider ?? new ManualTimeProvider()),
            CreateAbilityCatalog());
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
        source.WriteUInt32(
            new MemoryAddress(InventoryRootAddress),
            (uint)InventoryAddress);
        source.WriteUInt32(
            new MemoryAddress(EquipmentSnapshotRootAddress),
            (uint)EquipmentSnapshotAddress);
        source.WriteUInt32(
            new MemoryAddress(EquipmentRootAddress),
            (uint)EquipmentAddress);
        source.WriteUInt32(
            new MemoryAddress(SkillbookRootAddress),
            (uint)SkillbookAddress);
        source.WriteUInt32(
            new MemoryAddress(SpellbookRootAddress),
            (uint)SpellbookAddress);
        source.WriteUInt32(
            new MemoryAddress(SkillbookPanesRootAddress),
            (uint)SkillbookPaneTableAddress);
        source.WriteUInt32(
            new MemoryAddress(SpellbookPanesRootAddress),
            (uint)SpellbookPaneTableAddress);
        source.WriteInt32(
            new MemoryAddress(SkillbookPaneCapacityAddress),
            1);
        source.WriteInt32(
            new MemoryAddress(SpellbookPaneCapacityAddress),
            1);
        source.WriteUInt32(
            new MemoryAddress(SkillbookPaneTableAddress),
            (uint)SkillPaneAddress);
        source.WriteUInt32(
            new MemoryAddress(SpellbookPaneTableAddress),
            (uint)SpellPaneAddress);
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
        source.Write(new MemoryAddress(InventoryExpandedAddress), 1);
        source.Write(new MemoryAddress(UserChattingAddress), 0);
        source.WriteUInt32(new MemoryAddress(MapNumberAddress), 1);
        source.WriteInt32(new MemoryAddress(MapXAddress), 50);
        source.WriteInt32(new MemoryAddress(MapYAddress), 60);
        var inventory = new byte[
            ClientInventoryParser.RecordSize *
            ClientInventoryParser.RecordCount];
        WriteInventoryItem(inventory, slot: 1, "Holy Diana");
        WriteInventoryItem(inventory, slot: 3, "Gnarl");
        WriteInventoryItem(inventory, slot: 60, "Gold");
        source.Write(new MemoryAddress(InventoryAddress), inventory);

        var equipment = new byte[
            ClientEquipmentParser.RichSnapshotSize];
        WriteRichEquipmentItem(
            equipment,
            slotIndex: 0,
            rawSprite: 0x8123,
            "Holy Diana");
        WriteRichEquipmentItem(
            equipment,
            slotIndex: 2,
            rawSprite: 0x8456,
            "Dragon Shield");
        source.Write(
            new MemoryAddress(EquipmentSnapshotAddress),
            equipment);

        var compactEquipment = new byte[
            ClientEquipmentParser.CompactNameLength *
            ClientEquipmentParser.RecordCount];
        WriteCompactEquipmentItem(
            compactEquipment,
            slotIndex: 0,
            "Holy Diana");
        WriteCompactEquipmentItem(
            compactEquipment,
            slotIndex: 2,
            "Dragon Shield");
        source.Write(new MemoryAddress(EquipmentAddress), compactEquipment);

        var compactSkills = new byte[
            ClientAbilityParser.CompactSkillRecordSize *
            ClientAbilityParser.CompactRecordCount];
        WriteCompactSkill(
            compactSkills,
            slot: 1,
            "Assail (Lev:3/100)");
        source.Write(new MemoryAddress(SkillbookAddress), compactSkills);

        var compactSpells = new byte[
            ClientAbilityParser.CompactSpellRecordSize *
            ClientAbilityParser.CompactRecordCount];
        WriteCompactSpell(
            compactSpells,
            slot: 73,
            "ard cradh (Lev:7/100)");
        source.Write(new MemoryAddress(SpellbookAddress), compactSpells);

        var skillPane = new byte[
            ClientAbilityParser.SkillPaneSnapshotSize];
        Encoding.ASCII.GetBytes("Assail (Lev:3/100)").CopyTo(
            skillPane.AsSpan(0x02));
        skillPane[0x182] = 1;
        skillPane[0x192] = 1;
        source.Write(
            new MemoryAddress(
                SkillPaneAddress +
                ClientAbilityParser.PaneSnapshotOffset),
            skillPane);

        var spellPane = new byte[
            ClientAbilityParser.SpellPaneSnapshotSize];
        spellPane[0] = 73;
        Encoding.ASCII.GetBytes("ard cradh (Lev:7/100)").CopyTo(
            spellPane.AsSpan(0x05));
        spellPane[0x105] = 4;
        spellPane[0x107] = 1;
        source.Write(
            new MemoryAddress(
                SpellPaneAddress +
                ClientAbilityParser.PaneSnapshotOffset),
            spellPane);
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
        Dynamic("InventoryExpanded", 0x31, MemoryValueKind.Byte),
        Dynamic("UserChatting", 0x32, MemoryValueKind.Byte),
        Dynamic("MapNumber", 0x40, MemoryValueKind.Unsigned32),
        Dynamic("MapX", 0x44, MemoryValueKind.Signed32),
        Dynamic("MapY", 0x48, MemoryValueKind.Signed32),
        new(
            "MapName",
            new PointerChain(
                new MemoryAddress(MapNameRootAddress),
                ImmutableArray.Create(new PointerOffset(0))),
            MemoryValueKind.Text,
            maximumLength: 32),
        Block(
            "Inventory",
            InventoryRootAddress,
            maximumLength: ClientInventoryParser.NameLength,
            recordSize: ClientInventoryParser.RecordSize,
            capacity: ClientInventoryParser.RecordCount),
        Block(
            "Equipment",
            EquipmentRootAddress,
            maximumLength: ClientEquipmentParser.CompactNameLength,
            recordSize: ClientEquipmentParser.CompactNameLength,
            capacity: ClientEquipmentParser.RecordCount),
        Block(
            "EquipmentSnapshot",
            EquipmentSnapshotRootAddress,
            maximumLength: 0,
            recordSize: ClientEquipmentParser.RichSnapshotSize,
            capacity: ClientEquipmentParser.RecordCount),
        Block(
            "Skillbook",
            SkillbookRootAddress,
            maximumLength: ClientAbilityParser.NameLength,
            recordSize: ClientAbilityParser.CompactSkillRecordSize,
            capacity: ClientAbilityParser.CompactRecordCount),
        Block(
            "Spellbook",
            SpellbookRootAddress,
            maximumLength: ClientAbilityParser.NameLength,
            recordSize: ClientAbilityParser.CompactSpellRecordSize,
            capacity: ClientAbilityParser.CompactRecordCount),
        Block(
            "SkillbookPanes",
            SkillbookPanesRootAddress,
            maximumLength: 0,
            recordSize: ClientAbilityParser.PanePointerSize,
            capacity: ClientAbilityParser.PaneRecordCount),
        new(
            "SkillbookPaneCapacity",
            new PointerChain(
                new MemoryAddress(SkillbookPaneCapacityAddress)),
            MemoryValueKind.Signed32),
        Block(
            "SpellbookPanes",
            SpellbookPanesRootAddress,
            maximumLength: 0,
            recordSize: ClientAbilityParser.PanePointerSize,
            capacity: ClientAbilityParser.PaneRecordCount),
        new(
            "SpellbookPaneCapacity",
            new PointerChain(
                new MemoryAddress(SpellbookPaneCapacityAddress)),
            MemoryValueKind.Signed32)
    ];

    private static AbilitySnapshotCatalog CreateAbilityCatalog() =>
        new(
            [
                new SkillSnapshotMetadata(
                    "Assail",
                    manaCost: 0,
                    TimeSpan.Zero,
                    isAssail: true)
            ],
            [
                new SpellSnapshotMetadata(
                    "ard cradh",
                    castLines: 3,
                    manaCost: 500,
                    TimeSpan.Zero)
            ]);

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

    private static MemoryVariableDefinition Block(
        string key,
        ulong rootAddress,
        int maximumLength,
        int recordSize,
        int capacity) =>
        new(
            key,
            new PointerChain(
                new MemoryAddress(rootAddress),
                ImmutableArray.Create(new PointerOffset(0))),
            MemoryValueKind.Binary,
            maximumLength,
            recordSize,
            capacity);

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

    private static void WriteInventoryItem(
        Span<byte> snapshot,
        int slot,
        string name)
    {
        var record = snapshot.Slice(
            (slot - 1) * ClientInventoryParser.RecordSize,
            ClientInventoryParser.RecordSize);
        record[0] = 1;
        Encoding.ASCII.GetBytes(name).CopyTo(record[5..]);
    }

    private static void WriteRichEquipmentItem(
        Span<byte> snapshot,
        int slotIndex,
        ushort rawSprite,
        string name)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(
            snapshot.Slice(slotIndex * sizeof(ushort)),
            rawSprite);
        Encoding.ASCII.GetBytes(name).CopyTo(
            snapshot.Slice(
                0x36 +
                slotIndex * ClientEquipmentParser.CompactNameLength));
    }

    private static void WriteCompactEquipmentItem(
        Span<byte> snapshot,
        int slotIndex,
        string name) =>
        Encoding.ASCII.GetBytes(name).CopyTo(
            snapshot.Slice(
                slotIndex *
                ClientEquipmentParser.CompactNameLength));

    private static void WriteCompactSkill(
        Span<byte> snapshot,
        int slot,
        string name)
    {
        var record = snapshot.Slice(
            (slot - 1) * ClientAbilityParser.CompactSkillRecordSize,
            ClientAbilityParser.CompactSkillRecordSize);
        BinaryPrimitives.WriteInt16LittleEndian(record, 1);
        Encoding.ASCII.GetBytes(name).CopyTo(record[4..]);
    }

    private static void WriteCompactSpell(
        Span<byte> snapshot,
        int slot,
        string name)
    {
        var record = snapshot.Slice(
            (slot - 1) * ClientAbilityParser.CompactSpellRecordSize,
            ClientAbilityParser.CompactSpellRecordSize);
        BinaryPrimitives.WriteInt16LittleEndian(record, 1);
        Encoding.ASCII.GetBytes(name).CopyTo(record[5..]);
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
