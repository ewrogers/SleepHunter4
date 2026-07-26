namespace SleepHunter.Models
{
    public enum PlayerUserState : byte
    {
        Awake = 0,
        DoNotDisturb = 1,
        Daydreaming = 2,
        NeedGroup = 3,
        Grouped = 4,
        LoneHunter = 5,
        GroupHunting = 6,
        NeedHelp = 7
    }
}
