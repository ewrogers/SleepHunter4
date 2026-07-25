namespace SleepHunter.Runtime.Snapshots;

public sealed record EquipmentSnapshot
{
    public EquipmentSnapshot(
        string? weaponName,
        string? shieldName = null)
    {
        WeaponName = string.IsNullOrWhiteSpace(weaponName)
            ? null
            : weaponName.Trim();
        ShieldName = string.IsNullOrWhiteSpace(shieldName)
            ? null
            : shieldName.Trim();
    }

    public string? WeaponName { get; }

    public string? ShieldName { get; }

    public bool IsDisarmed => WeaponName is null && ShieldName is null;
}
