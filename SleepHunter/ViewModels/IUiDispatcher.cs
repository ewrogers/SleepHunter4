using System;
using System.Threading;
using System.Threading.Tasks;

namespace SleepHunter.ViewModels
{
    public interface IUiDispatcher
    {
        ValueTask InvokeAsync(
            Action action,
            CancellationToken cancellationToken = default);
    }
}
