namespace SleepHunter.Interop.Input;

internal static class Usda741InputMessages
{
    private const nuint LeftButton = 0x0001;

    public static bool TryKeystroke(
        IVirtualKeyMapper mapper,
        VirtualKey key,
        out WindowInputPlan? plan)
    {
        if (!mapper.TryMapScanCode(key, out var scanCode))
        {
            plan = null;
            return false;
        }

        var down = KeyMessage(
            ClientWindowMessage.KeyDown,
            key,
            scanCode);
        var up = KeyMessage(
            ClientWindowMessage.KeyUp,
            key,
            scanCode);
        plan = new WindowInputPlan([down, up], [up]);
        return true;
    }

    public static WindowInputPlan DoubleClick(ClientPoint point)
    {
        var move = MouseMessage(
            ClientWindowMessage.MouseMove,
            wParam: 0,
            point);
        var down = MouseMessage(
            ClientWindowMessage.LeftButtonDown,
            LeftButton,
            point);
        var up = MouseMessage(
            ClientWindowMessage.LeftButtonUp,
            wParam: 0,
            point);
        return new WindowInputPlan(
            [move, down, up, move, down, up],
            [up]);
    }

    public static bool TryClick(
        IVirtualKeyMapper mapper,
        ClientPoint point,
        bool withShift,
        out WindowInputPlan? plan)
    {
        var move = MouseMessage(
            ClientWindowMessage.MouseMove,
            wParam: 0,
            point);
        var down = MouseMessage(
            ClientWindowMessage.LeftButtonDown,
            LeftButton,
            point);
        var up = MouseMessage(
            ClientWindowMessage.LeftButtonUp,
            wParam: 0,
            point);
        if (!withShift)
        {
            plan = new WindowInputPlan(
                [move, down, up],
                [up]);
            return true;
        }

        if (!mapper.TryMapScanCode(
                VirtualKey.Shift,
                out var shiftScanCode))
        {
            plan = null;
            return false;
        }

        var shiftDown = KeyMessage(
            ClientWindowMessage.KeyDown,
            VirtualKey.Shift,
            shiftScanCode);
        var shiftUp = KeyMessage(
            ClientWindowMessage.KeyUp,
            VirtualKey.Shift,
            shiftScanCode);
        plan = new WindowInputPlan(
            [shiftDown, move, down, up, shiftUp],
            [up, shiftUp]);
        return true;
    }

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

    private static WindowInputMessage MouseMessage(
        ClientWindowMessage message,
        nuint wParam,
        ClientPoint point)
    {
        var lParam =
            (uint)(ushort)point.X |
            ((uint)(ushort)point.Y << 16);
        return new WindowInputMessage(
            message,
            wParam,
            new nint(unchecked((int)lParam)));
    }

    internal readonly record struct ClientPoint(int X, int Y);
}
