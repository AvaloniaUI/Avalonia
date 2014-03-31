// -----------------------------------------------------------------------
// <copyright file="LogicalChildren.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;

    /// <summary>
    /// Manages parenting for a collection of logical child controls.
    /// </summary>
    /// <typeparam name="T">The type of the controls.</typeparam>
    /// <remarks>
    /// Unfortunately, because of ObservableCollection's handling of clear (the cleared items
    /// aren't passed to the CollectionChanged event) we have to hold two lists of child controls:
    /// the ones in Panel.Children and the ones here - held in case the ObservableCollection
    /// gets cleared. It's either that or write a proper PerspexList which is too much work
    /// for now.
    /// </remarks>
    internal class LogicalChildren<T> where T : class, ILogical, IVisual
    {
        private T parent;

        private PerspexList<T> childrenCollection;

        private List<T> inner = new List<T>();

        public LogicalChildren(T parent, PerspexList<T> childrenCollection)
        {
            this.parent = parent;
            this.childrenCollection = childrenCollection;
            this.Add(childrenCollection);
            childrenCollection.CollectionChanged += this.CollectionChanged;
        }

        public void Change(PerspexList<T> childrenCollection)
        {
            this.childrenCollection.CollectionChanged -= this.CollectionChanged;
            this.Remove(inner.ToList());
            this.childrenCollection = childrenCollection;
            this.Add(childrenCollection);
            childrenCollection.CollectionChanged += this.CollectionChanged;
        }

        private void Add(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                this.inner.Add(item);
                item.LogicalParent = this.parent;
                item.VisualParent = this.parent;
            }
        }

        private void Remove(IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                this.inner.Remove(item);
                item.LogicalParent = null;
                item.VisualParent = null;
            }
        }

        private void Reset(IEnumerable<T> newState)
        {
            this.Add(newState.Except(this.inner));
            this.Remove(this.inner.Except(newState));
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.Add(e.NewItems.Cast<T>());
                    break;

                case NotifyCollectionChangedAction.Remove:
                    this.Remove(e.OldItems.Cast<T>());
                    break;

                case NotifyCollectionChangedAction.Replace:
                    this.Remove(e.OldItems.Cast<T>());
                    this.Add(e.NewItems.Cast<T>());
                    break;

                case NotifyCollectionChangedAction.Reset:
                    this.Reset((IEnumerable<T>)sender);
                    break;
            }
        }
    }
}
