namespace SleepHunter.Interop.Mappings;

public sealed record ClientMemoryMapLoadLimits
{
    public static ClientMemoryMapLoadLimits Default { get; } = new();

    public ClientMemoryMapLoadLimits(
        long maximumCharacters = 1024 * 1024,
        int maximumClients = 64,
        int maximumVariables = 2048,
        int maximumOffsetsPerVariable = 16)
    {
        if (maximumCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumCharacters),
                maximumCharacters,
                "The mapping document character limit must be positive.");
        }

        if (maximumClients <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumClients),
                maximumClients,
                "The client limit must be positive.");
        }

        if (maximumVariables <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumVariables),
                maximumVariables,
                "The variable limit must be positive.");
        }

        if (maximumOffsetsPerVariable <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumOffsetsPerVariable),
                maximumOffsetsPerVariable,
                "The pointer offset limit must be positive.");
        }

        MaximumCharacters = maximumCharacters;
        MaximumClients = maximumClients;
        MaximumVariables = maximumVariables;
        MaximumOffsetsPerVariable = maximumOffsetsPerVariable;
    }

    public long MaximumCharacters { get; }

    public int MaximumClients { get; }

    public int MaximumVariables { get; }

    public int MaximumOffsetsPerVariable { get; }
}
