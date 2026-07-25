using SleepHunter.Runtime.Automation.Dialogs;
using SleepHunter.Runtime.Automation.Equipment;
using SleepHunter.Runtime.Automation.Panels;

namespace SleepHunter.Runtime.Automation.Skills;

public sealed record SkillExecutionPolicy
{
    public static SkillExecutionPolicy Default { get; } = new();

    public SkillExecutionPolicy(
        SkillUsePolicy? planning = null,
        PanelTransitionPolicy? panelTransition = null,
        DisarmPolicy? disarm = null,
        TimeSpan? actionDuration = null,
        DialogPolicy? dialog = null)
    {
        var resolvedActionDuration =
            actionDuration ?? TimeSpan.FromMilliseconds(100);
        if (resolvedActionDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actionDuration),
                actionDuration,
                "Skill action durations must be positive.");
        }

        Planning = planning ?? SkillUsePolicy.Default;
        PanelTransition = panelTransition ?? PanelTransitionPolicy.Default;
        Disarm = disarm ?? DisarmPolicy.Default;
        ActionDuration = resolvedActionDuration;
        Dialog = dialog ?? DialogPolicy.Default;
    }

    public SkillUsePolicy Planning { get; }

    public PanelTransitionPolicy PanelTransition { get; }

    public DisarmPolicy Disarm { get; }

    public TimeSpan ActionDuration { get; }

    public DialogPolicy Dialog { get; }
}
