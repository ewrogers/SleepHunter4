namespace SleepHunter.Runtime.Automation.Spells;

public sealed record SpellTarget
{
    public const int MaximumAreaRadius = 100;

    public static SpellTarget None { get; } = new(
        SpellTargetKind.None,
        characterName: null,
        x: null,
        y: null,
        TargetOffset.Zero,
        innerRadius: null,
        outerRadius: null);

    public static SpellTarget Self { get; } = new(
        SpellTargetKind.Self,
        characterName: null,
        x: null,
        y: null,
        TargetOffset.Zero,
        innerRadius: null,
        outerRadius: null);

    private SpellTarget(
        SpellTargetKind kind,
        string? characterName,
        int? x,
        int? y,
        TargetOffset offset,
        int? innerRadius,
        int? outerRadius)
    {
        Kind = kind;
        CharacterName = characterName;
        X = x;
        Y = y;
        Offset = offset;
        InnerRadius = innerRadius;
        OuterRadius = outerRadius;
    }

    public SpellTargetKind Kind { get; }

    public string? CharacterName { get; }

    public int? X { get; }

    public int? Y { get; }

    public TargetOffset Offset { get; }

    public int? InnerRadius { get; }

    public int? OuterRadius { get; }

    public bool IsArea =>
        Kind is SpellTargetKind.RelativeArea or
            SpellTargetKind.AbsoluteArea;

    public static SpellTarget Character(
        string characterName,
        TargetOffset offset = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterName);

        return new SpellTarget(
            SpellTargetKind.Character,
            characterName.Trim(),
            x: null,
            y: null,
            offset,
            innerRadius: null,
            outerRadius: null);
    }

    public static SpellTarget RelativeTile(
        int x,
        int y,
        TargetOffset offset = default) =>
        Coordinates(SpellTargetKind.RelativeTile, x, y, offset);

    public static SpellTarget AbsoluteTile(
        int x,
        int y,
        TargetOffset offset = default)
    {
        ValidateAbsoluteCoordinates(x, y);

        return Coordinates(SpellTargetKind.AbsoluteTile, x, y, offset);
    }

    public static SpellTarget ScreenPoint(int x, int y)
    {
        if (x < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(x),
                x,
                "Screen X cannot be negative.");
        }

        if (y < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(y),
                y,
                "Screen Y cannot be negative.");
        }

        return Coordinates(
            SpellTargetKind.ScreenPoint,
            x,
            y,
            TargetOffset.Zero);
    }

    public static SpellTarget RelativeArea(
        int x,
        int y,
        int innerRadius,
        int outerRadius,
        TargetOffset offset = default) =>
        Area(
            SpellTargetKind.RelativeArea,
            x,
            y,
            innerRadius,
            outerRadius,
            offset);

    public static SpellTarget AbsoluteArea(
        int x,
        int y,
        int innerRadius,
        int outerRadius,
        TargetOffset offset = default)
    {
        ValidateAbsoluteCoordinates(x, y);
        return Area(
            SpellTargetKind.AbsoluteArea,
            x,
            y,
            innerRadius,
            outerRadius,
            offset);
    }

    public SpellTarget WithOffset(int x, int y)
    {
        if (Kind is SpellTargetKind.None or SpellTargetKind.ScreenPoint)
        {
            throw new InvalidOperationException(
                "This target kind does not support a pixel offset.");
        }

        return new SpellTarget(
            Kind,
            CharacterName,
            X,
            Y,
            new TargetOffset(x, y),
            InnerRadius,
            OuterRadius);
    }

    private static SpellTarget Coordinates(
        SpellTargetKind kind,
        int x,
        int y,
        TargetOffset offset) =>
        new(
            kind,
            characterName: null,
            x,
            y,
            offset,
            innerRadius: null,
            outerRadius: null);

    private static SpellTarget Area(
        SpellTargetKind kind,
        int x,
        int y,
        int innerRadius,
        int outerRadius,
        TargetOffset offset)
    {
        if (innerRadius < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(innerRadius),
                innerRadius,
                "The inner target radius cannot be negative.");
        }

        if (outerRadius <= 0 || outerRadius > MaximumAreaRadius)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outerRadius),
                outerRadius,
                $"The outer target radius must be between 1 and {MaximumAreaRadius}.");
        }

        if (innerRadius > outerRadius)
        {
            throw new ArgumentException(
                "The inner target radius cannot exceed the outer target radius.",
                nameof(innerRadius));
        }

        return new SpellTarget(
            kind,
            characterName: null,
            x,
            y,
            offset,
            innerRadius,
            outerRadius);
    }

    private static void ValidateAbsoluteCoordinates(int x, int y)
    {
        if (x < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(x),
                x,
                "Absolute tile X cannot be negative.");
        }

        if (y < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(y),
                y,
                "Absolute tile Y cannot be negative.");
        }
    }
}
