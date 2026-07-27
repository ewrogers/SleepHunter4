using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Input;

public sealed class ClientIntentExecutor
{
    private readonly WindowInputDispatcher dispatcher;
    private readonly IClientIntentPlanner planner;

    public ClientIntentExecutor(
        IClientIntentPlanner planner,
        WindowInputDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(planner);
        ArgumentNullException.ThrowIfNull(dispatcher);

        this.planner = planner;
        this.dispatcher = dispatcher;
    }

    public ClientIntentIssueResult Execute(
        ClientActionIntent intent,
        ClientWindowTarget target,
        ClientSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(snapshot);

        var plan = planner.Plan(intent, target, snapshot);
        if (!plan.IsPlanned)
        {
            return new ClientIntentIssueResult(
                intent.ActionId,
                plan.Status == ClientIntentPlanStatus.Unsupported
                    ? ClientIntentIssueStatus.Unsupported
                    : ClientIntentIssueStatus.Rejected,
                plan);
        }

        var dispatch = dispatcher.Dispatch(target, plan.Plan!);
        return new ClientIntentIssueResult(
            intent.ActionId,
            dispatch.Status switch
            {
                WindowInputDispatchStatus.Issued =>
                    ClientIntentIssueStatus.Issued,
                WindowInputDispatchStatus.Rejected =>
                    ClientIntentIssueStatus.Rejected,
                WindowInputDispatchStatus.Failed =>
                    ClientIntentIssueStatus.Failed,
                WindowInputDispatchStatus.PartiallyIssued =>
                    ClientIntentIssueStatus.PartiallyIssued,
                _ => throw new InvalidOperationException(
                    "The input dispatcher returned an unsupported status.")
            },
            plan,
            dispatch);
    }

    public WindowInputDispatchResult CloseClient(
        ClientWindowTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return dispatcher.Dispatch(
            target,
            new WindowInputPlan(
                [
                    new WindowInputMessage(
                        ClientWindowMessage.Close,
                        wParam: 0,
                        lParam: 0)
                ]));
    }
}
