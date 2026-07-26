namespace SleepHunter.Persistence.Configuration;

public sealed record HotkeyConfiguration
{
    public HotkeyConfiguration(
        string key,
        HotkeyModifiers modifiers = HotkeyModifiers.None)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        const HotkeyModifiers supported =
            HotkeyModifiers.Alt |
            HotkeyModifiers.Control |
            HotkeyModifiers.Shift |
            HotkeyModifiers.Windows;
        if ((modifiers & ~supported) != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(modifiers),
                modifiers,
                "The hotkey contains unsupported modifiers.");
        }

        Key = key.Trim();
        Modifiers = modifiers;
    }

    public string Key { get; }

    public HotkeyModifiers Modifiers { get; }
}
