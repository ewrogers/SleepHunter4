using System;

namespace SleepHunter.Services
{
    public interface IServiceProvider : IDisposable
    {
        bool IsRegistered<T>();

        T GetService<T>();
    }
}
