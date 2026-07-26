using SleepHunter.Runtime.Automation;

namespace SleepHunter.Runtime.Commands;

public sealed record ConfigureAutomationCommand : MacroCommand
{
    public ConfigureAutomationCommand(AutomationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        Configuration = configuration;
    }

    public AutomationConfiguration Configuration { get; }
}

public sealed record ApplyAutomationSetupCommand : MacroCommand
{
    public ApplyAutomationSetupCommand(
        ReplaceQueuesCommand queues,
        AutomationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(queues);
        ArgumentNullException.ThrowIfNull(configuration);
        Queues = queues;
        Configuration = configuration;
    }

    public ReplaceQueuesCommand Queues { get; }

    public AutomationConfiguration Configuration { get; }
}
