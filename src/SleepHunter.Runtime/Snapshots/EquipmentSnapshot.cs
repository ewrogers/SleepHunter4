namespace SleepHunter.Runtime.Snapshots;

public sealed record EquipmentSnapshot
{
    public EquipmentSnapshot(string? weaponName)
    {
        WeaponName = string.IsNullOrWhiteSpace(weaponName)
            ? null
            : weaponName.Trim();
    }

    public string? WeaponName { get; }
}
