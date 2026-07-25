namespace SleepHunter.Interop.Input;

public enum VirtualKey : byte
{
    Shift = 0x10,
    Escape = 0x1B,
    Space = 0x20,
    Oem3 = 0xC0
}

public interface IVirtualKeyMapper
{
    bool TryMapScanCode(VirtualKey key, out byte scanCode);
}

public sealed class WindowsVirtualKeyMapper : IVirtualKeyMapper
{
    public bool TryMapScanCode(VirtualKey key, out byte scanCode)
    {
        if (!Enum.IsDefined(key))
        {
            scanCode = default;
            return false;
        }

        var mapped = NativeMethods.MapVirtualKey(
            (uint)key,
            mapType: 0);
        if (mapped is 0 or > byte.MaxValue)
        {
            scanCode = default;
            return false;
        }

        scanCode = (byte)mapped;
        return true;
    }
}
