using System;
using System.Windows.Input;

namespace SleepHunter.Updater.Commands
{
    internal class DelegateCommand : ICommand
    {
        private readonly Action onExecute;
        private readonly Predicate<object> canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public DelegateCommand(Action onExecute, Predicate<object> canExecute = null)
        {
            this.onExecute = onExecute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object param) => canExecute?.Invoke(param) ?? true;
        public void Execute(object param) => onExecute?.Invoke();
    }
}
