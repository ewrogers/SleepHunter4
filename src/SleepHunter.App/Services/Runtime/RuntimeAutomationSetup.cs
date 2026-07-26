using SleepHunter.Runtime.Commands;

namespace SleepHunter.Services.Runtime
{
    public sealed record RuntimeAutomationSetup(
        ReplaceQueuesCommand ReplaceQueues,
        ConfigureAutomationCommand ConfigureAutomation);
}
