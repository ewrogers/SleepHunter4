using SleepHunter.Interop.Input;
using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Interop.Tests.Input;

public sealed class Usda741ClientIntentPlannerTests
{
    private static readonly ClientIdentity Client = new(
        "process:1234",
        Usda741ClientIntentPlanner.SupportedVersion);

    private static readonly ClientWindowTarget Target = new(
        Client,
        processId: 1234,
        windowHandle: new nint(0x1234),
        clientWidth: 640,
        clientHeight: 480);

    private readonly Usda741ClientIntentPlanner planner = new(
        new FixedVirtualKeyMapper());

    [TestCaseSource(nameof(KeystrokeCases))]
    public void ShouldPlanGuardedKeystrokes(
        ClientActionIntent intent,
        VirtualKey expectedKey,
        byte expectedScanCode)
    {
        var result = planner.Plan(
            intent,
            Target,
            Snapshot(ClientPanel.Inventory));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ClientIntentPlanStatus.Planned));
            Assert.That(
                result.Plan?.Messages,
                Is.EqualTo(
                    new[]
                    {
                        KeyMessage(
                            ClientWindowMessage.KeyDown,
                            expectedKey,
                            expectedScanCode),
                        KeyMessage(
                            ClientWindowMessage.KeyUp,
                            expectedKey,
                            expectedScanCode)
                    }));
            Assert.That(
                result.Plan?.CleanupMessages,
                Is.EqualTo(
                    new[]
                    {
                        KeyMessage(
                            ClientWindowMessage.KeyUp,
                            expectedKey,
                            expectedScanCode)
                    }));
        });
    }

    [Test]
    public void ShouldShiftClickFromTemuairToMedeniaPanelAtScaledCoordinates()
    {
        var target = new ClientWindowTarget(
            Client,
            Target.ProcessId,
            Target.WindowHandle,
            clientWidth: 1280,
            clientHeight: 960);
        var intent = new SwitchPanelIntent(
            new ClientActionId(4),
            ClientPanel.MedeniaSkills);

        var result = planner.Plan(
            intent,
            target,
            Snapshot(ClientPanel.TemuairSkills));

        var point = PackPoint(x: 1090, y: 720);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ClientIntentPlanStatus.Planned));
            Assert.That(
                result.Plan?.Messages,
                Is.EqualTo(
                    new[]
                    {
                        KeyMessage(
                            ClientWindowMessage.KeyDown,
                            VirtualKey.Shift,
                            scanCode: 0x2A),
                        new WindowInputMessage(
                            ClientWindowMessage.MouseMove,
                            wParam: 0,
                            point),
                        new WindowInputMessage(
                            ClientWindowMessage.LeftButtonDown,
                            wParam: 1,
                            point),
                        new WindowInputMessage(
                            ClientWindowMessage.LeftButtonUp,
                            wParam: 0,
                            point),
                        KeyMessage(
                            ClientWindowMessage.KeyUp,
                            VirtualKey.Shift,
                            scanCode: 0x2A)
                    }));
            Assert.That(
                result.Plan?.CleanupMessages,
                Is.EqualTo(
                    new[]
                    {
                        new WindowInputMessage(
                            ClientWindowMessage.LeftButtonUp,
                            wParam: 0,
                            point),
                        KeyMessage(
                            ClientWindowMessage.KeyUp,
                            VirtualKey.Shift,
                            scanCode: 0x2A)
                    }));
        });
    }

    [Test]
    public void ShouldClickWithoutShiftFromMedeniaToTemuairPanel()
    {
        var intent = new SwitchPanelIntent(
            new ClientActionId(5),
            ClientPanel.TemuairSkills);

        var result = planner.Plan(
            intent,
            Target,
            Snapshot(ClientPanel.MedeniaSkills));
        var plan = result.Plan!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ClientIntentPlanStatus.Planned));
            Assert.That(plan.Messages, Has.Length.EqualTo(3));
            Assert.That(
                plan.Messages.Any(
                    message =>
                        message.Message is
                            ClientWindowMessage.KeyDown or
                            ClientWindowMessage.KeyUp),
                Is.False);
        });
    }

    [TestCase(36, ClientPanel.TemuairSkills, 495, 420)]
    [TestCase(37, ClientPanel.MedeniaSkills, 110, 350)]
    [TestCase(73, ClientPanel.WorldSkills, 110, 350)]
    [TestCase(90, ClientPanel.WorldSkills, 285, 420)]
    public void ShouldDoubleClickTheCorrectRelativeSkillSlot(
        int slot,
        ClientPanel panel,
        int expectedX,
        int expectedY)
    {
        var intent = new UseSkillIntent(
            new ClientActionId(slot),
            "Test Skill",
            slot,
            panel);

        var result = planner.Plan(intent, Target, Snapshot(panel));
        var plan = result.Plan!;

        var point = PackPoint(expectedX, expectedY);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ClientIntentPlanStatus.Planned));
            Assert.That(plan.Messages, Has.Length.EqualTo(6));
            Assert.That(
                plan.Messages.Select(message => message.LParam),
                Is.All.EqualTo(point));
            Assert.That(
                plan.Messages.Select(message => message.Message),
                Is.EqualTo(
                    new[]
                    {
                        ClientWindowMessage.MouseMove,
                        ClientWindowMessage.LeftButtonDown,
                        ClientWindowMessage.LeftButtonUp,
                        ClientWindowMessage.MouseMove,
                        ClientWindowMessage.LeftButtonDown,
                        ClientWindowMessage.LeftButtonUp
                    }));
            Assert.That(
                plan.Messages
                    .Where(message => message.Message == ClientWindowMessage.LeftButtonUp)
                    .Select(message => message.WParam),
                Is.All.EqualTo((nuint)0));
        });
    }

    [Test]
    public void ShouldRejectARequestedSkillOnTheWrongObservedPanel()
    {
        var intent = new UseSkillIntent(
            new ClientActionId(6),
            "Test Skill",
            slot: 37,
            ClientPanel.MedeniaSkills);

        var result = planner.Plan(
            intent,
            Target,
            Snapshot(ClientPanel.TemuairSkills));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ClientIntentPlanStatus.Rejected));
            Assert.That(result.Failure, Is.EqualTo(ClientIntentPlanFailure.PanelMismatch));
            Assert.That(result.Plan, Is.Null);
        });
    }

    [Test]
    public void ShouldRejectAnAlreadyActiveEquivalentPanel()
    {
        var intent = new SwitchPanelIntent(
            new ClientActionId(7),
            ClientPanel.WorldSpells);

        var result = planner.Plan(
            intent,
            Target,
            Snapshot(ClientPanel.WorldSkills));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ClientIntentPlanStatus.Rejected));
            Assert.That(result.Failure, Is.EqualTo(ClientIntentPlanFailure.AlreadySatisfied));
        });
    }

    [Test]
    public void ShouldRejectAnIncompleteSnapshot()
    {
        var result = planner.Plan(
            new CancelDialogIntent(new ClientActionId(8)),
            Target,
            Snapshot(
                ClientPanel.Inventory,
                quality: SnapshotQuality.Partial));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ClientIntentPlanStatus.Rejected));
            Assert.That(
                result.Failure,
                Is.EqualTo(ClientIntentPlanFailure.SnapshotUnavailable));
        });
    }

    [Test]
    public void ShouldRejectALoggedOutClient()
    {
        var result = planner.Plan(
            new CancelDialogIntent(new ClientActionId(9)),
            Target,
            Snapshot(
                ClientPanel.Inventory,
                presence: ClientPresence.LoggedOut));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ClientIntentPlanStatus.Rejected));
            Assert.That(
                result.Failure,
                Is.EqualTo(ClientIntentPlanFailure.ClientNotInWorld));
        });
    }

    [Test]
    public void ShouldRejectAMismatchedClient()
    {
        var otherTarget = new ClientWindowTarget(
            new ClientIdentity(
                "process:5678",
                Usda741ClientIntentPlanner.SupportedVersion),
            processId: 5678,
            windowHandle: new nint(0x5678),
            clientWidth: 640,
            clientHeight: 480);

        var result = planner.Plan(
            new CancelDialogIntent(new ClientActionId(10)),
            otherTarget,
            Snapshot(ClientPanel.Inventory));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ClientIntentPlanStatus.Rejected));
            Assert.That(result.Failure, Is.EqualTo(ClientIntentPlanFailure.ClientMismatch));
        });
    }

    [Test]
    public void ShouldRejectAnUnsupportedClientVersion()
    {
        var unsupportedClient = new ClientIdentity(
            Client.InstanceId,
            "USDA 7.42");
        var target = new ClientWindowTarget(
            unsupportedClient,
            Target.ProcessId,
            Target.WindowHandle,
            Target.ClientWidth,
            Target.ClientHeight);

        var result = planner.Plan(
            new CancelDialogIntent(new ClientActionId(11)),
            target,
            Snapshot(
                ClientPanel.Inventory,
                client: unsupportedClient));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ClientIntentPlanStatus.Unsupported));
            Assert.That(
                result.Failure,
                Is.EqualTo(ClientIntentPlanFailure.UnsupportedClientVersion));
        });
    }

    [Test]
    public void ShouldReportUnimplementedClientIntentsAsUnsupported()
    {
        var cast = new CastSpellIntent(
            new ClientActionId(12),
            "Test Spell",
            slot: 1,
            ClientPanel.TemuairSpells,
            SpellTarget.Self);
        var equip = new EquipWeaponIntent(
            new ClientActionId(13),
            "Test Staff",
            inventorySlot: 1);

        var castResult = planner.Plan(
            cast,
            Target,
            Snapshot(ClientPanel.TemuairSpells));
        var equipResult = planner.Plan(
            equip,
            Target,
            Snapshot(ClientPanel.Inventory));

        Assert.Multiple(() =>
        {
            Assert.That(
                castResult.Status,
                Is.EqualTo(ClientIntentPlanStatus.Unsupported));
            Assert.That(
                equipResult.Status,
                Is.EqualTo(ClientIntentPlanStatus.Unsupported));
            Assert.That(
                castResult.Failure,
                Is.EqualTo(ClientIntentPlanFailure.UnsupportedIntent));
            Assert.That(
                equipResult.Failure,
                Is.EqualTo(ClientIntentPlanFailure.UnsupportedIntent));
        });
    }

    [Test]
    public void ShouldRejectInputWhenTheRequiredScanCodeIsUnavailable()
    {
        var unavailablePlanner = new Usda741ClientIntentPlanner(
            new FixedVirtualKeyMapper(unavailableKey: VirtualKey.Escape));

        var result = unavailablePlanner.Plan(
            new CancelDialogIntent(new ClientActionId(14)),
            Target,
            Snapshot(ClientPanel.Inventory));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ClientIntentPlanStatus.Rejected));
            Assert.That(
                result.Failure,
                Is.EqualTo(ClientIntentPlanFailure.InputUnavailable));
        });
    }

    private static IEnumerable<TestCaseData> KeystrokeCases()
    {
        yield return new TestCaseData(
                new CancelDialogIntent(new ClientActionId(1)),
                VirtualKey.Escape,
                (byte)0x01)
            .SetName("ShouldPlanEscapeForCancelDialog");
        yield return new TestCaseData(
                new DisarmIntent(new ClientActionId(2)),
                VirtualKey.Oem3,
                (byte)0x29)
            .SetName("ShouldPlanTildeForDisarm");
        yield return new TestCaseData(
                new AssailIntent(new ClientActionId(3), "Assail"),
                VirtualKey.Space,
                (byte)0x39)
            .SetName("ShouldPlanSpaceForAssail");
    }

    private static ClientSnapshot Snapshot(
        ClientPanel panel,
        SnapshotQuality quality = SnapshotQuality.Complete,
        ClientPresence presence = ClientPresence.InWorld,
        ClientIdentity? client = null) =>
        new(
            new SnapshotSequence(1),
            MacroTimestamp.Zero,
            MacroTimestamp.Zero,
            client ?? Client,
            quality,
            presence,
            panel);

    private static WindowInputMessage KeyMessage(
        ClientWindowMessage message,
        VirtualKey key,
        byte scanCode)
    {
        var lParam = 1u | ((uint)scanCode << 16);
        if (message == ClientWindowMessage.KeyUp)
        {
            lParam |= (1u << 30) | (1u << 31);
        }

        return new WindowInputMessage(
            message,
            (nuint)key,
            new nint(unchecked((int)lParam)));
    }

    private static nint PackPoint(int x, int y)
    {
        var lParam =
            (uint)(ushort)x |
            ((uint)(ushort)y << 16);
        return new nint(unchecked((int)lParam));
    }

    private sealed class FixedVirtualKeyMapper : IVirtualKeyMapper
    {
        private readonly VirtualKey? unavailableKey;

        public FixedVirtualKeyMapper(VirtualKey? unavailableKey = null)
        {
            this.unavailableKey = unavailableKey;
        }

        public bool TryMapScanCode(VirtualKey key, out byte scanCode)
        {
            if (key == unavailableKey)
            {
                scanCode = default;
                return false;
            }

            scanCode = key switch
            {
                VirtualKey.Shift => 0x2A,
                VirtualKey.Escape => 0x01,
                VirtualKey.Space => 0x39,
                VirtualKey.Oem3 => 0x29,
                _ => default
            };
            return scanCode != default;
        }
    }
}
