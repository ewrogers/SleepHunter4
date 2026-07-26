using System;

namespace SleepHunter.Services.Clients
{
    public interface IClientLaunchInteraction
    {
        void ShowError(Exception exception);
    }
}
