using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SleepHunter.Threading
{
    [AsyncMethodBuilder(typeof(UITaskMethodBuilder))]
    public class UITask
    {
        private readonly TaskCompletionSource tcs = new();

        public Task AsTask() => tcs.Task;

        public TaskAwaiter GetAwaiter() => tcs.Task.GetAwaiter();

        public void SetResult() => tcs.SetResult();

        public void SetException(Exception exception) => tcs.SetException(exception);


        public static implicit operator Task(UITask task) => task.AsTask();
    }
}
