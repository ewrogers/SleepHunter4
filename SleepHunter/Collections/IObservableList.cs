using System.Collections.Generic;
using System.Collections.Specialized;

namespace SleepHunter.Collections
{
    public interface IObservableList<T> : IReadOnlyList<T>, INotifyCollectionChanged { }
}
