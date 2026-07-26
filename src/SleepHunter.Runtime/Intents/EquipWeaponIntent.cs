using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Intents;

public sealed record EquipWeaponIntent : ClientActionIntent
{
    public EquipWeaponIntent(
        ClientActionId actionId,
        string? staffName,
        int? inventorySlot)
        : base(actionId)
    {
        var hasStaff = !string.IsNullOrWhiteSpace(staffName);
        if (hasStaff != inventorySlot.HasValue)
        {
            throw new ArgumentException(
                "Equipping requires both a staff name and inventory slot.");
        }

        if (inventorySlot is <= 0 or > InventoryItemSnapshot.MaximumUsableSlot)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inventorySlot),
                inventorySlot,
                "The staff inventory slot is outside the supported range.");
        }

        StaffName = hasStaff
            ? staffName!.Trim()
            : null;
        InventorySlot = inventorySlot;
    }

    public string? StaffName { get; }

    public int? InventorySlot { get; }

    public bool IsUnequip => StaffName is null;
}
