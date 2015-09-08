// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

/*  
  Copyright 2007-2013 The NGenerics Team
 (https://github.com/ngenerics/ngenerics/wiki/Team)

 This program is licensed under the GNU Lesser General Public License (LGPL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://www.gnu.org/copyleft/lesser.html.
  
 Community contributions :
    -  TKey Peek(out TPriority) contributed by Karl Shulze (http://www.karlschulze.com/).</remarks>
*/


using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NGenerics.Comparers;
using NGenerics.DataStructures.Trees;
using NGenerics.Util;

namespace NGenerics.DataStructures.Queues
{
    /// <summary>
    /// An implementation of a Priority Queue (can be <see cref="PriorityQueueType.Minimum"/> or <see cref="PriorityQueueType.Maximum"/>).
    /// </summary>
    /// <typeparam name="TPriority">The type of the priority in the <see cref="PriorityQueue{TPriority, TValue}"/>.</typeparam>
    /// <typeparam name="TValue">The type of the elements in the <see cref="PriorityQueue{TPriority, TValue}"/>.</typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    //[Serializable]
    public class PriorityQueue<TValue, TPriority> : ICollection<TValue>, IQueue<TValue>
    {
        #region Globals

        private readonly RedBlackTreeList<TPriority, TValue> _tree;
        private TPriority _defaultPriority;
        private readonly PriorityQueueType _queueType;

        #endregion

        #region Construction

        /// <param name="queueType">Type of the queue.</param>
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="Constructor" lang="cs" title="The following example shows how to use the default constructor."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="Constructor" lang="vbnet" title="The following example shows how to use the default constructor."/>
        /// </example>
        public PriorityQueue(PriorityQueueType queueType) :
            this(queueType, Comparer<TPriority>.Default)
        {
        }

