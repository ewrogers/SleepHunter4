using SleepHunter.Models;

namespace SleepHunter.Services.Clients
{
    public interface IClientProcessScanner
    {
        void RegisterWindowClassName(string className);

        void ScanForProcesses();

        bool TryDequeueAdded(out ClientProcess process);

        bool TryDequeueRemoved(out ClientProcess process);
    }
}
