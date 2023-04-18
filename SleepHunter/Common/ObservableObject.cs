using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SleepHunter.Common
{
    internal abstract class ObservableObject : INotifyPropertyChanged, INotifyPropertyChanging
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;

        protected ObservableObject() { }

        public void RaisePropertyChanged(string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            OnPropertyChanged(propertyName);
        }

        public void RaisePropertyChanging(string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            OnPropertyChanging(propertyName);
        }

        protected virtual bool SetProperty<T>(ref T backingStore,
            T newValue,
            [CallerMemberName] string propertyName = "",
            Action<T> onChanged = null,
            Action<T> onChanging = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, newValue))
                return false;

            onChanging?.Invoke(newValue);
            OnPropertyChanging(propertyName);

            backingStore = newValue;

            onChanged?.Invoke(newValue);
            OnPropertyChanged(propertyName);
            return true;
        }

        #region INotifyPropertyChanged Methods
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region INotifyPropertyChanging Methods
        protected virtual void OnPropertyChanging([CallerMemberName] string propertyName = "")
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }
        #endregion
    }
}
