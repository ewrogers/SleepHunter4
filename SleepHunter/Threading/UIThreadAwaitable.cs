using System;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace SleepHunter.Threading
{
    public readonly struct UIThreadAwaitable : INotifyCompletion
    {
        private readonly Dispatcher dispatcher;

        public UIThreadAwaitable(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public readonly UIThreadAwaitable GetAwaiter() => this;

        public readonly void GetResult() { }

        public readonly bool IsCompleted => dispatcher.CheckAccess();

        public readonly void OnCompleted(Action continuation) => dispatcher.BeginInvoke(continuation);
    }
}
