using System;
using System.Collections.Generic;
using System.Threading;

namespace SleepHunter.Common
{
    public sealed class DeferredDispatcher
    {
        private readonly ReaderWriterLockSlim readerWriterLock = new();
        private readonly List<DeferredAction> actions = new();

        public DeferredDispatcher() { }

        public void DispatchAfter(Action action, TimeSpan delay) => DispatchAt(action, DateTime.Now + delay);

        public void DispatchAt(Action action, DateTime timestamp)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            readerWriterLock.EnterWriteLock();

            try
            {
                var deferred = new DeferredAction(action, timestamp);
                actions.Add(deferred);
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        public void Tick()
        {
            readerWriterLock.EnterUpgradeableReadLock();

            try
            {
                for (var i = actions.Count - 1; i >= 0; i--)
                {
                    var action = actions[i];
                    
                    // If execution time is in the future, wait
                    if (action.ExecutionTime > DateTime.Now)
                        continue;

                    // Remove the entry from the deferred action queue
                    readerWriterLock.EnterWriteLock();
                    try
                    {
                        actions.RemoveAt(i);
                    }
                    finally
                    {
                        readerWriterLock.ExitWriteLock();
                    }

                    // Perform the action (outside of the write lock)
                    action.Action();
                }
            }
            finally
            {
                readerWriterLock.ExitUpgradeableReadLock();
            }
        }
    }
}
