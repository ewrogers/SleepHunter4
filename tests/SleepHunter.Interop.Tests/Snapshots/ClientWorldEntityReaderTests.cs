using System.Buffers.Binary;
using System.Text;
using SleepHunter.Interop.Memory;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Interop.Tests.Memory;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Tests.Snapshots;

public sealed class ClientWorldEntityReaderTests
{
    private const ulong ListAddress = 0x10000;
    private const ulong HeadAddress = 0x10100;
    private const ulong RootNodeAddress = 0x10200;
    private const ulong ItemNodeAddress = 0x10300;
    private const ulong PlayerNodeAddress = 0x10400;
    private const ulong MonsterNodeAddress = 0x10500;
    private const ulong ItemObjectAddress = 0x20000;
    private const ulong NonPlayerObjectAddress = 0x21000;
    private const ulong PlayerObjectAddress = 0x22000;
    private const ulong MonsterObjectAddress = 0x23000;

    [Test]
    public void ShouldReadBoundedWorldTreeWithDocumentedEntityTypes()
    {
        var source = CreateWorldImage();
        var session = new MemoryReadSession(
            source,
            MemoryReadLimits.Client32Bit);

        var succeeded = ClientWorldEntityReader.TryRead(
            session,
            new MemoryAddress(ListAddress),
            localCharacterId: 30,
            out var snapshot,
            out var error);

        Assert.Multiple(() =>
        {
            Assert.That(succeeded, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(snapshot.Entities.Length, Is.EqualTo(4));
            Assert.That(
                snapshot.Find(10),
                Is.EqualTo(
                    new WorldEntitySnapshot(
                        10,
                        WorldEntityType.GroundItem,
                        x: 40,
                        y: 41,
                        sprite: 0x8123,
                        dyeColor: 3,
                        drawLayer: 2,
                        broadCategory: 8,
                        collisionLevel: 1,
                        runtimeClassName: "WorldObject_Item",
                        appearanceKind:
                            WorldAppearanceKind.GroundItem)));
            Assert.That(
                snapshot.Find(20),
                Is.EqualTo(
                    new WorldEntitySnapshot(
                        20,
                        WorldEntityType.NonPlayerCharacter,
                        x: 50,
                        y: 51,
                        name: "Dar",
                        drawLayer: 4,
                        broadCategory: 16,
                        collisionLevel: 2,
                        direction: 3,
                        creatureType: 2,
                        runtimeClassName: "WorldObject_Monster",
                        appearanceKind:
                            WorldAppearanceKind.Monster,
                        imageSessionIdentity: 0x35000,
                        appearanceResourceIdentity: 0x36000,
                        imageSessionResourceIdentity: 0x36100)));
            Assert.That(
                snapshot.Find(30)?.Type,
                Is.EqualTo(WorldEntityType.Player));
            Assert.That(snapshot.Find(30)?.Name, Is.EqualTo("Aislinn"));
            Assert.That(snapshot.Find(30)?.Sprite, Is.EqualTo(0x0456));
            Assert.That(snapshot.Find(30)?.IsLocalPlayer, Is.True);
            Assert.That(
                snapshot.Find(30)?.AppearanceKind,
                Is.EqualTo(WorldAppearanceKind.Human));
            Assert.That(
                snapshot.Find(30)?.ImageSessionIdentity,
                Is.EqualTo(0x35200));
            Assert.That(
                snapshot.Find(30)?.AppearanceResourceIdentity,
                Is.EqualTo(0x36200));
            Assert.That(snapshot.Find(30)?.UsesHumanAppearance, Is.True);
            Assert.That(snapshot.Find(30)?.IsMonsterDisguise, Is.False);
            Assert.That(
                snapshot.Find(30)?.HumanAppearance?.WeaponSprite,
                Is.EqualTo(0x0789));
            Assert.That(
                snapshot.Find(40),
                Is.EqualTo(
                    new WorldEntitySnapshot(
                        40,
                        WorldEntityType.Monster,
                        x: 70,
                        y: 71,
                        drawLayer: 5,
                        broadCategory: 32,
                        collisionLevel: 3,
                        direction: 2,
                        creatureType: 9,
                        runtimeClassName: "WorldObject_Monster",
                        appearanceKind:
                            WorldAppearanceKind.Monster,
                        imageSessionIdentity: 0x35400,
                        appearanceResourceIdentity: 0x36400,
                        imageSessionResourceIdentity: 0x36500)));
        });
    }

    [Test]
    public void ShouldRejectTreeIdentityChangedDuringCapture()
    {
        var source = CreateWorldImage();
        var headReads = 0;
        source.ReadStarting = (address, _) =>
        {
            if (address.Value != ListAddress + 0x20)
            {
                return;
            }

            headReads++;
            if (headReads == 2)
            {
                source.WriteUInt32(address, 0x10900);
            }
        };
        var session = new MemoryReadSession(
            source,
            MemoryReadLimits.Client32Bit);

        var succeeded = ClientWorldEntityReader.TryRead(
            session,
            new MemoryAddress(ListAddress),
            localCharacterId: 30,
            out var snapshot,
            out var error);

        Assert.Multiple(() =>
        {
            Assert.That(succeeded, Is.False);
            Assert.That(snapshot, Is.EqualTo(WorldEntitiesSnapshot.Empty));
            Assert.That(
                error,
                Is.EqualTo("The world entity tree changed during capture."));
        });
    }

    [Test]
    public void ShouldKeepHumanTypeWhileExposingMonsterDisguise()
    {
        var source = CreateWorldImage();
        source.Write(
            new MemoryAddress(PlayerObjectAddress + 0x104),
            0);
        source.WriteUInt32(
            new MemoryAddress(0x35210),
            0x36300);
        var session = new MemoryReadSession(
            source,
            MemoryReadLimits.Client32Bit);

        var succeeded = ClientWorldEntityReader.TryRead(
            session,
            new MemoryAddress(ListAddress),
            localCharacterId: 30,
            out var snapshot,
            out var error);
        var player = snapshot.Find(30);

        Assert.Multiple(() =>
        {
            Assert.That(succeeded, Is.True);
            Assert.That(error, Is.Null);
            Assert.That(player?.Type, Is.EqualTo(WorldEntityType.Player));
            Assert.That(player?.Sprite, Is.Null);
            Assert.That(
                player?.AppearanceKind,
                Is.EqualTo(WorldAppearanceKind.Monster));
            Assert.That(player?.IsMonsterDisguise, Is.True);
            Assert.That(
                player?.ImageSessionResourceIdentity,
                Is.EqualTo(0x36300));
        });
    }

    [TestCase(".?AVWorldObject_Item@@", "WorldObject_Item")]
    [TestCase(".?AUWorldObject_Human@@", "WorldObject_Human")]
    [TestCase("CustomWorldObject", "CustomWorldObject")]
    public void ShouldNormalizeMicrosoftRuntimeClassNames(
        string decoratedName,
        string expected)
    {
        Assert.That(
            ClientWorldEntityReader.NormalizeRuntimeClassName(decoratedName),
            Is.EqualTo(expected));
    }

    [TestCase(0, null, WorldEntityType.Monster)]
    [TestCase(1, null, WorldEntityType.Monster)]
    [TestCase(2, null, WorldEntityType.NonPlayerCharacter)]
    [TestCase(3, null, WorldEntityType.Monster)]
    [TestCase(4, null, WorldEntityType.Player)]
    [TestCase(0, "Named", WorldEntityType.NonPlayerCharacter)]
    public void ShouldClassifyDocumentedCreatureTypes(
        byte creatureType,
        string? name,
        WorldEntityType expected)
    {
        Assert.That(
            ClientWorldEntityReader.ClassifyMonster(
                creatureType,
                name),
            Is.EqualTo(expected));
    }

    private static MemoryImageSource CreateWorldImage()
    {
        var source = new MemoryImageSource();
        source.WriteUInt32(
            new MemoryAddress(ListAddress + 0x20),
            (uint)HeadAddress);
        source.WriteUInt32(
            new MemoryAddress(HeadAddress + 0x04),
            (uint)RootNodeAddress);

        WriteNode(
            source,
            RootNodeAddress,
            ItemNodeAddress,
            PlayerNodeAddress,
            id: 20,
            NonPlayerObjectAddress);
        WriteNode(
            source,
            ItemNodeAddress,
            HeadAddress,
            HeadAddress,
            id: 10,
            ItemObjectAddress);
        WriteNode(
            source,
            PlayerNodeAddress,
            HeadAddress,
            MonsterNodeAddress,
            id: 30,
            PlayerObjectAddress);
        WriteNode(
            source,
            MonsterNodeAddress,
            HeadAddress,
            HeadAddress,
            id: 40,
            MonsterObjectAddress);

        const ulong itemVtable = 0x30000;
        const ulong monsterVtable = 0x30100;
        const ulong humanVtable = 0x30200;
        WriteRuntimeClass(
            source,
            itemVtable,
            locatorAddress: 0x31000,
            typeDescriptorAddress: 0x32000,
            ".?AVWorldObject_Item@@");
        WriteRuntimeClass(
            source,
            monsterVtable,
            locatorAddress: 0x31100,
            typeDescriptorAddress: 0x32100,
            ".?AVWorldObject_Monster@@");
        WriteRuntimeClass(
            source,
            humanVtable,
            locatorAddress: 0x31200,
            typeDescriptorAddress: 0x32200,
            ".?AVWorldObject_Human@@");

        var item = CreateCommonObject(
            itemVtable,
            id: 10,
            x: 40,
            y: 41,
            drawLayer: 2,
            broadCategory: 8,
            collisionLevel: 1,
            length: 0xB8);
        BinaryPrimitives.WriteUInt16LittleEndian(
            item.AsSpan(0x7C),
            0x8123);
        item[0xB4] = 3;
        source.Write(new MemoryAddress(ItemObjectAddress), item);

        const ulong namePaneAddress = 0x40000;
        var nonPlayer = CreateCommonObject(
            monsterVtable,
            id: 20,
            x: 50,
            y: 51,
            drawLayer: 4,
            broadCategory: 16,
            collisionLevel: 2,
            length: 0x1F0);
        BinaryPrimitives.WriteUInt32LittleEndian(
            nonPlayer.AsSpan(0x58),
            (uint)namePaneAddress);
        nonPlayer[0x192] = 3;
        nonPlayer[0x1EC] = 2;
        BinaryPrimitives.WriteUInt32LittleEndian(
            nonPlayer.AsSpan(0x90),
            0x35000);
        BinaryPrimitives.WriteUInt32LittleEndian(
            nonPlayer.AsSpan(0x9C),
            0x36000);
        source.Write(
            new MemoryAddress(NonPlayerObjectAddress),
            nonPlayer);
        source.WriteUInt32(
            new MemoryAddress(0x35010),
            0x36100);
        WriteFixedAscii(
            source,
            namePaneAddress + 0x198,
            "Dar",
            length: 64);

        var player = CreateCommonObject(
            humanVtable,
            id: 30,
            x: 60,
            y: 61,
            drawLayer: 1,
            broadCategory: 64,
            collisionLevel: 0,
            length: 0x1F0);
        player[0xA4] = 2;
        BinaryPrimitives.WriteUInt16LittleEndian(
            player.AsSpan(0xAA),
            0x0456);
        BinaryPrimitives.WriteUInt16LittleEndian(
            player.AsSpan(0xBE),
            0x0789);
        BinaryPrimitives.WriteUInt32LittleEndian(
            player.AsSpan(0x90),
            0x35200);
        BinaryPrimitives.WriteUInt32LittleEndian(
            player.AsSpan(0x9C),
            0x36200);
        player[0x104] = 1;
        Encoding.ASCII.GetBytes("Aislinn").CopyTo(
            player.AsSpan(0x112));
        player[0x192] = 1;
        source.Write(new MemoryAddress(PlayerObjectAddress), player);

        var monster = CreateCommonObject(
            monsterVtable,
            id: 40,
            x: 70,
            y: 71,
            drawLayer: 5,
            broadCategory: 32,
            collisionLevel: 3,
            length: 0x1F0);
        monster[0x192] = 2;
        monster[0x1EC] = 9;
        BinaryPrimitives.WriteUInt32LittleEndian(
            monster.AsSpan(0x90),
            0x35400);
        BinaryPrimitives.WriteUInt32LittleEndian(
            monster.AsSpan(0x9C),
            0x36400);
        source.Write(new MemoryAddress(MonsterObjectAddress), monster);
        source.WriteUInt32(
            new MemoryAddress(0x35410),
            0x36500);
        return source;
    }

    private static byte[] CreateCommonObject(
        ulong vtableAddress,
        uint id,
        int x,
        int y,
        byte drawLayer,
        uint broadCategory,
        byte collisionLevel,
        int length)
    {
        var bytes = new byte[length];
        BinaryPrimitives.WriteUInt32LittleEndian(
            bytes,
            (uint)vtableAddress);
        BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(0x24),
            id);
        bytes[0x28] = drawLayer;
        BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(0x2C),
            broadCategory);
        bytes[0x31] = collisionLevel;
        BinaryPrimitives.WriteInt32LittleEndian(
            bytes.AsSpan(0x40),
            y);
        BinaryPrimitives.WriteInt32LittleEndian(
            bytes.AsSpan(0x44),
            x);
        bytes[0x48] = 1;
        return bytes;
    }

    private static void WriteNode(
        MemoryImageSource source,
        ulong address,
        ulong left,
        ulong right,
        uint id,
        ulong objectAddress)
    {
        var bytes = new byte[0x18];
        BinaryPrimitives.WriteUInt32LittleEndian(
            bytes,
            (uint)left);
        BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(0x08),
            (uint)right);
        BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(0x0C),
            id);
        BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(0x10),
            (uint)objectAddress);
        source.Write(new MemoryAddress(address), bytes);
    }

    private static void WriteRuntimeClass(
        MemoryImageSource source,
        ulong vtableAddress,
        ulong locatorAddress,
        ulong typeDescriptorAddress,
        string decoratedName)
    {
        source.WriteUInt32(
            new MemoryAddress(vtableAddress - sizeof(uint)),
            (uint)locatorAddress);
        source.WriteUInt32(
            new MemoryAddress(locatorAddress + 0x0C),
            (uint)typeDescriptorAddress);
        WriteFixedAscii(
            source,
            typeDescriptorAddress + 0x08,
            decoratedName,
            length: 96);
    }

    private static void WriteFixedAscii(
        MemoryImageSource source,
        ulong address,
        string value,
        int length)
    {
        var bytes = new byte[length];
        Encoding.ASCII.GetBytes(value).CopyTo(bytes, 0);
        source.Write(new MemoryAddress(address), bytes);
    }
}
