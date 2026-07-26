namespace SleepHunter.Runtime.Snapshots;

public sealed record WorldEntitySnapshot
{
    public WorldEntitySnapshot(
        uint id,
        WorldEntityType type,
        int x,
        int y,
        ushort? sprite = null,
        string? name = null,
        byte? dyeColor = null,
        byte drawLayer = 0,
        uint broadCategory = 0,
        byte collisionLevel = 0,
        byte? direction = null,
        byte? creatureType = null,
        bool isLocalPlayer = false,
        HumanAppearanceSnapshot? humanAppearance = null,
        string? runtimeClassName = null,
        WorldAppearanceKind appearanceKind = WorldAppearanceKind.Unknown,
        ulong? imageSessionIdentity = null,
        ulong? appearanceResourceIdentity = null,
        ulong? imageSessionResourceIdentity = null,
        bool? usesHumanAppearance = null)
    {
        if (id == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                id,
                "World entity identifiers must be positive.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "The world entity type is not supported.");
        }

        if (!Enum.IsDefined(appearanceKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(appearanceKind),
                appearanceKind,
                "The world appearance kind is not supported.");
        }

        Id = id;
        Type = type;
        X = x;
        Y = y;
        Sprite = sprite;
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        DyeColor = dyeColor;
        DrawLayer = drawLayer;
        BroadCategory = broadCategory;
        CollisionLevel = collisionLevel;
        Direction = direction;
        CreatureType = creatureType;
        IsLocalPlayer = isLocalPlayer;
        HumanAppearance = humanAppearance;
        RuntimeClassName = string.IsNullOrWhiteSpace(runtimeClassName)
            ? null
            : runtimeClassName.Trim();
        AppearanceKind = appearanceKind;
        ImageSessionIdentity = NormalizeIdentity(imageSessionIdentity);
        AppearanceResourceIdentity =
            NormalizeIdentity(appearanceResourceIdentity);
        ImageSessionResourceIdentity =
            NormalizeIdentity(imageSessionResourceIdentity);
        UsesHumanAppearance = usesHumanAppearance;
    }

    public uint Id { get; }

    public WorldEntityType Type { get; }

    public int X { get; }

    public int Y { get; }

    public ushort? Sprite { get; }

    public string? Name { get; }

    public byte? DyeColor { get; }

    public byte DrawLayer { get; }

    public uint BroadCategory { get; }

    public byte CollisionLevel { get; }

    public byte? Direction { get; }

    public byte? CreatureType { get; }

    public bool IsLocalPlayer { get; }

    public HumanAppearanceSnapshot? HumanAppearance { get; }

    public string? RuntimeClassName { get; }

    public WorldAppearanceKind AppearanceKind { get; }

    public ulong? ImageSessionIdentity { get; }

    public ulong? AppearanceResourceIdentity { get; }

    public ulong? ImageSessionResourceIdentity { get; }

    public bool? UsesHumanAppearance { get; }

    public bool IsMonsterDisguise =>
        Type == WorldEntityType.Player &&
        UsesHumanAppearance == false;

    private static ulong? NormalizeIdentity(ulong? value) =>
        value is null or 0 ? null : value;
}
