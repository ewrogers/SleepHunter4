using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using SleepHunter.Common;

namespace SleepHunter.ViewModels
{
    public abstract class ViewModelBase : ObservableObject, IDisposable
    {
        bool isDisposed;
        string displayName;
        bool throwOnInvalidPropertyName;

        public virtual string DisplayName
        {
            get { return displayName; }
            set { SetProperty(ref displayName, value); }
        }

        public bool ThrowOnInvalidPropertyName
        {
            get { return throwOnInvalidPropertyName; }
            set { SetProperty(ref throwOnInvalidPropertyName, value); }
        }

        protected ViewModelBase(string displayName = "")
        {
            this.displayName = displayName;
        }

        #region IDisposable Methods
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                // Dispose of managed resources
            }

            // Dispose of unmanaged resources

            isDisposed = true;
        }

        void CheckIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
        #endregion

        #region ObservableObject Methods
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            VerifyProperty(propertyName);
            base.OnPropertyChanged(propertyName);
        }

        protected override void OnPropertyChanging([CallerMemberName] string propertyName = "")
        {
            VerifyProperty(propertyName);
            base.OnPropertyChanging(propertyName);
        }
        #endregion

        [Conditional("DEBUG")]
        void VerifyProperty(string propertyName)
        {
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                var message = $"{GetType().Name} does not have a property named '{propertyName}'.";

                if (ThrowOnInvalidPropertyName)
                    throw new Exception(message);

                Debug.Fail(message);
            }
        }
    }
}
