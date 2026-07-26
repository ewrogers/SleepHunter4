using System.Buffers.Binary;
using System.Text;
using SleepHunter.Interop.Memory;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Snapshots;

internal static class ClientWorldEntityReader
{
    public const int MaximumEntities = 512;

    private const int TreeHeadOffset = 0x20;
    private const int TreeNodeSize = 0x18;
    private const int CommonObjectSize = 0x7C;
    private const int ItemObjectSize = 0xB8;
    private const int LivingObjectSize = 0x1F0;
    private const int MaximumRuntimeClassName = 96;
    private const int MaximumEntityName = 128;
    private const int MaximumNamePaneText = 64;

    private static readonly Encoding StrictAscii = Encoding.GetEncoding(
        Encoding.ASCII.CodePage,
        EncoderFallback.ExceptionFallback,
        DecoderFallback.ExceptionFallback);

    public static bool TryRead(
        MemoryReadSession session,
        MemoryAddress worldObjectList,
        uint localCharacterId,
        out WorldEntitiesSnapshot snapshot,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!TryReadTreeIdentity(
                session,
                worldObjectList,
                out var head,
                out var root))
        {
            snapshot = WorldEntitiesSnapshot.Empty;
            error = "The world entity tree identity could not be read.";
            return false;
        }

        var entities = new List<WorldEntitySnapshot>();
        var visited = new HashSet<MemoryAddress>();
        var runtimeClasses = new Dictionary<MemoryAddress, string>();
        var stack = new Stack<NodeObservation>();
        var current = root;
        while (current != head || stack.Count > 0)
        {
            while (current != head)
            {
                if (current.IsNull ||
                    !visited.Add(current) ||
                    visited.Count > MaximumEntities ||
                    !TryReadNode(session, current, out var node))
                {
                    snapshot = WorldEntitiesSnapshot.Empty;
                    error =
                        "The world entity tree contains an invalid, repeated, or excessive node.";
                    return false;
                }

                stack.Push(node);
                current = node.Left;
            }

            var observation = stack.Pop();
            if (!TryReadEntity(
                    session,
                    observation,
                    localCharacterId,
                    runtimeClasses,
                    out var entity,
                    out error))
            {
                snapshot = WorldEntitiesSnapshot.Empty;
                return false;
            }

            entities.Add(entity);
            current = observation.Right;
        }

        if (!TryReadTreeIdentity(
                session,
                worldObjectList,
                out var currentHead,
                out var currentRoot) ||
            currentHead != head ||
            currentRoot != root)
        {
            snapshot = WorldEntitiesSnapshot.Empty;
            error = "The world entity tree changed during capture.";
            return false;
        }

