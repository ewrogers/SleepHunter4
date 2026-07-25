using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Spells;
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
            CastSpellIntent castSpell =>
                PlanSpell(castSpell, target, snapshot),
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

        if (!TrySlotPoint(
                target,
                intent.Slot,
                intent.Panel,
                out var point))
        {
            return CoordinateFailure(intent);
        }

        return ClientIntentPlanResult.Planned(
            intent.ActionId,
            Usda741InputMessages.DoubleClick(point));
    }

    private static ClientIntentPlanResult PlanSpell(
        CastSpellIntent intent,
        ClientWindowTarget target,
        ClientSnapshot snapshot)
    {
        if (!snapshot.ActivePanel.IsEquivalentTo(intent.Panel))
        {
            return ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.PanelMismatch,
                "The observed client panel does not contain the requested spell.");
        }

        var selectedSpell = snapshot.Spellbook?.Spells.FirstOrDefault(
            spell => spell.Slot == intent.Slot);
        if (selectedSpell is null ||
            !string.Equals(
                selectedSpell.Name,
                intent.SpellName,
                StringComparison.OrdinalIgnoreCase))
        {
            return ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.SpellMismatch,
                "The requested spell is not present in the observed spell slot.");
        }

        if (!TrySlotPoint(
                target,
                intent.Slot,
                intent.Panel,
                out var spellPoint))
        {
            return CoordinateFailure(intent);
        }

        var spellPlan = Usda741InputMessages.DoubleClick(spellPoint);
        if (intent.Target.Kind == SpellTargetKind.None)
        {
            return ClientIntentPlanResult.Planned(
                intent.ActionId,
                spellPlan);
        }

        if (!TryProjectSpellTarget(
                intent,
                target,
                snapshot,
                out var targetPoint,
                out var targetFailure))
        {
            return targetFailure!;
        }

        return ClientIntentPlanResult.Planned(
            intent.ActionId,
            Usda741InputMessages.Sequence(
                spellPlan,
                Usda741InputMessages.Click(targetPoint)));
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

    private static bool TrySlotPoint(
        ClientWindowTarget target,
        int slot,
        ClientPanel panel,
        out Usda741InputMessages.ClientPoint point)
    {
        var isWorldPanel = panel is
            ClientPanel.WorldSkills or
            ClientPanel.WorldSpells;
        var panelCapacity = isWorldPanel
            ? 18
            : 36;
        var relativeSlot = ((slot - 1) % panelCapacity) + 1;
        var rowSize = isWorldPanel
            ? 6
            : 12;
        var columnOffset = panel == ClientPanel.WorldSpells
            ? 6
            : 0;
        var row = (relativeSlot - 1) / rowSize;
        var column = ((relativeSlot - 1) % rowSize) + columnOffset;
        return TryScalePoint(
            target,
            new LogicalPoint(
                SlotOriginX + (column * SlotSize),
                SlotOriginY + (row * SlotSize)),
            TargetOffset.Zero,
            out point);
    }

    private static bool TryScalePoint(
        ClientWindowTarget target,
        Usda741InputMessages.ClientPoint basePoint,
        out Usda741InputMessages.ClientPoint point) =>
        TryScalePoint(
            target,
            new LogicalPoint(basePoint.X, basePoint.Y),
            TargetOffset.Zero,
            out point);

    private static bool TryScalePoint(
        ClientWindowTarget target,
        LogicalPoint basePoint,
        TargetOffset offset,
        out Usda741InputMessages.ClientPoint point)
    {
        var scaledX =
            basePoint.X *
            (target.ClientWidth / (double)BaseClientWidth);
        var scaledY =
            basePoint.Y *
            (target.ClientHeight / (double)BaseClientHeight);
        if (scaledX < int.MinValue ||
            scaledX > int.MaxValue ||
            scaledY < int.MinValue ||
            scaledY > int.MaxValue)
        {
            point = default;
            return false;
        }

        var x = (long)scaledX + offset.X;
        var y = (long)scaledY + offset.Y;
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

        point = new Usda741InputMessages.ClientPoint((int)x, (int)y);
        return true;
    }

    private static bool TryProjectSpellTarget(
        CastSpellIntent intent,
        ClientWindowTarget target,
        ClientSnapshot snapshot,
        out Usda741InputMessages.ClientPoint point,
        out ClientIntentPlanResult? failure)
    {
        LogicalPoint logicalPoint;
        switch (intent.Target.Kind)
        {
            case SpellTargetKind.Self:
                logicalPoint = new LogicalPoint(315, 160);
                break;

            case SpellTargetKind.ScreenPoint:
                logicalPoint = new LogicalPoint(
                    intent.Target.X!.Value,
                    intent.Target.Y!.Value);
                break;

            case SpellTargetKind.RelativeTile:
                logicalPoint = RelativeTilePoint(
                    intent.Target.X!.Value,
                    intent.Target.Y!.Value);
                break;

            case SpellTargetKind.AbsoluteTile:
                if (snapshot.Location is not { } location)
                {
                    point = default;
                    failure = ClientIntentPlanResult.Rejected(
                        intent.ActionId,
                        ClientIntentPlanFailure.TargetUnavailable,
                        "Absolute spell targeting requires an observed map location.");
                    return false;
                }

                var deltaX = (long)intent.Target.X!.Value - location.X;
                var deltaY = (long)intent.Target.Y!.Value - location.Y;
                if (Math.Abs(deltaX) > 10 || Math.Abs(deltaY) > 10)
                {
                    point = default;
                    failure = ClientIntentPlanResult.Rejected(
                        intent.ActionId,
                        ClientIntentPlanFailure.TargetOutOfRange,
                        "The absolute spell target is outside the supported local tile range.");
                    return false;
                }

                logicalPoint = RelativeTilePoint(deltaX, deltaY);
                break;

            case SpellTargetKind.Character:
                point = default;
                failure = ClientIntentPlanResult.Unsupported(
                    intent.ActionId,
                    ClientIntentPlanFailure.UnsupportedTarget,
                    "Character spell targeting requires a coherent target-location observation.");
                return false;

            case SpellTargetKind.RelativeArea:
            case SpellTargetKind.AbsoluteArea:
                point = default;
                failure = ClientIntentPlanResult.Unsupported(
                    intent.ActionId,
                    ClientIntentPlanFailure.UnsupportedTarget,
                    "Area spell targets must be resolved to one tile before input planning.");
                return false;

            default:
                point = default;
                failure = ClientIntentPlanResult.Unsupported(
                    intent.ActionId,
                    ClientIntentPlanFailure.UnsupportedTarget,
                    $"Spell target kind '{intent.Target.Kind}' is not supported.");
                return false;
        }

        if (!TryScalePoint(
                target,
                logicalPoint,
                intent.Target.Offset,
                out point))
        {
            failure = ClientIntentPlanResult.Rejected(
                intent.ActionId,
                ClientIntentPlanFailure.TargetOutOfRange,
                "The projected spell target is outside the guarded client area.");
            return false;
        }

        failure = null;
        return true;
    }

    private static LogicalPoint RelativeTilePoint(
        long deltaX,
        long deltaY) =>
        new(
            315 + (((deltaX - deltaY) / 2.0) * 56),
            160 + (((deltaY + deltaX) / 2.0) * 27));

    private static ClientIntentPlanResult CoordinateFailure(
        ClientActionIntent intent) =>
        ClientIntentPlanResult.Rejected(
            intent.ActionId,
            ClientIntentPlanFailure.CoordinateOutOfBounds,
            "The planned client coordinate is outside the guarded client area.");

    private readonly record struct LogicalPoint(double X, double Y);
}
