using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SleepHunter.ViewModels
{
    public sealed class WpfUiDispatcher : IUiDispatcher
    {
        private readonly Dispatcher dispatcher;

        public WpfUiDispatcher(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher ??
                throw new ArgumentNullException(nameof(dispatcher));
        }

        public ValueTask InvokeAsync(
            Action action,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (dispatcher.CheckAccess())
            {
                cancellationToken.ThrowIfCancellationRequested();
                action();
                return ValueTask.CompletedTask;
            }

            var operation = dispatcher.InvokeAsync(
                action,
                DispatcherPriority.DataBind,
                cancellationToken);
            return new ValueTask(operation.Task);
        }
    }
}
