using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SleepHunter.Updater.ViewModels
{
    internal sealed class MainWindowViewModel : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private bool isBusy;
        private string statusText = "Preparing...";
        private int progressPercent = 50;
        private string currentFilename = "SleepHunter.exe";
        private string fileCountText = "1 of 6 files";
        private string errorTitle;
        private string errorMessage;
        private bool hasError;
        private bool canRetry;
        private bool canCancel;

        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;

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

        public void SetError(string title, string message)
        {
            ErrorTitle = title;
            ErrorMessage = message;
            HasError = true;
        }

        public void ClearError()
        {
            ErrorTitle = null;
            ErrorMessage= null;
            HasError = false;
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
