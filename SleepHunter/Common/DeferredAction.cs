using System;

namespace SleepHunter.Common
{
    public readonly record struct DeferredAction(Action Action, DateTime ExecutionTime) { }
}
