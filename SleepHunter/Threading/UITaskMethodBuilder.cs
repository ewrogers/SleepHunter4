using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace SleepHunter.Threading
{
    public sealed class UITaskMethodBuilder
    {
        private readonly Dispatcher dispatcher;

        public UITask Task { get; init; } = new UITask();

        public UITaskMethodBuilder(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            if (!dispatcher.CheckAccess())
                dispatcher.BeginInvoke(new Action(stateMachine.MoveNext));
            else
                stateMachine.MoveNext();
        }

        public static UITaskMethodBuilder Create() => new(Application.Current.Dispatcher);

        public void SetStateMachine(IAsyncStateMachine _) { }

        public void SetResult() => Task.SetResult();

        public void SetException(Exception exception) => Task.SetException(exception);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, TStateMachine stateMachine) 
            where TAwaiter: INotifyCompletion 
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(ResumeAfterAwait(stateMachine));
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, TStateMachine stateMachine)
            where TAwaiter: ICriticalNotifyCompletion
            where TStateMachine: IAsyncStateMachine
        {
            awaiter.UnsafeOnCompleted(ResumeAfterAwait(stateMachine));
        }

        private Action ResumeAfterAwait<TStateMachine>(TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            return () =>
            {
                if (!dispatcher.CheckAccess())
                    dispatcher.BeginInvoke(new Action(stateMachine.MoveNext));
                else
                    stateMachine.MoveNext();
            };
        }
    }
}
