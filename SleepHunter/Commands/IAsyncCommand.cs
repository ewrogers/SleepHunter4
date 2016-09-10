using System.Threading.Tasks;

namespace SleepHunter.Commands
{
  public interface IAsyncCommand : IAsyncCommand<object> { }

  public interface IAsyncCommand<in T> : IRaiseCanExecuteChanged
  {
    Task ExecuteAsync(T parameter);
    bool CanExecute(object paramter);
  }
}
