namespace SleepHunter.Runtime.Automation.Staves;

public sealed record StaffSelection
{
    internal StaffSelection(
        StaffSelectionAction action,
        StaffSelectionReason reason,
        int castLines,
        StaffCandidate? staff,
        int? inventorySlot)
    {
        Action = action;
        Reason = reason;
        CastLines = castLines;
        Staff = staff;
        InventorySlot = inventorySlot;
    }

    public StaffSelectionAction Action { get; }

    public StaffSelectionReason Reason { get; }

    public int CastLines { get; }

    public StaffCandidate? Staff { get; }

    public int? InventorySlot { get; }
}
