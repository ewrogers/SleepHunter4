using System.Collections.Immutable;
using SleepHunter.Interop.Memory;

namespace SleepHunter.Interop.Mappings;

public sealed class ClientMemoryMap
{
    private readonly ImmutableDictionary<string, MemoryVariableDefinition>
        variables;

    public ClientMemoryMap(
        PointerWidth pointerWidth,
        IEnumerable<MemoryVariableDefinition> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        if (!Enum.IsDefined(pointerWidth))
        {
            throw new ArgumentOutOfRangeException(
                nameof(pointerWidth),
                pointerWidth,
                "The client pointer width is not supported.");
        }

        var builder = ImmutableDictionary.CreateBuilder<
            string,
            MemoryVariableDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var variable in variables)
        {
            ArgumentNullException.ThrowIfNull(variable);
            if (!builder.TryAdd(variable.Key, variable))
            {
                throw new ArgumentException(
                    $"Memory variable '{variable.Key}' is duplicated.",
                    nameof(variables));
            }
        }

        PointerWidth = pointerWidth;
        this.variables = builder.ToImmutable();
    }

    public PointerWidth PointerWidth { get; }

    public IReadOnlyDictionary<string, MemoryVariableDefinition> Variables =>
        variables;

    public MemoryVariableDefinition? Find(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return variables.GetValueOrDefault(key.Trim());
    }
}
