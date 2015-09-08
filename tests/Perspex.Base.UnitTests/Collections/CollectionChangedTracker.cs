





namespace Perspex.Base.UnitTests.Collections
{
    using System;
    using System.Collections.Specialized;

    internal class CollectionChangedTracker
    {
        public CollectionChangedTracker(INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += this.CollectionChanged;
        }

        public NotifyCollectionChangedEventArgs Args { get; private set; }

        public void Reset()
        {
            this.Args = null;
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.Args != null)
            {
                throw new Exception("CollectionChanged called more than once.");
            }

            this.Args = e;
        }
    }
}
