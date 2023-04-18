using System;

namespace SleepHunter.Services
{
    internal interface IServiceProvider : IDisposable
    {
        bool IsRegistered<T>();

        T GetService<T>();
    }
}
