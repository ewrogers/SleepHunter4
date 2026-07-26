using SleepHunter.Settings;

namespace SleepHunter.Services.Clients
{
    public interface IClientLaunchService
    {
        void Launch(
            ClientLaunchOptions options,
            ClientLayout layout);
    }
}
