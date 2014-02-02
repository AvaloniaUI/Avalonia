namespace Perspex
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Text;
    using System.Threading.Tasks;

    public class PerspexList<T> : ObservableCollection<T>
    {
        public PerspexList()
        {
            this.Changed = Observable.FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => (sender, e) => handler(e),
                handler => this.CollectionChanged += handler,
                handler => this.CollectionChanged -= handler);
        }

        public IObservable<NotifyCollectionChangedEventArgs> Changed
        {
            get;
            private set;
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                this.Add(item);
            }
        }
    }
}