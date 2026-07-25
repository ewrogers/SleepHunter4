namespace SleepHunter.Services.Hotkeys
{
    public sealed record HotkeyAssignmentResult(
        HotkeyAssignmentStatus Status)
    {
        public bool Succeeded =>
            Status != HotkeyAssignmentStatus.RegistrationFailed;
    }
}
