// -----------------------------------------------------------------------
// <copyright file="PerspexList.cs" company="Steven Kirk">
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

    /// <summary>
    /// Implements the <see cref="IReadOnlyPerspexList{T}"/> interface for single items.
    /// </summary>
    /// <typeparam name="T">The type of the single item.</typeparam>
    /// <remarks>
    /// Classes such as Border can only ever have a single logical child, but they need to 
    /// implement a list of logical children in their ILogical.LogicalChildren property using the
    /// <see cref="IReadOnlyPerspexList{T}"/> interface. This class facilitates that 
    /// without creating an actual <see cref="PerspexList{T}"/>.
    /// </remarks>
    public class SingleItemPerspexList<T> : IReadOnlyPerspexList<T> where T : class
    {
        private T item;

        public SingleItemPerspexList()
        {
        }

        public SingleItemPerspexList(T item)
        {
            this.item = item;
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= this.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return item;
            }
        }

        public int Count
        {
            get { return this.item != null ? 1 : 0; }
        }

        public T SingleItem
        {
            get
            {
                return this.item;
            }

            set
            {
                NotifyCollectionChangedEventArgs e = null;
                bool countChanged = false;

                if (value == null && this.item != null )
                {
                    e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, this.item, 0);
                    this.item = null;
                    countChanged = true;
                }
                else if (value != null && this.item == null)
                {
                    e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, this.item, 0);
                    this.item = value;
                    countChanged = true;
                }
                else
                {
                    e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, this.item);
                    this.item = value;
                }

                if (e != null && this.CollectionChanged != null)
                {
                    this.CollectionChanged(this, e);
                }

                if (countChanged && this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Count"));
                }
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public IEnumerator<T> GetEnumerator()
        {
            return Enumerable.Repeat(this.item, this.Count).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}