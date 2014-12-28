// -----------------------------------------------------------------------
// <copyright file="PerspexReadOnlyListView.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;

    public class PerspexReadOnlyListView<TIn, TOut> : IReadOnlyPerspexList<TOut>, IDisposable
    {
        private IReadOnlyPerspexList<TIn> inner;

        private Func<TIn, TOut> convert;

        public PerspexReadOnlyListView(
            IReadOnlyPerspexList<TIn> inner,
            Func<TIn, TOut> convert)
        {
            this.inner = inner;
            this.convert = convert;
            this.inner.CollectionChanged += this.InnerCollectionChanged;
        }

        public TOut this[int index]
        {
            get { return this.convert(this.inner[index]); }
        }

        public int Count
        {
            get { return this.inner.Count; }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            this.inner.CollectionChanged -= this.InnerCollectionChanged;
        }

        public IEnumerator<TOut> GetEnumerator()
        {
            return this.inner.Select(convert).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private IList<TOut> ConvertList(IList list)
        {
            return list.Cast<TIn>().Select(this.convert).ToList();
        }

        private void InnerCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.CollectionChanged != null)
            {
                NotifyCollectionChangedEventArgs ev;

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        ev = new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Add,
                            this.ConvertList(e.NewItems),
                            e.NewStartingIndex);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        ev = new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Remove,
                            this.ConvertList(e.OldItems),
                            e.NewStartingIndex);
                        break;
                    default:
                        throw new NotSupportedException("Action not yet implemented.");
                }

                this.CollectionChanged(this, ev);
            }
        }
    }
}
