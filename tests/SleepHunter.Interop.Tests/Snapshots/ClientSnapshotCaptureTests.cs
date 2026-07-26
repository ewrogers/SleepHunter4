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
    private const ulong InventoryPanesRootAddress = 0x1B00;
    private const ulong GroupMemberCacheRootAddress = 0x1C00;
    private const ulong ActiveSpellEffectsRootAddress = 0x1D00;
    private const ulong WorldObjectListRootAddress = 0x1E00;
    private const ulong EventDispatcherRootAddress = 0x1F00;
    private const ulong InputManagerRootAddress = 0x1F20;
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
    private const ulong InventoryPaneTableAddress = 0x27000;
    private const ulong FirstInventoryPaneAddress = 0x28000;
    private const ulong ThirdInventoryPaneAddress = 0x29000;
    private const ulong GroupMemberCacheAddress = 0x2A000;
    private const ulong ActiveSpellEffectsAddress = 0x2B000;
    private const ulong WorldObjectListAddress = 0x2C000;
    private const ulong WorldObjectTreeHeadAddress = 0x2C100;
    private const ulong EventDispatcherAddress = 0x2D000;
    private const ulong InputManagerAddress = 0x2E000;
    private const ulong FocusedChatPaneAddress = 0x2F000;
    private const ulong WindowMessageDialogPaneVtableAddress = 0x672A84;
    private const ulong ChatInputPaneVtableAddress = 0x682FEC;
    private const ulong TellReceiverInputPaneVtableAddress = 0x68306C;
    private const ulong TellInputPaneVtableAddress = 0x6830EC;
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
    private const ulong MapNumberAddress = PlayerAddress + 0x40;
    private const ulong MapXAddress = PlayerAddress + 0x44;
    private const ulong MapYAddress = PlayerAddress + 0x48;
    private const ulong UserStateAddress = PlayerAddress + 0x100;
    private const ulong PrivilegeLevelAddress = PlayerAddress + 0x104;
    private const ulong GoldAddress = PlayerAddress + 0x108;
    private const ulong TotalExperienceAddress = PlayerAddress + 0x10C;
    private const ulong StrengthAddress = PlayerAddress + 0x110;
    private const ulong DexterityAddress = PlayerAddress + 0x112;
    private const ulong WisdomAddress = PlayerAddress + 0x114;
    private const ulong ConstitutionAddress = PlayerAddress + 0x116;
    private const ulong IntelligenceAddress = PlayerAddress + 0x118;
    private const ulong StatPointsAddress = PlayerAddress + 0x11A;
    private const ulong ExperienceToNextLevelAddress =
        PlayerAddress + 0x11C;
    private const ulong GamePointsAddress = PlayerAddress + 0x120;
    private const ulong AbilityToNextLevelAddress = PlayerAddress + 0x124;
    private const ulong TotalAbilityAddress = PlayerAddress + 0x128;
    private const ulong WeightAddress = PlayerAddress + 0x12C;
    private const ulong MaximumWeightAddress = PlayerAddress + 0x130;
    private const ulong ArmorClassAddress = PlayerAddress + 0x134;
    private const ulong DamageModifierAddress = PlayerAddress + 0x135;
    private const ulong HitModifierAddress = PlayerAddress + 0x136;
    private const ulong AttackElementAddress = PlayerAddress + 0x138;
    private const ulong DefenseElementAddress = PlayerAddress + 0x13A;
    private const ulong MagicResistanceAddress = PlayerAddress + 0x13C;
    private const ulong ActionStateAddress = PlayerAddress + 0x13E;
    private const ulong ShowAbilityMetadataAddress = PlayerAddress + 0x140;
    private const ulong ShowMasterMetadataAddress = PlayerAddress + 0x144;
    private const ulong MapWidthAddress = PlayerAddress + 0x150;
    private const ulong MapHeightAddress = PlayerAddress + 0x154;
    private const ulong MapFlagsAddress = PlayerAddress + 0x158;
    private const ulong MapWeatherAddress = PlayerAddress + 0x15C;
    private const ulong MapTransferActiveAddress = PlayerAddress + 0x15D;
    private const ulong GroupMemberCountAddress = PlayerAddress + 0x160;

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
            Assert.That(result.Snapshot?.IsChatOpen, Is.False);
            Assert.That(
                result.Snapshot?.Character,
                Is.EqualTo(
                    new CharacterSnapshot(
                        CharacterClass.Wizard,
                        level: 99,
                        abilityLevel: 50,
                        name: "Aislinn",
                        characterId: 1234,
                        CharacterUserState.Grouped,
                        privilegeLevel: 1,
                        gold: 123456,
                        totalExperience: 654321,
                        strength: 10,
                        dexterity: 11,
                        wisdom: 12,
                        constitution: 13,
                        intelligence: 14,
                        statPoints: 5,
                        experienceToNextLevel: 1000,
                        gamePoints: 2000,
                        abilityToNextLevel: 3000,
                        totalAbility: 4000,
                        weight: 50,
                        maximumWeight: 100,
                        armorClass: -10,
                        damageModifier: 20,
                        hitModifier: 30,
                        attackElement: 1,
                        defenseElement: 2,
                        magicResistance: 3,
                        actionState: 1,
                        showAbilityMetadata: true)));
            Assert.That(
                result.Snapshot?.Vitals,
                Is.EqualTo(new VitalsSnapshot(1000, 1200, 500, 600)));
            Assert.That(
                result.Snapshot?.Location,
                Is.EqualTo(
                    new MapLocationSnapshot(
                        1,
                        "Mileth",
                        50,
                        60,
                        width: 100,
                        height: 100,
                        flags: 0x12,
                        weather: 2)));
            Assert.That(
                result.Snapshot?.Inventory,
                Is.EqualTo(
                    new InventorySnapshot(
                    [
                        new InventoryItemSnapshot(
                            1,
                            "Holy Diana",
                            sprite: 0x8123,
                            dyeColor: 1,
                            currentDurability: 13499,
                            maximumDurability: 15000),
                        new InventoryItemSnapshot(
                            3,
                            "Gnarl",
                            sprite: 0x8456,
                            dyeColor: 2,
                            displayName: "Gnarl[ 12 ]",
                            quantity: 12,
                            isStackable: true)
                    ])));
            Assert.That(
                result.Snapshot?.Equipment,
                Is.EqualTo(
                    new EquipmentSnapshot(
                    [
                        new EquipmentItemSnapshot(
                            1,
                            "Holy Diana",
                            sprite: 0x8123,
                            currentDurability: 2596615,
                            maximumDurability: 2600000),
                        new EquipmentItemSnapshot(
                            3,
                            "Dragon Shield",
                            sprite: 0x8456)
                    ])));
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
                result.Snapshot?.Group,
                Is.EqualTo(
                    new GroupSnapshot(
                    [
                        new GroupMemberSnapshot(
                            "Aislinn",
                            isStarred: true),
                        new GroupMemberSnapshot(
                            "Eidolon",
                            isStarred: false)
                    ])));
            Assert.That(
                result.Snapshot?.ActiveSpellEffects,
                Is.EqualTo(
                    new ActiveSpellEffectsSnapshot(
                    [
                        new ActiveSpellEffectSnapshot(
                            1,
                            icon: 321,
                            SpellEffectDurationStage.White)
                    ])));
            Assert.That(
                result.Snapshot?.WorldEntities,
                Is.EqualTo(WorldEntitiesSnapshot.Empty));
            Assert.That(
                result.Snapshot?.MessageDialogs,
                Is.EqualTo(MessageDialogsSnapshot.Empty));
            Assert.That(result.Snapshot?.IsPopupOpen, Is.False);
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
                        SnapshotSection.MessageDialogs,
                        SnapshotSection.Inventory,
                        SnapshotSection.Equipment,
                        SnapshotSection.Skillbook,
                        SnapshotSection.Spellbook,
                        SnapshotSection.Group,
                        SnapshotSection.ActiveSpellEffects,
                        SnapshotSection.WorldEntities,
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
    public void ShouldCaptureZeroHealthAndLaterRevival()
    {
        var source = CreateMemoryImage();
        source.WriteUInt32(new MemoryAddress(CurrentHealthAddress), 0);
        var capture = CreateCapture(source);

        var defeated = capture.Capture(new SnapshotSequence(1));
        source.WriteUInt32(
            new MemoryAddress(CurrentHealthAddress),
            750);
        var revived = capture.Capture(new SnapshotSequence(2));

        Assert.Multiple(() =>
        {
            Assert.That(defeated.Succeeded, Is.True);
            Assert.That(defeated.Snapshot?.Vitals?.CurrentHealth, Is.Zero);
            Assert.That(defeated.Snapshot?.Vitals?.HealthPercent, Is.Zero);
            Assert.That(revived.Succeeded, Is.True);
            Assert.That(
                revived.Snapshot?.Vitals?.CurrentHealth,
                Is.EqualTo(750));
            Assert.That(
                revived.Snapshot?.Vitals?.MaximumHealth,
                Is.EqualTo(1200));
            Assert.That(
                revived.Snapshot?.Sequence.Value,
                Is.EqualTo(2));
        });
    }

    [TestCase(ChatInputPaneVtableAddress)]
    [TestCase(TellReceiverInputPaneVtableAddress)]
    [TestCase(TellInputPaneVtableAddress)]
    public void ShouldCaptureFocusedVisibleLiveChatInputState(
        ulong vtableAddress)
    {
        var source = CreateMemoryImage();
        WriteFocusedChatPane(source, vtableAddress);
        var capture = CreateCapture(source);

        var result = capture.Capture(new SnapshotSequence(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Snapshot?.IsChatOpen, Is.True);
        });
    }

    [Test]
    public void ShouldIgnoreFocusedChatPaneWithoutLiveCookie()
    {
        var source = CreateMemoryImage();
        WriteFocusedChatPane(
            source,
            ChatInputPaneVtableAddress,
            timerHandlerCookie: 0);
        var capture = CreateCapture(source);

        var result = capture.Capture(new SnapshotSequence(1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Snapshot?.IsChatOpen, Is.False);
        });
    }

    [Test]
    public void ShouldRejectFocusedInputPaneChangedDuringCapture()
    {
        var source = CreateMemoryImage();
        var focusedPaneReads = 0;
        source.ReadStarting = (address, _) =>
        {
            if (address.Value !=
                InputManagerAddress +
                ClientChatInputReader.FocusedPaneOffset)
            {
                return;
            }

            focusedPaneReads++;
            if (focusedPaneReads == 2)
            {
                source.WriteUInt32(
                    address,
                    (uint)FocusedChatPaneAddress);
            }
        };
        WriteFocusedChatPane(source, ChatInputPaneVtableAddress);
        source.WriteUInt32(
            new MemoryAddress(
                InputManagerAddress +
                ClientChatInputReader.FocusedPaneOffset),
            0);
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
                Is.EqualTo(SnapshotSection.ClientState));
            Assert.That(
                result.Error?.VariableKey,
                Is.EqualTo("InputManager"));
        });
    }

    [TestCase(0x00, CharacterClass.Peasant)]
    [TestCase(0x01, CharacterClass.Warrior)]
    [TestCase(0x02, CharacterClass.Rogue)]
    [TestCase(0x03, CharacterClass.Wizard)]
    [TestCase(0x04, CharacterClass.Priest)]
    [TestCase(0x05, CharacterClass.Monk)]
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
        source.Write(new MemoryAddress(CharacterClassAddress), 0x06);
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
                    new MapLocationSnapshot(
                        2,
                        "Abel",
                        10,
                        20,
                        width: 100,
                        height: 100,
                        flags: 0x12,
                        weather: 2)));
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
                        20,
                        width: 100,
                        height: 100,
                        flags: 0x12,
                        weather: 2)));
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
            Assert.That(result.Snapshot?.Group, Is.Null);
            Assert.That(result.Snapshot?.ActiveSpellEffects, Is.Null);
            Assert.That(result.Snapshot?.WorldEntities, Is.Null);
            Assert.That(
                result.Metrics.Sections.Any(
                    section =>
                        section.Section is
                            SnapshotSection.Inventory or
                            SnapshotSection.Equipment or
                            SnapshotSection.Skillbook or
                            SnapshotSection.Spellbook or
                            SnapshotSection.Group or
                            SnapshotSection.ActiveSpellEffects or
                            SnapshotSection.WorldEntities),
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
    public void ShouldRejectInventoryChangedDuringCapture()
    {
        var source = CreateMemoryImage();
        var inventoryReads = 0;
        source.ReadStarting = (address, _) =>
        {
            if (address.Value != InventoryAddress)
            {
                return;
            }

            inventoryReads++;
            if (inventoryReads == 2)
            {
                source.Write(address, 0);
            }
        };
        var capture = CreateCapture(source);

        var result = capture.Capture(
            new SnapshotSequence(1),
            SnapshotCaptureSections.Inventory);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Quality,
                Is.EqualTo(SnapshotQuality.Incoherent));
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
        source.WriteUInt32(
            new MemoryAddress(InventoryPanesRootAddress),
            (uint)InventoryPaneTableAddress);
        source.WriteUInt32(
            new MemoryAddress(GroupMemberCacheRootAddress),
            (uint)GroupMemberCacheAddress);
        source.WriteUInt32(
            new MemoryAddress(ActiveSpellEffectsRootAddress),
            (uint)ActiveSpellEffectsAddress);
        source.WriteUInt32(
            new MemoryAddress(WorldObjectListRootAddress),
            (uint)WorldObjectListAddress);
        source.WriteUInt32(
            new MemoryAddress(EventDispatcherRootAddress),
            (uint)EventDispatcherAddress);
        source.WriteUInt32(
            new MemoryAddress(InputManagerRootAddress),
            (uint)InputManagerAddress);
        source.WriteUInt32(
            new MemoryAddress(
                InputManagerAddress +
                ClientChatInputReader.FocusedPaneOffset),
            0);
        source.Write(
            new MemoryAddress(EventDispatcherAddress + 0x64),
            new byte[12]);
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
        source.Write(new MemoryAddress(CharacterClassAddress), 0x03);
        source.WriteUInt32(new MemoryAddress(CharacterIdAddress), 1234);
        source.WriteUInt32(new MemoryAddress(CurrentHealthAddress), 1000);
        source.WriteUInt32(new MemoryAddress(MaximumHealthAddress), 1200);
        source.WriteUInt32(new MemoryAddress(CurrentManaAddress), 500);
        source.WriteUInt32(new MemoryAddress(MaximumManaAddress), 600);
        source.Write(new MemoryAddress(ActivePanelAddress), 0);
        source.Write(new MemoryAddress(InventoryExpandedAddress), 1);
        source.WriteUInt32(new MemoryAddress(MapNumberAddress), 1);
        source.WriteInt32(new MemoryAddress(MapXAddress), 50);
        source.WriteInt32(new MemoryAddress(MapYAddress), 60);
        source.WriteUInt32(
            new MemoryAddress(UserStateAddress),
            (uint)CharacterUserState.Grouped);
        source.WriteInt32(
            new MemoryAddress(PrivilegeLevelAddress),
            1);
        source.WriteUInt32(new MemoryAddress(GoldAddress), 123456);
        source.WriteUInt32(
            new MemoryAddress(TotalExperienceAddress),
            654321);
        WriteUInt16(source, StrengthAddress, 10);
        WriteUInt16(source, DexterityAddress, 11);
        WriteUInt16(source, WisdomAddress, 12);
        WriteUInt16(source, ConstitutionAddress, 13);
        WriteUInt16(source, IntelligenceAddress, 14);
        WriteUInt16(source, StatPointsAddress, 5);
        source.WriteUInt32(
            new MemoryAddress(ExperienceToNextLevelAddress),
            1000);
        source.WriteUInt32(new MemoryAddress(GamePointsAddress), 2000);
        source.WriteUInt32(
            new MemoryAddress(AbilityToNextLevelAddress),
            3000);
        source.WriteUInt32(
            new MemoryAddress(TotalAbilityAddress),
            4000);
        source.WriteUInt32(new MemoryAddress(WeightAddress), 50);
        source.WriteUInt32(new MemoryAddress(MaximumWeightAddress), 100);
        source.Write(
            new MemoryAddress(ArmorClassAddress),
            unchecked((byte)-10));
        source.Write(new MemoryAddress(DamageModifierAddress), 20);
        source.Write(new MemoryAddress(HitModifierAddress), 30);
        WriteUInt16(source, AttackElementAddress, 1);
        WriteUInt16(source, DefenseElementAddress, 2);
        WriteUInt16(source, MagicResistanceAddress, 3);
        source.Write(new MemoryAddress(ActionStateAddress), 1);
        source.WriteUInt32(
            new MemoryAddress(ShowAbilityMetadataAddress),
            1);
        source.WriteUInt32(
            new MemoryAddress(ShowMasterMetadataAddress),
            0);
        source.WriteInt32(new MemoryAddress(MapWidthAddress), 100);
        source.WriteInt32(new MemoryAddress(MapHeightAddress), 100);
        source.WriteUInt32(new MemoryAddress(MapFlagsAddress), 0x12);
        source.Write(new MemoryAddress(MapWeatherAddress), 2);
        source.Write(new MemoryAddress(MapTransferActiveAddress), 0);
        source.WriteUInt32(
            new MemoryAddress(GroupMemberCountAddress),
            2);
        var inventory = new byte[
            ClientInventoryParser.RecordSize *
            ClientInventoryParser.RecordCount];
        WriteInventoryItem(
            inventory,
            slot: 1,
            rawSprite: 0x8123,
            dyeColor: 1,
            "Holy Diana");
        WriteInventoryItem(
            inventory,
            slot: 3,
            rawSprite: 0x8456,
            dyeColor: 2,
            "Gnarl");
        WriteInventoryItem(
            inventory,
            slot: 60,
            rawSprite: 0,
            dyeColor: 0,
            "Gold");
        source.Write(new MemoryAddress(InventoryAddress), inventory);

        var inventoryPanePointers = new byte[
            ClientInventoryParser.RecordCount *
            ClientInventoryParser.PanePointerSize];
        BinaryPrimitives.WriteUInt32LittleEndian(
            inventoryPanePointers.AsSpan(0, sizeof(uint)),
            (uint)FirstInventoryPaneAddress);
        BinaryPrimitives.WriteUInt32LittleEndian(
            inventoryPanePointers.AsSpan(2 * sizeof(uint), sizeof(uint)),
            (uint)ThirdInventoryPaneAddress);
        source.Write(
            new MemoryAddress(InventoryPaneTableAddress),
            inventoryPanePointers);
        WriteInventoryPane(
            source,
            FirstInventoryPaneAddress,
            slot: 1,
            rawSprite: 0x8123,
            dyeColor: 1,
            "Holy Diana",
            quantity: 1,
            currentDurability: 13499,
            maximumDurability: 15000);
        WriteInventoryPane(
            source,
            ThirdInventoryPaneAddress,
            slot: 3,
            rawSprite: 0x8456,
            dyeColor: 2,
            "Gnarl[ 12 ]",
            quantity: 12,
            isStackable: true);

        var equipment = new byte[
            ClientEquipmentParser.RichSnapshotSize];
        WriteRichEquipmentItem(
            equipment,
            slotIndex: 0,
            rawSprite: 0x8123,
            "Holy Diana",
            currentDurability: 2596615,
            maximumDurability: 2600000);
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

        var groupMembers = new byte[
            ClientGroupParser.RecordSize *
            2];
        WriteGroupMember(
            groupMembers,
            index: 0,
            "Aislinn",
            isStarred: true);
        WriteGroupMember(
            groupMembers,
            index: 1,
            "Eidolon",
            isStarred: false);
        source.Write(
            new MemoryAddress(GroupMemberCacheAddress),
            groupMembers);

        var activeEffects = new byte[
            ClientSpellEffectParser.SnapshotSize];
        for (var index = 0;
             index < ClientSpellEffectParser.RecordCount;
             index++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(
                activeEffects.AsSpan(
                    index * sizeof(short),
                    sizeof(short)),
                -1);
        }

        BinaryPrimitives.WriteInt16LittleEndian(
            activeEffects.AsSpan(0, sizeof(short)),
            321);
        activeEffects[
            ClientSpellEffectParser.RecordCount *
            sizeof(short)] =
            (byte)SpellEffectDurationStage.White;
        source.Write(
            new MemoryAddress(ActiveSpellEffectsAddress),
            activeEffects);

        source.WriteUInt32(
            new MemoryAddress(WorldObjectListAddress + 0x20),
            (uint)WorldObjectTreeHeadAddress);
        source.WriteUInt32(
            new MemoryAddress(WorldObjectTreeHeadAddress + 0x04),
            (uint)WorldObjectTreeHeadAddress);
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
        new(
            "InputManager",
            new PointerChain(
                new MemoryAddress(InputManagerRootAddress),
                ImmutableArray.Create(new PointerOffset(0))),
            MemoryValueKind.Unsigned32),
        Static(
            "ChatInputPaneVtable",
            ChatInputPaneVtableAddress,
            MemoryValueKind.Unsigned32),
        Static(
            "TellReceiverInputPaneVtable",
            TellReceiverInputPaneVtableAddress,
            MemoryValueKind.Unsigned32),
        Static(
            "TellInputPaneVtable",
            TellInputPaneVtableAddress,
            MemoryValueKind.Unsigned32),
        Dynamic("MapNumber", 0x40, MemoryValueKind.Unsigned32),
        Dynamic("MapX", 0x44, MemoryValueKind.Signed32),
        Dynamic("MapY", 0x48, MemoryValueKind.Signed32),
        Dynamic("UserState", 0x100, MemoryValueKind.Unsigned32),
        Dynamic("PrivilegeLevel", 0x104, MemoryValueKind.Signed32),
        Dynamic("Gold", 0x108, MemoryValueKind.Unsigned32),
        Dynamic("TotalExperience", 0x10C, MemoryValueKind.Unsigned32),
        Dynamic("Strength", 0x110, MemoryValueKind.Unsigned16),
        Dynamic("Dexterity", 0x112, MemoryValueKind.Unsigned16),
        Dynamic("Wisdom", 0x114, MemoryValueKind.Unsigned16),
        Dynamic("Constitution", 0x116, MemoryValueKind.Unsigned16),
        Dynamic("Intelligence", 0x118, MemoryValueKind.Unsigned16),
        Dynamic("StatPoints", 0x11A, MemoryValueKind.Unsigned16),
        Dynamic(
            "ExperienceToNextLevel",
            0x11C,
            MemoryValueKind.Unsigned32),
        Dynamic("GamePoints", 0x120, MemoryValueKind.Unsigned32),
        Dynamic(
            "AbilityToNextLevel",
            0x124,
            MemoryValueKind.Unsigned32),
        Dynamic("TotalAbility", 0x128, MemoryValueKind.Unsigned32),
        Dynamic("Weight", 0x12C, MemoryValueKind.Unsigned32),
        Dynamic("MaximumWeight", 0x130, MemoryValueKind.Unsigned32),
        Dynamic("ArmorClass", 0x134, MemoryValueKind.SByte),
        Dynamic("DamageModifier", 0x135, MemoryValueKind.Byte),
        Dynamic("HitModifier", 0x136, MemoryValueKind.Byte),
        Dynamic("AttackElement", 0x138, MemoryValueKind.Unsigned16),
        Dynamic("DefenseElement", 0x13A, MemoryValueKind.Unsigned16),
        Dynamic("MagicResistance", 0x13C, MemoryValueKind.Unsigned16),
        Dynamic("ActionState", 0x13E, MemoryValueKind.Byte),
        Dynamic(
            "ShowAbilityMetadata",
            0x140,
            MemoryValueKind.Unsigned32),
        Dynamic(
            "ShowMasterMetadata",
            0x144,
            MemoryValueKind.Unsigned32),
        Dynamic("MapWidth", 0x150, MemoryValueKind.Signed32),
        Dynamic("MapHeight", 0x154, MemoryValueKind.Signed32),
        Dynamic("MapFlags", 0x158, MemoryValueKind.Unsigned32),
        Dynamic("MapWeather", 0x15C, MemoryValueKind.Byte),
        Dynamic("MapTransferActive", 0x15D, MemoryValueKind.Byte),
        Dynamic(
            "GroupMemberCount",
            0x160,
            MemoryValueKind.Unsigned32),
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
            MemoryValueKind.Signed32),
        Block(
            "InventoryPanes",
            InventoryPanesRootAddress,
            maximumLength: 0,
            recordSize: ClientInventoryParser.PanePointerSize,
            capacity: ClientInventoryParser.RecordCount),
        Block(
            "GroupMemberCache",
            GroupMemberCacheRootAddress,
            maximumLength: ClientGroupParser.NameLength,
            recordSize: ClientGroupParser.RecordSize,
            capacity: ClientGroupParser.RecordCount),
        Block(
            "ActiveSpellEffects",
            ActiveSpellEffectsRootAddress,
            maximumLength: 0,
            recordSize: ClientSpellEffectParser.SnapshotSize,
            capacity: ClientSpellEffectParser.RecordCount),
        new(
            "WorldObjectList",
            new PointerChain(
                new MemoryAddress(WorldObjectListRootAddress),
                ImmutableArray.Create(new PointerOffset(0))),
            MemoryValueKind.Unsigned32),
        new(
            "ActiveEventDispatcher",
            new PointerChain(
                new MemoryAddress(EventDispatcherRootAddress),
                ImmutableArray.Create(new PointerOffset(0))),
            MemoryValueKind.Unsigned32),
        new(
            "WindowMessageDialogPaneVtable",
            new PointerChain(
                new MemoryAddress(
                    WindowMessageDialogPaneVtableAddress)),
            MemoryValueKind.Unsigned32)
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

    private static MemoryVariableDefinition Static(
        string key,
        ulong address,
        MemoryValueKind kind) =>
        new(
            key,
            new PointerChain(new MemoryAddress(address)),
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

    private static void WriteFocusedChatPane(
        MemoryImageSource source,
        ulong vtableAddress,
        uint timerHandlerCookie =
            ClientChatInputReader.LiveTimerHandlerCookie,
        byte visible = 1)
    {
        source.WriteUInt32(
            new MemoryAddress(
                InputManagerAddress +
                ClientChatInputReader.FocusedPaneOffset),
            (uint)FocusedChatPaneAddress);
        source.WriteUInt32(
            new MemoryAddress(FocusedChatPaneAddress),
            (uint)vtableAddress);
        source.WriteUInt32(
            new MemoryAddress(
                FocusedChatPaneAddress +
                ClientChatInputReader.TimerHandlerCookieOffset),
            timerHandlerCookie);
        source.Write(
            new MemoryAddress(
                FocusedChatPaneAddress +
                ClientChatInputReader.VisibleOffset),
            visible);
    }

    private static void WriteInventoryItem(
        Span<byte> snapshot,
        int slot,
        ushort rawSprite,
        byte dyeColor,
        string name)
    {
        var record = snapshot.Slice(
            (slot - 1) * ClientInventoryParser.RecordSize,
            ClientInventoryParser.RecordSize);
        record[0] = 1;
        BinaryPrimitives.WriteUInt16LittleEndian(record[2..], rawSprite);
        record[4] = dyeColor;
        Encoding.ASCII.GetBytes(name).CopyTo(record[5..]);
    }

    private static void WriteInventoryPane(
        MemoryImageSource source,
        ulong paneAddress,
        int slot,
        ushort rawSprite,
        byte dyeColor,
        string displayName,
        uint quantity,
        bool isStackable = false,
        uint currentDurability = 0,
        uint maximumDurability = 0)
    {
        var snapshot = new byte[ClientInventoryParser.PaneSnapshotSize];
        BinaryPrimitives.WriteUInt16LittleEndian(snapshot, rawSprite);
        Encoding.ASCII.GetBytes(displayName).CopyTo(snapshot.AsSpan(0x02));
        snapshot[0x82] = dyeColor;
        snapshot[0x84] = checked((byte)slot);
        BinaryPrimitives.WriteUInt32LittleEndian(
            snapshot.AsSpan(0xA8),
            currentDurability);
        BinaryPrimitives.WriteUInt32LittleEndian(
            snapshot.AsSpan(0xAC),
            maximumDurability);
        BinaryPrimitives.WriteUInt32LittleEndian(
            snapshot.AsSpan(0xB0),
            quantity);
        snapshot[0xB4] = isStackable ? (byte)1 : (byte)0;
        source.Write(
            new MemoryAddress(
                paneAddress + ClientInventoryParser.PaneSnapshotOffset),
            snapshot);
    }

    private static void WriteGroupMember(
        Span<byte> snapshot,
        int index,
        string name,
        bool isStarred)
    {
        var record = snapshot.Slice(
            index * ClientGroupParser.RecordSize,
            ClientGroupParser.RecordSize);
        Encoding.ASCII.GetBytes(name).CopyTo(record);
        record[ClientGroupParser.NameLength] =
            isStarred ? (byte)1 : (byte)0;
    }

    private static void WriteUInt16(
        MemoryImageSource source,
        ulong address,
        ushort value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        source.Write(new MemoryAddress(address), buffer.ToArray());
    }

    private static void WriteRichEquipmentItem(
        Span<byte> snapshot,
        int slotIndex,
        ushort rawSprite,
        string name,
        uint currentDurability = 0,
        uint maximumDurability = 0)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(
            snapshot.Slice(slotIndex * sizeof(ushort)),
            rawSprite);
        Encoding.ASCII.GetBytes(name).CopyTo(
            snapshot.Slice(
                0x36 +
                slotIndex * ClientEquipmentParser.CompactNameLength));
        var durabilityOffset = 0x938 + slotIndex * 0x08;
        BinaryPrimitives.WriteUInt32LittleEndian(
            snapshot.Slice(durabilityOffset),
            currentDurability);
        BinaryPrimitives.WriteUInt32LittleEndian(
            snapshot.Slice(durabilityOffset + sizeof(uint)),
            maximumDurability);
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
