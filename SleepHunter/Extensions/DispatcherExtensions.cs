using System.Windows.Threading;
using SleepHunter.Threading;

namespace SleepHunter.Extensions
{
    public static class DispatcherExtensions
    {
        public static UIThreadAwaitable SwitchToUIThread(this Dispatcher dispatcher)
        {
            return new UIThreadAwaitable(dispatcher);
        }
    }
}
