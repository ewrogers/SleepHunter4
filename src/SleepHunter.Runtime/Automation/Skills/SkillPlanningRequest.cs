using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Automation.Skills;

public sealed record SkillPlanningRequest
{
    public SkillPlanningRequest(
        SkillQueueState queue,
        VitalsSnapshot? vitals,
        SkillbookSnapshot? skillbook,
        SkillCooldownState? cooldowns,
        MacroTimestamp currentTime,
        SkillUsePolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(queue);

        Queue = queue;
        Vitals = vitals;
        Skillbook = skillbook;
        Cooldowns = cooldowns ?? SkillCooldownState.Empty;
        CurrentTime = currentTime;
        Policy = policy ?? SkillUsePolicy.Default;
    }

    public SkillQueueState Queue { get; }

    public VitalsSnapshot? Vitals { get; }

    public SkillbookSnapshot? Skillbook { get; }

    public SkillCooldownState Cooldowns { get; }

    public MacroTimestamp CurrentTime { get; }

    public SkillUsePolicy Policy { get; }
}
