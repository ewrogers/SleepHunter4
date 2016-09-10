using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SleepHunter.Commands
{
  public class AsyncDelegateCommand : AsyncDelegateCommand<object>, IAsyncCommand
  {
    public AsyncDelegateCommand(Func<Task> onExecute, Func<bool> onCanExecute = null)
        : base(o => onExecute?.Invoke(), o => onCanExecute != null ? onCanExecute() : true)
    {
      if (onExecute == null)
        throw new ArgumentNullException(nameof(onExecute));
    }
  }

  public class AsyncDelegateCommand<T> : IAsyncCommand<T>, ICommand
  {
    readonly Func<T, Task> onExecute;
    readonly DelegateCommand<T> underlyingCommand;
    bool isExecuting;

    public event EventHandler CanExecuteChanged
    {
      add { underlyingCommand.CanExecuteChanged += value; }
      remove { underlyingCommand.CanExecuteChanged -= value; }
    }

    public AsyncDelegateCommand(Func<T, Task> onExecute, Func<T, bool> onCanExecute = null)
    {
      if (onExecute == null)
        throw new ArgumentNullException(nameof(onExecute));

      this.onExecute = onExecute;
      underlyingCommand = new DelegateCommand<T>(x => { }, onCanExecute);
    }

    #region ICommand Methods
    public async void Execute(object parameter)
    {
      await ExecuteAsync((T)parameter);
    }

    public bool CanExecute(object parameter)
    {
      return !isExecuting && underlyingCommand.CanExecute((T)parameter);
    }
    #endregion

    #region IAsyncCommand<T> Methods
    public async Task ExecuteAsync(T parameter)
    {
      try
      {
        isExecuting = true;
        RaiseCanExecuteChanged();
        await onExecute(parameter);
      }
      finally
      {
        isExecuting = false;
        RaiseCanExecuteChanged();
      }
    }
    #endregion

    #region IRaiseCanExecuteChanged Methods
    public void RaiseCanExecuteChanged()
    {
      underlyingCommand.RaiseCanExecuteChanged();
    }
    #endregion
  }
}
