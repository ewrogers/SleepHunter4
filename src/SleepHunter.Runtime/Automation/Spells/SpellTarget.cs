namespace SleepHunter.Runtime.Automation.Spells;

public sealed record SpellTarget
{
    public static SpellTarget None { get; } = new(
        SpellTargetKind.None,
        characterName: null,
        x: null,
        y: null);

    public static SpellTarget Self { get; } = new(
        SpellTargetKind.Self,
        characterName: null,
        x: null,
        y: null);

    private SpellTarget(
        SpellTargetKind kind,
        string? characterName,
        int? x,
        int? y)
    {
        Kind = kind;
        CharacterName = characterName;
        X = x;
        Y = y;
    }

    public SpellTargetKind Kind { get; }

    public string? CharacterName { get; }

    public int? X { get; }

    public int? Y { get; }

    public static SpellTarget Character(string characterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterName);

        return new SpellTarget(
            SpellTargetKind.Character,
            characterName.Trim(),
            x: null,
            y: null);
    }

    public static SpellTarget RelativeTile(int x, int y) =>
        Coordinates(SpellTargetKind.RelativeTile, x, y);

    public static SpellTarget AbsoluteTile(int x, int y)
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

        return Coordinates(SpellTargetKind.AbsoluteTile, x, y);
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

        return Coordinates(SpellTargetKind.ScreenPoint, x, y);
    }

    private static SpellTarget Coordinates(
        SpellTargetKind kind,
        int x,
        int y) =>
        new(
            kind,
            characterName: null,
            x,
            y);
}