        try
        {
            snapshot = new WorldEntitiesSnapshot(entities);
            error = null;
            return true;
        }
        catch (ArgumentException exception)
        {
            snapshot = WorldEntitiesSnapshot.Empty;
            error = exception.Message;
            return false;
        }
    }

    internal static string? NormalizeRuntimeClassName(string decoratedName)
    {
        if (string.IsNullOrWhiteSpace(decoratedName))
        {
            return null;
        }

        const string classPrefix = ".?AV";
        const string structPrefix = ".?AU";
        var start = decoratedName.StartsWith(
            classPrefix,
            StringComparison.Ordinal)
            ? classPrefix.Length
            : decoratedName.StartsWith(
                structPrefix,
                StringComparison.Ordinal)
                ? structPrefix.Length
                : 0;
        if (start == 0)
        {
            return decoratedName.Trim();
        }

        var end = decoratedName.IndexOf(
            "@@",
            start,
            StringComparison.Ordinal);
        if (end < 0)
        {
            end = decoratedName.Length;
        }

        return decoratedName[start..end];
    }

    private static bool TryReadTreeIdentity(
        MemoryReadSession session,
        MemoryAddress list,
        out MemoryAddress head,
        out MemoryAddress root)
    {
        if (!list.TryOffset(TreeHeadOffset, out var headAddress) ||
            !session.TryReadPointer(headAddress, out head, out _) ||
            head.IsNull ||
            !head.TryOffset(sizeof(uint), out var rootAddress) ||
            !session.TryReadPointer(rootAddress, out root, out _) ||
            root.IsNull)
        {
            head = default;
            root = default;
            return false;
        }

        return true;
    }

    private static bool TryReadNode(
        MemoryReadSession session,
        MemoryAddress address,
        out NodeObservation node)
    {
        Span<byte> bytes = stackalloc byte[TreeNodeSize];
        if (!session.TryRead(address, bytes, out _))
        {
            node = default;
            return false;
        }

        node = new NodeObservation(
            address,
            ReadAddress(bytes, 0x00),
            ReadAddress(bytes, 0x08),
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[0x0C..]),
            ReadAddress(bytes, 0x10));
        return !node.Object.IsNull && node.EntityId != 0;
    }

    private static bool TryReadEntity(
        MemoryReadSession session,
        NodeObservation node,
        uint localCharacterId,
        Dictionary<MemoryAddress, string> runtimeClasses,
        out WorldEntitySnapshot entity,
        out string? error)
    {
        var common = new byte[CommonObjectSize];
        if (!session.TryRead(node.Object, common, out _))
        {
            entity = null!;
            error = $"World entity {node.EntityId} could not be read.";
            return false;
        }

        var objectId = BinaryPrimitives.ReadUInt32LittleEndian(
            common.AsSpan(0x24, sizeof(uint)));
        if (objectId != node.EntityId || common[0x48] == 0)
        {
            entity = null!;
            error =
                $"World entity {node.EntityId} changed identity or insertion state.";
            return false;
        }

        var virtualTable = ReadAddress(common, 0);
        if (!runtimeClasses.TryGetValue(
                virtualTable,
                out var runtimeClassName) &&
            (!TryReadRuntimeClassName(
                session,
                virtualTable,
                out runtimeClassName) ||
             !runtimeClasses.TryAdd(
                 virtualTable,
                 runtimeClassName)))
        {
            entity = null!;
            error =
                $"World entity {node.EntityId} has an unreadable runtime class.";
            return false;
        }

        var x = BinaryPrimitives.ReadInt32LittleEndian(
            common.AsSpan(0x44, sizeof(int)));
        var y = BinaryPrimitives.ReadInt32LittleEndian(
            common.AsSpan(0x40, sizeof(int)));
        var drawLayer = common[0x28];
        var broadCategory = BinaryPrimitives.ReadUInt32LittleEndian(
            common.AsSpan(0x2C, sizeof(uint)));
        var collisionLevel = common[0x31];

        switch (runtimeClassName)
        {
            case "WorldObject_Item":
                return TryReadItem(
                    session,
                    node,
                    x,
                    y,
                    drawLayer,
                    broadCategory,
                    collisionLevel,
                    runtimeClassName,
                    out entity,
                    out error);

            case "WorldObject_Monster":
                return TryReadMonster(
                    session,
                    node,
                    common,
                    x,
                    y,
                    drawLayer,
                    broadCategory,
                    collisionLevel,
                    runtimeClassName,
                    out entity,
                    out error);

            case "WorldObject_Human":
            case "WorldObject_User":
                return TryReadHuman(
                    session,
                    node,
                    x,
                    y,
                    drawLayer,
                    broadCategory,
                    collisionLevel,
                    runtimeClassName,
                    localCharacterId,
                    out entity,
                    out error);

            default:
                entity = new WorldEntitySnapshot(
                    node.EntityId,
                    WorldEntityType.Unknown,
                    x,
                    y,
                    drawLayer: drawLayer,
                    broadCategory: broadCategory,
                    collisionLevel: collisionLevel,
                    runtimeClassName: runtimeClassName);
                error = null;
                return true;
        }
    }

    private static bool TryReadItem(
        MemoryReadSession session,
        NodeObservation node,
        int x,
        int y,
        byte drawLayer,
        uint broadCategory,
        byte collisionLevel,
        string runtimeClassName,
        out WorldEntitySnapshot entity,
        out string? error)
    {
        var bytes = new byte[ItemObjectSize];
        if (!session.TryRead(node.Object, bytes, out _))
        {
            entity = null!;
            error = $"Ground item {node.EntityId} could not be read.";
            return false;
        }

        entity = new WorldEntitySnapshot(
            node.EntityId,
            WorldEntityType.GroundItem,
            x,
            y,
            BinaryPrimitives.ReadUInt16LittleEndian(
                bytes.AsSpan(0x7C, sizeof(ushort))),
            dyeColor: bytes[0xB4],
            drawLayer: drawLayer,
            broadCategory: broadCategory,
            collisionLevel: collisionLevel,
            runtimeClassName: runtimeClassName,
            appearanceKind: WorldAppearanceKind.GroundItem);
        error = null;
        return true;
    }

    private static bool TryReadMonster(
        MemoryReadSession session,
        NodeObservation node,
        ReadOnlySpan<byte> common,
        int x,
        int y,
        byte drawLayer,
        uint broadCategory,
        byte collisionLevel,
        string runtimeClassName,
        out WorldEntitySnapshot entity,
        out string? error)
    {
        var bytes = new byte[LivingObjectSize];
        if (!session.TryRead(node.Object, bytes, out _))
        {
            entity = null!;
            error = $"Living entity {node.EntityId} could not be read.";
            return false;
        }

        var imageSession = ReadAddress(bytes, 0x90);
        var appearanceResource = ReadAddress(bytes, 0x9C);
        if (!TryReadImageSessionResource(
                session,
                imageSession,
                out var imageSessionResource))
        {
            entity = null!;
            error =
                $"Living entity {node.EntityId} has an unreadable image-session resource.";
            return false;
        }

        string? name = null;
        var namePane = ReadAddress(common, 0x58);
        if (!namePane.IsNull)
        {
            if (!namePane.TryOffset(0x198, out var textAddress) ||
                !session.TryReadString(
                    textAddress,
                    MaximumNamePaneText,
                    StrictAscii,
                    out name,
                    out _,
                    requireTerminator: true))
            {
                entity = null!;
                error =
                    $"Living entity {node.EntityId} has an unreadable name pane.";
                return false;
            }
        }

        entity = new WorldEntitySnapshot(
            node.EntityId,
            ClassifyMonster(bytes[0x1EC], name),
            x,
            y,
            sprite: null,
            name,
            drawLayer: drawLayer,
            broadCategory: broadCategory,
            collisionLevel: collisionLevel,
            direction: bytes[0x192],
            creatureType: bytes[0x1EC],
            runtimeClassName: runtimeClassName,
            appearanceKind: WorldAppearanceKind.Monster,
            imageSessionIdentity: IdentityOf(imageSession),
            appearanceResourceIdentity: IdentityOf(appearanceResource),
            imageSessionResourceIdentity:
                IdentityOf(imageSessionResource));
        error = null;
        return true;
    }

    internal static WorldEntityType ClassifyMonster(
        byte creatureType,
        string? name) =>
        creatureType switch
        {
            2 => WorldEntityType.NonPlayerCharacter,
            4 => WorldEntityType.Player,
            _ when !string.IsNullOrWhiteSpace(name) =>
                WorldEntityType.NonPlayerCharacter,
            _ => WorldEntityType.Monster
        };

    private static bool TryReadHuman(
        MemoryReadSession session,
        NodeObservation node,
        int x,
        int y,
        byte drawLayer,
        uint broadCategory,
        byte collisionLevel,
        string runtimeClassName,
        uint localCharacterId,
        out WorldEntitySnapshot entity,
        out string? error)
    {
        var bytes = new byte[LivingObjectSize];
        if (!session.TryRead(node.Object, bytes, out _))
        {
            entity = null!;
            error = $"Player entity {node.EntityId} could not be read.";
            return false;
        }

        var appearance = new HumanAppearanceSnapshot(
            bytes[0xA4],
            ReadUInt16(bytes, 0xA6),
            ReadUInt16(bytes, 0xAA),
            ReadUInt16(bytes, 0xAE),
            ReadUInt16(bytes, 0xB0),
            ReadUInt16(bytes, 0xB4),
            ReadUInt16(bytes, 0xB8),
            ReadUInt16(bytes, 0xBE),
            ReadUInt16(bytes, 0xC4),
            ReadUInt16(bytes, 0xC6),
            ReadUInt16(bytes, 0xC0),
            ReadUInt16(bytes, 0xCA),
            ReadUInt16(bytes, 0xCE),
            bytes[0xD5] == 1);
        var imageSession = ReadAddress(bytes, 0x90);
        var appearanceResource = ReadAddress(bytes, 0x9C);
        var usesHumanAppearance = bytes[0x104] != 0;
        var imageSessionResource = MemoryAddress.Null;
        if (!usesHumanAppearance &&
            !TryReadImageSessionResource(
                session,
                imageSession,
                out imageSessionResource))
        {
            entity = null!;
            error =
                $"Player entity {node.EntityId} has an unreadable disguise resource.";
            return false;
        }

        var name = ReadInlineString(bytes, 0x112, MaximumEntityName);
        entity = new WorldEntitySnapshot(
            node.EntityId,
            WorldEntityType.Player,
            x,
            y,
            usesHumanAppearance
                ? appearance.BodySprite
                : null,
            name,
            drawLayer: drawLayer,
            broadCategory: broadCategory,
            collisionLevel: collisionLevel,
            direction: bytes[0x192],
            isLocalPlayer:
                node.EntityId == localCharacterId ||
                runtimeClassName == "WorldObject_User" ||
                bytes[0x98] != 0,
            humanAppearance: appearance,
            runtimeClassName: runtimeClassName,
            appearanceKind: usesHumanAppearance
                ? WorldAppearanceKind.Human
                : WorldAppearanceKind.Monster,
            imageSessionIdentity: IdentityOf(imageSession),
            appearanceResourceIdentity: IdentityOf(appearanceResource),
            imageSessionResourceIdentity:
                IdentityOf(imageSessionResource),
            usesHumanAppearance: usesHumanAppearance);
        error = null;
        return true;
    }

    private static bool TryReadImageSessionResource(
        MemoryReadSession session,
        MemoryAddress imageSession,
        out MemoryAddress resource)
    {
        if (imageSession.IsNull)
        {
            resource = MemoryAddress.Null;
            return true;
        }

        if (imageSession.TryOffset(0x10, out var resourceAddress) &&
            session.TryReadPointer(
                resourceAddress,
                out resource,
                out _))
        {
            return true;
        }

        resource = MemoryAddress.Null;
        return false;
    }

    private static ulong? IdentityOf(MemoryAddress address) =>
        address.IsNull ? null : address.Value;

    private static bool TryReadRuntimeClassName(
        MemoryReadSession session,
        MemoryAddress virtualTable,
        out string className)
    {
        if (virtualTable.IsNull ||
            !virtualTable.TryOffset(
                -sizeof(uint),
                out var locatorPointerAddress) ||
            !session.TryReadPointer(
                locatorPointerAddress,
                out var locator,
                out _) ||
            locator.IsNull ||
            !locator.TryOffset(0x0C, out var typePointerAddress) ||
            !session.TryReadPointer(
                typePointerAddress,
                out var typeDescriptor,
                out _) ||
            typeDescriptor.IsNull ||
            !typeDescriptor.TryOffset(0x08, out var nameAddress) ||
            !session.TryReadString(
                nameAddress,
                MaximumRuntimeClassName,
                StrictAscii,
                out var decoratedName,
                out _,
                requireTerminator: true))
        {
            className = string.Empty;
            return false;
        }

        className = NormalizeRuntimeClassName(decoratedName!) ?? string.Empty;
        return className.Length > 0;
    }

    private static string? ReadInlineString(
        ReadOnlySpan<byte> bytes,
        int offset,
        int maximumLength)
    {
        try
        {
            var value = ClientText.ReadNullTerminatedAscii(
                bytes.Slice(offset, maximumLength));
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static MemoryAddress ReadAddress(
        ReadOnlySpan<byte> bytes,
        int offset) =>
        new(
            BinaryPrimitives.ReadUInt32LittleEndian(
                bytes.Slice(offset, sizeof(uint))));

    private static ushort ReadUInt16(
        ReadOnlySpan<byte> bytes,
        int offset) =>
        BinaryPrimitives.ReadUInt16LittleEndian(
            bytes.Slice(offset, sizeof(ushort)));

    private readonly record struct NodeObservation(
        MemoryAddress Address,
        MemoryAddress Left,
        MemoryAddress Right,
        uint EntityId,
        MemoryAddress Object);
}