        /// <inheritdoc/>
        public PriorityQueue(PriorityQueueType queueType, IComparer<TPriority> comparer)
        {
            if ((queueType != PriorityQueueType.Minimum) && (queueType != PriorityQueueType.Maximum))
            {
                throw new ArgumentOutOfRangeException("queueType");
            }
            _queueType = queueType;
            _tree = new RedBlackTreeList<TPriority, TValue>(comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityQueue&lt;TValue, TPriority&gt;"/> class.
        /// </summary>
        /// <param name="queueType">Type of the queue.</param>
        /// <param name="comparison">The comparison.</param>
        /// <inheritdoc/>
        public PriorityQueue(PriorityQueueType queueType, Comparison<TPriority> comparison) :
            this(queueType, new ComparisonComparer<TPriority>(comparison))
        {
        }

        #endregion

        #region IQueue<T> Members

        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="Enqueue" lang="cs" title="The following example shows how to use the Enqueue method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="Enqueue" lang="vbnet" title="The following example shows how to use the Enqueue method."/>
        /// </example>
        public void Enqueue(TValue item)
        {
            Add(item);
        }

        /// <summary>
        /// Enqueues the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="priority">The priority.</param>
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="EnqueuePriority" lang="cs" title="The following example shows how to use the Enqueue method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="EnqueuePriority" lang="vbnet" title="The following example shows how to use the Enqueue method."/>
        /// </example>
        public void Enqueue(TValue item, TPriority priority)
        {
            Add(item, priority);
        }

        /// <summary>
        /// Dequeues the item at the front of the queue.
        /// </summary>
        /// <returns>The item at the front of the queue.</returns>
        /// <example>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="Dequeue" lang="cs" title="The following example shows how to use the Dequeue method."/>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="Dequeue" lang="vbnet" title="The following example shows how to use the Dequeue method."/>
        /// </example>
        public TValue Dequeue()
        {
            TPriority priority;
            return Dequeue(out priority);
        }


        /// <summary>
        /// Peeks at the item in the front of the queue, without removing it.
        /// </summary>
        /// <returns>The item at the front of the queue.</returns>
        /// <example>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="Peek" lang="cs" title="The following example shows how to use the Peek method."/>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="Peek" lang="vbnet" title="The following example shows how to use the Peek method."/>
        /// </example>
        public TValue Peek()
        {
            var association = GetNextItem();

            // Always dequeue in FIFO manner
            return association.Value.First.Value;
        }


        /// <summary>
        /// Peeks at the item in the front of the queue, without removing it.
        /// </summary>
        /// <param name="priority">The priority of the item.</param>
        /// <returns>The item at the front of the queue.</returns>
        /// <example>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="PeekPriority" lang="cs" title="The following example shows how to use the Peek method."/>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="PeekPriority" lang="vbnet" title="The following example shows how to use the Peek method."/>
        /// </example>
        public TValue Peek(out TPriority priority)
        {
            var association = GetNextItem();
            var item = association.Value.First.Value;
            priority = association.Key;
            return item;
        }

        #endregion

        #region ICollection<T> Members

        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="IsReadOnly" lang="cs" title="The following example shows how to use the IsReadOnly property."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="IsReadOnly" lang="vbnet" title="The following example shows how to use the IsReadOnly property."/>
        /// </example>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region IEnumerable Members

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <example>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="GetEnumerator" lang="cs" title="The following example shows how to use the GetEnumerator method."/>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="GetEnumerator" lang="vbnet" title="The following example shows how to use the GetEnumerator method."/>
        /// </example>
        public IEnumerator<TValue> GetEnumerator()
        {
            return _tree.GetValueEnumerator();
        }

        #endregion

        #region Public Members

        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="Count" lang="cs" title="The following example shows how to use the Count property."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="Count" lang="vbnet" title="The following example shows how to use the Count property."/>
        /// </example>
        public int Count { get; private set; }

        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="Contains" lang="cs" title="The following example shows how to use the Contains method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="Contains" lang="vbnet" title="The following example shows how to use the Contains method."/>
        /// </example>
        public bool Contains(TValue item)
        {
            return _tree.ContainsValue(item);
        }

        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="CopyTo" lang="cs" title="The following example shows how to use the CopyTo method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="CopyTo" lang="vbnet" title="The following example shows how to use the CopyTo method."/>
        /// </example>
        public void CopyTo(TValue[] array, int arrayIndex)
        {
            #region Validation

            Guard.ArgumentNotNull(array, "array");

            if ((array.Length - arrayIndex) < Count)
            {
                throw new ArgumentException(Constants.NotEnoughSpaceInTheTargetArray, "array");
            }

            #endregion


            foreach (var association in _tree)
            {
                var items = association.Value;

                foreach (var item in items)
                {
                    array.SetValue(item, arrayIndex++);
                }
            }
        }

        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="Add" lang="cs" title="The following example shows how to use the Add method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="Add" lang="vbnet" title="The following example shows how to use the Add method."/>
        /// </example>
        public void Add(TValue item)
        {
            Add(item, _defaultPriority);
        }

        /// <summary>
        /// Adds an item to the <see cref="ICollection{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="ICollection{T}"/>.</param>
        /// <param name="priority">The priority of the item.</param>
        /// <exception cref="NotSupportedException">The <see cref="ICollection{T}"/> is read-only.</exception>
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="AddPriority" lang="cs" title="The following example shows how to use the Add method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="AddPriority" lang="vbnet" title="The following example shows how to use the Add method."/>
        /// </example>
        public void Add(TValue item, TPriority priority)
        {
            AddItem(item, priority);
        }

        /// <inheritdoc />
        public bool Remove(TValue item)
        {
            TPriority priority;
            return Remove(item, out priority);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the keys in the collection.
        /// </summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the keys in the collection.</returns>
        /// <example>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="GetKeyEnumerator" lang="cs" title="The following example shows how to use the GetKeyEnumerator method."/>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="GetKeyEnumerator" lang="vbnet" title="The following example shows how to use the GetKeyEnumerator method."/>
        /// </example>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IEnumerator<KeyValuePair<TPriority, TValue>> GetKeyEnumerator()
        {
            return _tree.GetKeyEnumerator();
        }

        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="Clear" lang="cs" title="The following example shows how to use the Clear method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="Clear" lang="vbnet" title="The following example shows how to use the Clear method."/>
        /// </example>
        public void Clear()
        {
            ClearItems();
        }

        /// <summary>
        /// Dequeues the item from the head of the queue.
        /// </summary>
        /// <param name="priority">The priority of the item to dequeue.</param>
        /// <returns>The item at the head of the queue.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="PriorityQueue{TValue, TPriority}"/> is empty.</exception>
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Queues\PriorityQueueExamples.cs" region="DequeueWithPriority" lang="cs" title="The following example shows how to use the Dequeue method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Queues\PriorityQueueExamples.vb" region="DequeueWithPriority" lang="vbnet" title="The following example shows how to use the Dequeue method."/>
        /// </example>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#")]
        public TValue Dequeue(out TPriority priority)
        {
            return DequeueItem(out priority);
        }

        /// <summary>
        /// Dequeues the item at the front of the queue.
        /// </summary>
        /// <returns>The item at the front of the queue.</returns>
        /// <remarks>
        /// <b>Notes to Inheritors: </b>
        ///  Derived classes can override this method to change the behavior of the <see cref="Dequeue()"/> or <see cref="Dequeue(out TPriority)"/> methods.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#")]
        protected virtual TValue DequeueItem(out TPriority priority)
        {
            var association = GetNextItem();

            var item = association.Value.First.Value;
            association.Value.RemoveFirst();

            var key = association.Key;

            if (association.Value.Count == 0)
            {
                _tree.Remove(association.Key);
            }

            Count--;

            priority = key;
            return item;
        }

        /// <summary>
        /// Gets or sets the default priority.
        /// </summary>
        /// <value>The default priority.</value>
        public TPriority DefaultPriority
        {
            get
            {
                return _defaultPriority;
            }
            set
            {
                _defaultPriority = value;
            }
        }

        /// <summary>
        /// Removes the first occurrence of the specified item from the property queue.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <param name="priority">The priority associated with the item.</param>
        /// <returns><c>true</c> if the item exists in the <see cref="PriorityQueue{TValue, TPriority}"/> and has been removed; otherwise <c>false</c>.</returns>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        public bool Remove(TValue item, out TPriority priority)
        {
            return RemoveItem(item, out priority);
        }
        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <param name="priority">The priority of the item that was removed.</param>
        /// <returns>An indication of whether the item was found, and removed.</returns>
        /// <remarks>
        /// 	<b>Notes to Inheritors: </b>
        /// Derived classes can override this method to change the behavior of the <see cref="Remove(TValue,out TPriority)"/> method.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        protected virtual bool RemoveItem(TValue item, out TPriority priority)
        {
            var removed = _tree.Remove(item, out priority);

            if (removed)
            {
                Count--;
            }

            return removed;
        }

        /// <summary>
        /// Removes the items with the specified priority.
        /// </summary>
        /// <param name="priority">The priority.</param>
        /// <returns><c>true</c> if the priority exists in the <see cref="PriorityQueue{TValue, TPriority}"/> and has been removed; otherwise <c>false</c>.</returns>
        public bool RemovePriorityGroup(TPriority priority)
        {
            return RemoveItems(priority);
        }

        /// <summary>
        /// Removes the items from the collection with the specified priority.
        /// </summary>
        /// <param name="priority">The priority to search for.</param>
        /// <returns>An indication of whether items were found having the specified priority.</returns>
		protected virtual bool RemoveItems(TPriority priority)
        {
            LinkedList<TValue> items;

            if (_tree.TryGetValue(priority, out items))
            {
                _tree.Remove(priority);
                Count -= items.Count;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the items with the specified priority.
        /// </summary>
        /// <param name="priority">The priority.</param>
        /// <returns>The items with the specified priority.</returns>
        public IList<TValue> GetPriorityGroup(TPriority priority)
        {
            LinkedList<TValue> items;

            return _tree.TryGetValue(priority, out items) ? new List<TValue>(items) : new List<TValue>();
        }


        /// <summary>
        /// Adds the specified items to the priority queue with the specified priority.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="priority">The priority.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is a null reference (<c>Nothing</c> in Visual Basic).</exception>
        public void AddPriorityGroup(IList<TValue> items, TPriority priority)
        {
            #region Validation

            Guard.ArgumentNotNull(items, "items");

            #endregion

            AddPriorityGroupItem(items, priority);
        }

        /// <summary>
        /// Adds the specified items to the priority queue with the specified priority.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="priority">The priority.</param>
        /// <remarks>
        /// 	<b>Notes to Inheritors: </b>
        /// Derived classes can override this method to change the behavior of the <see cref="AddPriorityGroup"/> method.
        /// </remarks>
        protected virtual void AddPriorityGroupItem(IList<TValue> items, TPriority priority)
        {
            LinkedList<TValue> currentValues;

            if (_tree.TryGetValue(priority, out currentValues))
            {
                for (var i = 0; i < items.Count; i++)
                {
                    currentValues.AddLast(items[i]);
                }
            }
            else
            {
                currentValues = new LinkedList<TValue>(items);
                _tree.Add(priority, currentValues);
            }
        }

        #endregion

        #region Protected Members

        /// <summary>
        /// Adds the item to the queue.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="priority">The priority of the item.</param>
        /// <remarks>
        /// 	<b>Notes to Inheritors: </b>
        /// Derived classes can override this method to change the behavior of the <see cref="Add(TValue,TPriority)"/> method.
        /// </remarks>
        protected virtual void AddItem(TValue item, TPriority priority)
        {
            LinkedList<TValue> list;

            if (_tree.TryGetValue(priority, out list))
            {
                list.AddLast(item);
            }
            else
            {
                list = new LinkedList<TValue>();
                list.AddLast(item);
                _tree.Add(priority, list);
            }

            Count++;
        }

        /// <summary>
        /// Clears all the objects in this instance.
        /// </summary>
        /// <remarks>
        /// <b>Notes to Inheritors: </b>
        ///  Derived classes can override this method to change the behavior of the <see cref="Clear"/> method.
        /// </remarks>
        protected virtual void ClearItems()
        {
            _tree.Clear();
            Count = 0;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Checks if the list is not empty, and if it is, throw an exception.
        /// </summary>
        private void CheckTreeNotEmpty()
        {
            if (_tree.Count == 0)
            {
                throw new InvalidOperationException("The Priority Queue is empty.");
            }
        }

        /// <summary>
        /// Gets the next item.
        /// </summary>
        private KeyValuePair<TPriority, LinkedList<TValue>> GetNextItem()
        {
            #region Validation

            CheckTreeNotEmpty();

            #endregion

            return _queueType == PriorityQueueType.Maximum ? _tree.Maximum : _tree.Minimum;
        }

        #endregion
    }
}
