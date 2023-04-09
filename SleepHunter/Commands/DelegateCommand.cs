using System;
using System.Windows.Input;

namespace SleepHunter.Commands
{
    public class DelegateCommand : DelegateCommand<object>
    {
        public DelegateCommand(Action onExecute, Func<bool> onCanExecute = null)
            : base(o => onExecute?.Invoke(), o => onCanExecute != null ? onCanExecute() : true)
        {
            if (onExecute == null)
                throw new ArgumentNullException(nameof(onExecute));
        }
    }

    public class DelegateCommand<T> : ICommand, IRaiseCanExecuteChanged
    {
        readonly Action<T> onExecute;
        readonly Func<T, bool> onCanExecute;
        bool isExecuting;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public DelegateCommand(Action<T> onExecute, Func<T, bool> onCanExecute = null)
        {
            if (onExecute == null)
                throw new ArgumentNullException(nameof(onExecute));

            this.onExecute = onExecute;
            this.onCanExecute = onCanExecute;
        }

        #region ICommand Methods
        public void Execute(T parameter)
        {
            onExecute(parameter);
        }

        public bool CanExecute(T parameter)
        {
            return onCanExecute != null ? onCanExecute(parameter) : true;
        }

        void ICommand.Execute(object parameter)
        {
            isExecuting = true;

            try
            {
                RaiseCanExecuteChanged();
                Execute((T)parameter);
            }
            finally
            {
                isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return !isExecuting && CanExecute((T)parameter);
        }
        #endregion

        #region IRaiseCanExecuteChanged Methods
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
        #endregion
    }
}
