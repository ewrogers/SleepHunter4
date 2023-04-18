using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SleepHunter.Updater.Commands;

namespace SleepHunter.Updater.ViewModels
{
    public sealed class MainWindowViewModel : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private bool isBusy;
        private string statusText = "Preparing...";
        private int progressPercent = 50;
        private string currentFilename = "SleepHunter.exe";
        private string fileCountText = "1 of 10 files";
        private string errorTitle;
        private string errorMessage;
        private bool hasError;
        private bool canRetry;
        private bool canCancel;

        private readonly ICommand retryCommand;
        private readonly ICommand cancelCommand;

        public event EventHandler RetryRequested;
        public event EventHandler CancelRequested;

        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;

        public MainWindowViewModel()
        {
            retryCommand = new DelegateCommand(() => RetryRequested?.Invoke(this, EventArgs.Empty), _ => CanRetry);
            cancelCommand = new DelegateCommand(() => CancelRequested?.Invoke(this, EventArgs.Empty), _ => CanCancel);
        }

        public string VersionString
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }

        public bool IsBusy
        {
            get => isBusy;
            set => SetProperty(ref isBusy, value);
        }

        public string StatusText
        {
            get => statusText;
            set => SetProperty(ref statusText, value);
        }

        public int ProgressPercent
        {
            get => progressPercent;
            set => SetProperty(ref progressPercent, value);
        }

        public string CurrentFilename
        {
            get => currentFilename;
            set => SetProperty(ref currentFilename, value);
        }

        public string FileCountText
        {
            get => fileCountText;
            set => SetProperty(ref fileCountText, value);
        }

        public string ErrorTitle
        {
            get => errorTitle;
            set => SetProperty(ref  errorTitle, value);
        }

        public string ErrorMessage
        {
            get => errorMessage;
            set => SetProperty(ref errorMessage, value);
        }

        public bool HasError
        {
            get => hasError;
            set => SetProperty(ref hasError, value);
        }

        public bool CanRetry
        {
            get => canRetry;
            set => SetProperty(ref canRetry, value);
        }

        public bool CanCancel
        {
            get => canCancel;
            set => SetProperty(ref canCancel, value);
        }

        public ICommand RetryCommand => retryCommand;
        public ICommand CancelCommand => cancelCommand;

        public void SetError(string title, string message, bool canRetry = true)
        {
            ErrorTitle = title;
            ErrorMessage = message;
            HasError = true;

            CanRetry= canRetry;
            CanCancel = true;

            // Forces commands to re-evaluate their CanExecute state
            CommandManager.InvalidateRequerySuggested();
        }

        public void ClearError()
        {
            ErrorTitle = null;
            ErrorMessage = null;
            HasError = false;

            CanRetry = false;
            CanCancel = false;

            // Forces commands to re-evaluate their CanExecute state
            CommandManager.InvalidateRequerySuggested();
        }

        private bool SetProperty<T>(ref T value, T newValue, Action<T> onChanged = null, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(value, newValue))
                return false;

            OnPropertyChanging(propertyName);
            value = newValue;
            OnPropertyChanged(propertyName);

            onChanged?.Invoke(newValue);
            return true;
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        private void OnPropertyChanging(string propertyName) => PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));

    }
}
