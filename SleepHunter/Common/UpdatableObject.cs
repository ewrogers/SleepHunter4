using System;

namespace SleepHunter.Common
{
    public abstract class UpdatableObject : ObservableObject, IDisposable
    {
        protected bool isDisposed;

        public event EventHandler Updated;

        public void Update()
        {
            CheckIfDisposed();

            OnUpdate();
            RaiseUpdated();
        }

        public bool TryUpdate()
        {
            CheckIfDisposed();

            try
            {
                Update();
                return true;
            }
            catch
            {
                return false;
            }
        }


        ~UpdatableObject() => Dispose(false);

        public void RaiseUpdated()
        {
            CheckIfDisposed();
            Updated?.Invoke(this, EventArgs.Empty);
        }

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

            }

            isDisposed = true;
        }

        protected abstract void OnUpdate();

        protected void CheckIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}
