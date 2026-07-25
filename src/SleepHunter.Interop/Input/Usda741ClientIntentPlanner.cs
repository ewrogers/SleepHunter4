using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Input;

public sealed class Usda741ClientIntentPlanner : IClientIntentPlanner
{
    public const string SupportedVersion = "USDA 7.41";

    private const int BaseClientWidth = 640;
    private const int BaseClientHeight = 480;
    private const int PanelX = 545;
    private const int InventoryToggleX = 570;
    private const int InventoryToggleY = 320;
    private const int SlotOriginX = 110;
    private const int SlotOriginY = 350;
    private const int ExpandedSlotOriginY = 285;
    private const int SlotSize = 35;

    private readonly IVirtualKeyMapper keyMapper;

    public Usda741ClientIntentPlanner(IVirtualKeyMapper keyMapper)
    {
        ArgumentNullException.ThrowIfNull(keyMapper);
        this.keyMapper = keyMapper;
    }

    public ClientIntentPlanResult Plan(
        ClientActionIntent intent,
        ClientWindowTarget target,
        ClientSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(snapshot);

        var invalidContext = ValidateContext(intent, target, snapshot);
        if (invalidContext is not null)
        {
            return invalidContext;
        }

        return intent switch
        {
            CancelDialogIntent =>
                Keystroke(intent, VirtualKey.Escape),
            DisarmIntent =>
                Keystroke(intent, VirtualKey.Oem3),
            AssailIntent =>
                Keystroke(intent, VirtualKey.Space),
            ExpandInventoryIntent expandInventory =>
                PlanInventoryMode(
                    expandInventory,
                    target,
                    snapshot,
                    targetInventoryExpanded: true),
            CollapseInventoryIntent collapseInventory =>
                PlanInventoryMode(
                    collapseInventory,
                    target,
                    snapshot,
                    targetInventoryExpanded: false),
            SwitchPanelIntent switchPanel =>
                PlanPanelSwitch(switchPanel, target, snapshot),
            UseSkillIntent useSkill =>
                PlanSkill(useSkill, target, snapshot),
            EquipWeaponIntent equipWeapon =>
                PlanWeapon(equipWeapon, target, snapshot),
            _ => ClientIntentPlanResult.Unsupported(
                intent.ActionId,
                ClientIntentPlanFailure.UnsupportedIntent,
                $"Intent type '{intent.GetType().Name}' does not yet have a USDA 7.41 input plan.")
        };
    }

