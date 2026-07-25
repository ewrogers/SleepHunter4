namespace SleepHunter.Persistence.Serialization;

public sealed class MacroConfigurationException : Exception
{
    public MacroConfigurationException(string message)
        : base(message)
    {
    }

    public MacroConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
