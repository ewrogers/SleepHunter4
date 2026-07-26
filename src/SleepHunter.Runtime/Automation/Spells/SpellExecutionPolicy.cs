using SleepHunter.Runtime.Automation.Dialogs;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Staves;

namespace SleepHunter.Runtime.Automation.Spells;

public sealed record SpellExecutionPolicy
{
    public static SpellExecutionPolicy Default { get; } = new(
        SpellCastPolicy.Default,
        PanelTransitionPolicy.Default,
        allowStaffSwitching: true,
        staffEquipment: StaffEquipmentPolicy.Default,
        dialog: DialogPolicy.Default);

    public SpellExecutionPolicy(
        SpellCastPolicy? cast = null,
        PanelTransitionPolicy? panelTransition = null,
        bool allowStaffSwitching = true,
        StaffEquipmentPolicy? staffEquipment = null,
        DialogPolicy? dialog = null)
    {
        Cast = cast ?? SpellCastPolicy.Default;
        PanelTransition = panelTransition ?? PanelTransitionPolicy.Default;
        AllowStaffSwitching = allowStaffSwitching;
        StaffEquipment = staffEquipment ?? StaffEquipmentPolicy.Default;
        Dialog = dialog ?? DialogPolicy.Default;
    }

    public SpellCastPolicy Cast { get; }

    public PanelTransitionPolicy PanelTransition { get; }

    public bool AllowStaffSwitching { get; }

    public StaffEquipmentPolicy StaffEquipment { get; }

    public DialogPolicy Dialog { get; }
}
