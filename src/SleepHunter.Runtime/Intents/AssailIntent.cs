using SleepHunter.Runtime.Actions;

namespace SleepHunter.Runtime.Intents;

public sealed record AssailIntent : ClientActionIntent
{
    public AssailIntent(ClientActionId actionId, string skillName)
        : base(actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillName);
        SkillName = skillName.Trim();
    }

    public string SkillName { get; }
}