    private ClientIntentPlanResult Keystroke(
        ClientActionIntent intent,
        VirtualKey key) =>
        Usda741InputMessages.TryKeystroke(
            keyMapper,
            key,
            out var plan)
            ? ClientIntentPlanResult.Planned(intent.ActionId, plan!)
            : ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.InputUnavailable,
                $"Virtual key '{key}' could not be mapped to a scan code.");

    private static ClientIntentPlanResult PlanInventoryMode(
        ClientActionIntent intent,
        ClientWindowTarget target,
        ClientSnapshot snapshot,
        bool targetInventoryExpanded)
    {
        if (snapshot.ActivePanel != ClientPanel.Inventory)
        {
            return ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.PanelMismatch,
                "The inventory display mode can change only from the inventory panel.");
        }

        if (snapshot.IsInventoryExpanded == targetInventoryExpanded)
        {
            return ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.AlreadySatisfied,
                "The requested inventory display mode is already active.");
        }

        var basePoint = new Usda741InputMessages.ClientPoint(
            InventoryToggleX,
            InventoryToggleY);
        if (!TryScalePoint(target, basePoint, out var point))
        {
            return CoordinateFailure(intent);
        }

        return ClientIntentPlanResult.Planned(
            intent.ActionId,
            Usda741InputMessages.Click(point));
    }

    private ClientIntentPlanResult PlanPanelSwitch(
        SwitchPanelIntent intent,
        ClientWindowTarget target,
        ClientSnapshot snapshot)
    {
        var currentPanel = snapshot.ActivePanel;
        if (currentPanel == ClientPanel.Unknown)
        {
            return ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.SnapshotUnavailable,
                "The active client panel is unknown.");
        }

        if (currentPanel.IsEquivalentTo(intent.TargetPanel))
        {
            return ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.AlreadySatisfied,
                "The target client panel is already active.");
        }

        var basePoint = new Usda741InputMessages.ClientPoint(
            PanelX,
            PanelY(intent.TargetPanel));
        if (!TryScalePoint(target, basePoint, out var point))
        {
            return CoordinateFailure(intent);
        }

        var withShift =
            IsTemuairToMedenia(currentPanel, intent.TargetPanel) ||
            (!IsMedeniaToTemuair(currentPanel, intent.TargetPanel) &&
             IsMedeniaPanel(intent.TargetPanel));
        return Usda741InputMessages.TryClick(
            keyMapper,
            point,
            withShift,
            out var plan)
            ? ClientIntentPlanResult.Planned(intent.ActionId, plan!)
            : ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.InputUnavailable,
                "The Shift key could not be mapped to a scan code.");
    }

    private static ClientIntentPlanResult PlanSkill(
        UseSkillIntent intent,
        ClientWindowTarget target,
        ClientSnapshot snapshot)
    {
        if (!snapshot.ActivePanel.IsEquivalentTo(intent.Panel))
        {
            return ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.PanelMismatch,
                "The observed client panel does not contain the requested skill.");
        }

        var relativeSlot = RelativeSlot(intent.Slot, intent.Panel);
        var rowSize = intent.Panel == ClientPanel.WorldSkills
            ? 6
            : 12;
        var row = (relativeSlot - 1) / rowSize;
        var column = (relativeSlot - 1) % rowSize;
        var basePoint = new Usda741InputMessages.ClientPoint(
            SlotOriginX + (column * SlotSize),
            SlotOriginY + (row * SlotSize));
        if (!TryScalePoint(target, basePoint, out var point))
        {
            return CoordinateFailure(intent);
        }

        return ClientIntentPlanResult.Planned(
            intent.ActionId,
            Usda741InputMessages.DoubleClick(point));
    }

    private ClientIntentPlanResult PlanWeapon(
        EquipWeaponIntent intent,
        ClientWindowTarget target,
        ClientSnapshot snapshot)
    {
        if (intent.IsUnequip)
        {
            return Keystroke(intent, VirtualKey.Oem3);
        }

        if (snapshot.ActivePanel != ClientPanel.Inventory)
        {
            return ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.PanelMismatch,
                "Equipping a weapon requires the inventory panel.");
        }

        var inventorySlot = intent.InventorySlot!.Value;
        var requiresExpandedInventory =
            inventorySlot > InventoryItemSnapshot.MaximumCollapsedSlot;
        if (snapshot.IsInventoryExpanded != requiresExpandedInventory)
        {
            return ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.InventoryModeMismatch,
                "The observed inventory display mode does not expose the requested slot.");
        }

        var selectedItem = snapshot.Inventory?.Items.FirstOrDefault(
            item => item.Slot == inventorySlot);
        if (selectedItem is null ||
            !string.Equals(
                selectedItem.Name,
                intent.StaffName,
                StringComparison.OrdinalIgnoreCase))
        {
            return ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.InventoryItemMismatch,
                "The requested staff is not present in the observed inventory slot.");
        }

        var row = (inventorySlot - 1) / 12;
        var column = (inventorySlot - 1) % 12;
        var originY = snapshot.IsInventoryExpanded
            ? ExpandedSlotOriginY
            : SlotOriginY;
        var basePoint = new Usda741InputMessages.ClientPoint(
            SlotOriginX + (column * SlotSize),
            originY + (row * SlotSize));
        if (!TryScalePoint(target, basePoint, out var point))
        {
            return CoordinateFailure(intent);
        }

        return ClientIntentPlanResult.Planned(
            intent.ActionId,
            Usda741InputMessages.DoubleClick(point));
    }

    private static ClientIntentPlanResult? ValidateContext(
        ClientActionIntent intent,
        ClientWindowTarget target,
        ClientSnapshot snapshot)
    {
        if (snapshot.Client != target.Client)
        {
            return ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.ClientMismatch,
                "The snapshot and target window belong to different clients.");
        }

        if (!string.Equals(
                snapshot.Client.Version,
                SupportedVersion,
                StringComparison.OrdinalIgnoreCase))
        {
            return ClientIntentPlanResult.Unsupported(
                intent.ActionId,
                ClientIntentPlanFailure.UnsupportedClientVersion,
                $"Client version '{snapshot.Client.Version}' is not supported by this input planner.");
        }

        if (!snapshot.IsUsable)
        {
            return ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.SnapshotUnavailable,
                "Intent planning requires a complete client snapshot.");
        }

        if (snapshot.Presence != ClientPresence.InWorld)
        {
            return ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.ClientNotInWorld,
                "Intent planning requires an in-world client.");
        }

        return null;
    }

    private static int PanelY(ClientPanel panel) =>
        panel switch
        {
            ClientPanel.Inventory => 340,
            ClientPanel.TemuairSkills or
                ClientPanel.MedeniaSkills => 360,
            ClientPanel.TemuairSpells or
                ClientPanel.MedeniaSpells => 390,
            ClientPanel.Chat or
                ClientPanel.ChatHistory => 410,
            ClientPanel.Stats or
                ClientPanel.Modifiers => 435,
            ClientPanel.WorldSkills or
                ClientPanel.WorldSpells => 460,
            _ => throw new InvalidOperationException(
                "The target panel does not have a USDA 7.41 input coordinate.")
        };

    private static bool IsMedeniaPanel(ClientPanel panel) =>
        panel is ClientPanel.MedeniaSkills or ClientPanel.MedeniaSpells;

    private static bool IsTemuairToMedenia(
        ClientPanel current,
        ClientPanel target) =>
        (current, target) is
            (ClientPanel.TemuairSkills, ClientPanel.MedeniaSkills) or
            (ClientPanel.TemuairSpells, ClientPanel.MedeniaSpells);

    private static bool IsMedeniaToTemuair(
        ClientPanel current,
        ClientPanel target) =>
        (current, target) is
            (ClientPanel.MedeniaSkills, ClientPanel.TemuairSkills) or
            (ClientPanel.MedeniaSpells, ClientPanel.TemuairSpells);

    private static int RelativeSlot(int slot, ClientPanel panel)
    {
        var panelCapacity = panel == ClientPanel.WorldSkills
            ? 18
            : 36;
        return ((slot - 1) % panelCapacity) + 1;
    }

    private static bool TryScalePoint(
        ClientWindowTarget target,
        Usda741InputMessages.ClientPoint basePoint,
        out Usda741InputMessages.ClientPoint point)
    {
        var x = (int)(
            basePoint.X *
            (target.ClientWidth / (double)BaseClientWidth));
        var y = (int)(
            basePoint.Y *
            (target.ClientHeight / (double)BaseClientHeight));
        if (x < 0 ||
            y < 0 ||
            x >= target.ClientWidth ||
            y >= target.ClientHeight ||
            x > short.MaxValue ||
            y > short.MaxValue)
        {
            point = default;
            return false;
        }

        point = new Usda741InputMessages.ClientPoint(x, y);
        return true;
    }

    private static ClientIntentPlanResult CoordinateFailure(
        ClientActionIntent intent) =>
        ClientIntentPlanResult.Rejected(
            intent.ActionId,
            ClientIntentPlanFailure.CoordinateOutOfBounds,
            "The planned client coordinate is outside the guarded client area.");
}
