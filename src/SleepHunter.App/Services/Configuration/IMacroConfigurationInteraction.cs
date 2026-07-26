namespace SleepHunter.Services.Configuration
{
    public interface IMacroConfigurationInteraction
    {
        string SelectLoadFile(string characterName);

        string SelectSaveFile(string characterName);

        void ShowMessage(
            string title,
            string message,
            string detail);
    }
}
