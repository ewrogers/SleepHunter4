using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Data
{
   public abstract class NotifyObject : INotifyPropertyChanging, INotifyPropertyChanged
   {
      public virtual event PropertyChangingEventHandler PropertyChanging;
      public virtual event PropertyChangedEventHandler PropertyChanged;      

      protected virtual void SetProperty<T>(ref T backingStore, T newValue, string propertyName, Action<string> onChanged = null, Action<string, T> onChanging = null)
      {
         if (propertyName == null)
            throw new ArgumentNullException("propertyName");

         if (EqualityComparer<T>.Default.Equals(backingStore, newValue))
            return;

         if (onChanging != null)
            onChanging(propertyName, newValue);

         OnPropertyChanging(propertyName);

         backingStore = newValue;

         if (onChanged != null)
            onChanged(propertyName);

         OnPropertyChanged(propertyName);
      }

      protected virtual void OnPropertyChanging(string propertyName)
      {
         if (propertyName == null)
            throw new ArgumentNullException("propertyName");

         var handler = this.PropertyChanging;

         if (handler != null)
            handler(this, new PropertyChangingEventArgs(propertyName));
      }

      protected virtual void OnPropertyChanged(string propertyName)
      {
         if (propertyName == null)
            throw new ArgumentNullException("propertyName");

         var handler = this.PropertyChanged;

         if (handler != null)
            handler(this, new PropertyChangedEventArgs(propertyName));
      }
   }
}
