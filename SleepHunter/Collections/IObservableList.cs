using System.Collections.Generic;
using System.Collections.Specialized;

namespace SleepHunter.Collections
{
    internal interface IObservableList<T> : IReadOnlyList<T>, INotifyCollectionChanged { }
}
