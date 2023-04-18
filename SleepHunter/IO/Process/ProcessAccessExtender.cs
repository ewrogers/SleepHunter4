using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
    public static class ProcessAccessExtender
    {
        internal static ProcessAccessFlags ToWin32Flags(this ProcessAccess access)
        {
            var flags = ProcessAccessFlags.VmOperation | ProcessAccessFlags.QueryInformation;

            if (access.HasFlag(ProcessAccess.Read))
                flags |= ProcessAccessFlags.VmRead;

            if (access.HasFlag(ProcessAccess.Write))
                flags |= ProcessAccessFlags.VmWrite;

            return flags;
        }
    }
}
