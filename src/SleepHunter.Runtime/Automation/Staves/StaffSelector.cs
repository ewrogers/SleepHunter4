using SleepHunter.Runtime.Characters;

namespace SleepHunter.Runtime.Automation.Staves;

public static class StaffSelector
{
    public static StaffSelection Select(StaffSelectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var equippedWeaponName = request.Equipment.WeaponName;
        var available = request.Candidates
            .Where(candidate => IsEligible(candidate, request))
            .Select(candidate => CreateAvailableCandidate(
                candidate,
                equippedWeaponName,
                request))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .OrderBy(candidate => candidate.Staff.CastLines)
            .ThenBy(candidate => candidate.IsEquipped ? 0 : 1)
            .ThenBy(candidate => candidate.InventorySlot ?? int.MaxValue)
            .ThenBy(
                candidate => candidate.Staff.Name,
                StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Staff.Name, StringComparer.Ordinal)
            .ToArray();

        if (available.Length == 0)
        {
            return new StaffSelection(
                StaffSelectionAction.None,
                StaffSelectionReason.NoEligibleStaff,
                request.BaseCastLines,
                staff: null,
                inventorySlot: null);
        }

        var best = available[0];
        var equipped = available.FirstOrDefault(candidate => candidate.IsEquipped);

        if (best.Staff.CastLines >= request.BaseCastLines)
        {
            if (equipped is not null &&
                equipped.Staff.CastLines <= request.BaseCastLines)
            {
                return new StaffSelection(
                    StaffSelectionAction.None,
                    StaffSelectionReason.AlreadyEquipped,
                    equipped.Staff.CastLines,
                    equipped.Staff,
                    inventorySlot: null);
            }

            return new StaffSelection(
                equipped is null
                    ? StaffSelectionAction.None
                    : StaffSelectionAction.Unequip,
                StaffSelectionReason.BaseCastIsOptimal,
                request.BaseCastLines,
                staff: null,
                inventorySlot: null);
        }

        if (best.IsEquipped)
        {
            return new StaffSelection(
                StaffSelectionAction.None,
                StaffSelectionReason.AlreadyEquipped,
                best.Staff.CastLines,
                best.Staff,
                inventorySlot: null);
        }

        return new StaffSelection(
            StaffSelectionAction.Equip,
            StaffSelectionReason.BetterStaffAvailable,
            best.Staff.CastLines,
            best.Staff,
            best.InventorySlot);
    }

    private static bool IsEligible(
        StaffCandidate candidate,
        StaffSelectionRequest request)
    {
        var character = request.Character;
        var classCompatible = candidate.RequiredClass is null ||
                              (character.Class != CharacterClass.Unknown &&
                               candidate.RequiredClass == character.Class);

        return classCompatible &&
               candidate.RequiredLevel <= character.Level &&
               candidate.RequiredAbilityLevel <= character.AbilityLevel;
    }

    private static AvailableStaff? CreateAvailableCandidate(
        StaffCandidate candidate,
        string? equippedWeaponName,
        StaffSelectionRequest request)
    {
        var isEquipped = string.Equals(
            candidate.Name,
            equippedWeaponName,
            StringComparison.OrdinalIgnoreCase);
        var inventoryItem = request.Inventory.FindFirst(candidate.Name);

        return isEquipped || inventoryItem is not null
            ? new AvailableStaff(
                candidate,
                isEquipped,
                inventoryItem?.Slot)
            : null;
    }

    private sealed record AvailableStaff(
        StaffCandidate Staff,
        bool IsEquipped,
        int? InventorySlot);
}
