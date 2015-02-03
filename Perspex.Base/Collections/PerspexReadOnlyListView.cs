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

    public class PerspexReadOnlyListView<T> : IPerspexReadOnlyList<T>, IDisposable
    {
        private IPerspexReadOnlyList<T> source;

        public PerspexReadOnlyListView()
            : this(null)
        {
        }

        public PerspexReadOnlyListView(IPerspexReadOnlyList<T> source)
        {
            this.source = source;

            if (source != null)
            {
                this.source.CollectionChanged += this.SourceCollectionChanged;
            }
        }

        public T this[int index]
        {
            get { return this.source[index]; }
        }

        public int Count
        {
            get { return this.source.Count; }
        }

        public IPerspexReadOnlyList<T> Source
        {
            get
            {
                return this.source;
            }

            set
            {
                if (this.source != null)
                {
                    this.source.CollectionChanged -= this.SourceCollectionChanged;

                    if (this.CollectionChanged != null)
                    {
                        var ev = new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Remove,
                            this.source,
                            0);
                        this.CollectionChanged(this, ev);
                    }
                }

                this.source = value;

                if (this.source != null)
                {
                    this.source.CollectionChanged += this.SourceCollectionChanged;

                    if (this.CollectionChanged != null)
                    {
                        var ev = new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Add,
                            this.source,
                            0);
                        this.CollectionChanged(this, ev);
                    }
                }
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            this.source.CollectionChanged -= this.SourceCollectionChanged;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (this.source != null) ? 
                this.source.GetEnumerator() : 
                Enumerable.Empty<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.CollectionChanged != null)
            {
                NotifyCollectionChangedEventArgs ev;

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        ev = new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Add,
                            e.NewItems,
                            e.NewStartingIndex);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        ev = new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Remove,
                            e.OldItems,
                            e.NewStartingIndex);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        ev = new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Replace,
                            e.NewItems,
                            e.OldItems,
                            e.OldStartingIndex);
                        break;

                    default:
                        throw new NotSupportedException("Action not yet implemented.");
                }

                this.CollectionChanged(this, ev);
            }
        }
    }

    public class PerspexReadOnlyListView<TIn, TOut> : IPerspexReadOnlyList<TOut>, IDisposable
    {
        private IPerspexReadOnlyList<TIn> source;

        private Func<TIn, TOut> convert;

        public PerspexReadOnlyListView(Func<TIn, TOut> convert)
            : this(null, convert)
        {
        }

        public PerspexReadOnlyListView(IPerspexReadOnlyList<TIn> source, Func<TIn, TOut> convert)
        {
            this.source = source;
            this.convert = convert;

            if (source != null)
            {
                this.source.CollectionChanged += this.SourceCollectionChanged;
            }
        }

        public TOut this[int index]
        {
            get
            {
                return (this.convert != null) ?
                    this.convert(this.source[index]) :
                    (TOut)(object)this.source[index];
            }
        }

        public int Count
        {
            get { return this.source.Count; }
        }

        public IPerspexReadOnlyList<TIn> Source
        {
            get
            {
                return this.source;
            }

            set
            {
                if (this.source != null)
                {
                    this.source.CollectionChanged -= this.SourceCollectionChanged;

                    if (this.CollectionChanged != null)
                    {
                        var ev = new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Remove,
                            this.ConvertList(this.source),
                            0);
                        this.CollectionChanged(this, ev);
                    }
                }

                this.source = value;

                if (this.source != null)
                {
                    this.source.CollectionChanged += this.SourceCollectionChanged;

                    if (this.CollectionChanged != null)
                    {
                        var ev = new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Add,
                            this.ConvertList(this.source),
                            0);
                        this.CollectionChanged(this, ev);
                    }
                }
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public void Dispose()
        {
            if (this.source != null)
            {
                this.source.CollectionChanged -= this.SourceCollectionChanged;
            }
        }

        public IEnumerator<TOut> GetEnumerator()
        {
            if (this.source != null)
            {
                return this.source.Select(this.convert).GetEnumerator();
            }
            else
            {
                return Enumerable.Empty<TOut>().GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private IList<TOut> ConvertList(IEnumerable list)
        {
            return list.Cast<TIn>().Select(this.convert).ToList();
        }

        private void SourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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
                    case NotifyCollectionChangedAction.Replace:
                        ev = new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Replace,
                            this.ConvertList(e.NewItems),
                            this.ConvertList(e.OldItems),
                            e.OldStartingIndex);
                        break;

                    default:
                        throw new NotSupportedException("Action not yet implemented.");
                }

                this.CollectionChanged(this, ev);
            }
        }
    }

}
