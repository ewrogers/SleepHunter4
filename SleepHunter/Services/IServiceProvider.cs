
namespace SleepHunter.Services
{
    public interface IServiceProvider
    {
        bool IsRegistered<T>();

        T GetService<T>();
    }
}
