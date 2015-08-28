/*  
  Copyright 2007-2013 The NGenerics Team
 (https://github.com/ngenerics/ngenerics/wiki/Team)

 This program is licensed under the GNU Lesser General Public License (LGPL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://www.gnu.org/copyleft/lesser.html.
*/


using System;
using System.Collections;
using System.Collections.Generic;
using NGenerics.Comparers;
using System.Diagnostics.CodeAnalysis;
using NGenerics.Util;

namespace NGenerics.DataStructures.General
{
    /// <summary>
    /// An implementation of a Heap data structure.
    /// </summary>
	/// <typeparam name="T">The type of item stored in the <see cref="Heap{T}"/>.</typeparam>
    //[Serializable]
    [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class Heap<T> : ICollection<T>, IHeap<T>
    {
        #region Globals

        const string heapIsEmpty = "The heap is empty.";
        private readonly List<T> data;
        private readonly IComparer<T> comparerToUse;
        private readonly HeapType thisType;

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="Heap&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="type">The type of Heap to create.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> is not either <see cref="HeapType.Maximum"/> or <see cref="HeapType.Minimum"/> .</exception>
        /// <example>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\General\HeapExamples.cs" region="Constructor" lang="cs" title="The following example shows how to use the default constructor."/>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\General\HeapExamples.vb" region="Constructor" lang="vbnet" title="The following example shows how to use the default constructor."/>
        /// </example>
        public Heap(HeapType type) : this(type, Comparer<T>.Default) { }


        /// <summary>
        /// Initializes a new instance of the <see cref="Heap&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="type">The type of heap.</param>
        /// <param name="capacity">The capacity.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> is not either <see cref="HeapType.Maximum"/> or <see cref="HeapType.Minimum"/> .</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
        /// .
        /// <example>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\General\HeapExamples.cs" region="ConstructorCapacity" lang="cs" title="The following example shows how to use the capacity constructor."/>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\General\HeapExamples.vb" region="ConstructorCapacity" lang="vbnet" title="The following example shows how to use the capacity constructor."/>
        /// </example>
        public Heap(HeapType type, int capacity) : this(type, capacity, Comparer<T>.Default) { }


        /// <summary>
        /// Initializes a new instance of the <see cref="Heap&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="type">The type of heap.</param>
        /// <param name="comparer">The comparer.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> is not either <see cref="HeapType.Maximum"/> or <see cref="HeapType.Minimum"/> .</exception>
        public Heap(HeapType type, Comparison<T> comparer) : this(type, new ComparisonComparer<T>(comparer)){}

        /// <summary>
        /// Initializes a new instance of the <see cref="Heap&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="type">The type of heap.</param>
        /// <param name="capacity">The capacity of the heap to start with.</param>
        /// <param name="comparer">The comparer.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> is not either <see cref="HeapType.Maximum"/> or <see cref="HeapType.Minimum"/> .</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
        public Heap(HeapType type, int capacity, Comparison<T> comparer) : this(type, capacity, new ComparisonComparer<T>(comparer)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Heap&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="type">The type of Heap to create.</param>
        /// <param name="comparer">The comparer to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is a null reference (<c>Nothing</c> in Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> is not either <see cref="HeapType.Maximum"/> or <see cref="HeapType.Minimum"/> .</exception>
        /// <example>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\General\HeapExamples.cs" region="ConstructorComparer" lang="cs" title="The following example shows how to use the comparer constructor."/>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\General\HeapExamples.vb" region="ConstructorComparer" lang="vbnet" title="The following example shows how to use the comparer constructor."/>
        /// </example>
        public Heap(HeapType type, IComparer<T> comparer)
        {
            Guard.ArgumentNotNull(comparer, "comparer");

            if ((type != HeapType.Minimum) && (type != HeapType.Maximum))
            {
                throw new ArgumentOutOfRangeException("type");
            }

            thisType = type;

            data = new List<T> {default(T)};

            comparerToUse = type == HeapType.Minimum ? comparer : new ReverseComparer<T>(comparer);
        }


        /// <param name="type">The type of heap.</param>
        /// <param name="capacity">The initial capacity of the Heap.</param>
        /// <param name="comparer">The comparer to use.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>.
		/// <exception cref="ArgumentNullException"><paramref name="comparer"/> is a null reference (<c>Nothing</c> in Visual Basic).</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> is not either <see cref="HeapType.Maximum"/> or <see cref="HeapType.Minimum"/> .</exception>
        public Heap(HeapType type, int capacity, IComparer<T> comparer)
        {
            Guard.ArgumentNotNull(comparer, "comparer");

            if ((type != HeapType.Minimum) && (type != HeapType.Maximum))
            {
                throw new ArgumentOutOfRangeException("type");
            }
            thisType = type;

            data = new List<T>(capacity) {default(T)};

            comparerToUse = type == HeapType.Minimum ? comparer : new ReverseComparer<T>(comparer);
        }

        #endregion

        #region Public Members


		/// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\General\HeapExamples.cs" region="Root" lang="cs" title="The following example shows how to use the Root property."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\General\HeapExamples.vb" region="Root" lang="vbnet" title="The following example shows how to use the Root property."/>
        /// </example>
        public T Root
        {
            get
            {
                #region Validation

                if (Count == 0)
                {
                    throw new InvalidOperationException(heapIsEmpty);
                }

                #endregion

                return data[1];
            }
        }


		/// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\General\HeapExamples.cs" region="RemoveRoot" lang="cs" title="The following example shows how to use the RemoveRoot method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\General\HeapExamples.vb" region="RemoveRoot" lang="vbnet" title="The following example shows how to use the RemoveRoot method."/>
        /// </example>
        public T RemoveRoot()
        {
            #region Validation

            if (Count == 0)
            {
                throw new InvalidOperationException(heapIsEmpty);
            }

            #endregion

            // The minimum item to return.
            var minimum = data[1];

			RemoveRootItem(minimum);
            return minimum;
        }


        /// <summary>
        /// Removes the root item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <remarks>
        /// 	<b>Notes to Inheritors: </b>
        /// Derived classes can override this method to change the behavior of the <see cref="RemoveRoot"/> method.
        /// </remarks>
        protected virtual void RemoveRootItem(T item)
        {

            // The last item in the heap
            var last = data[Count];
            data.RemoveAt(Count);

            // If there's still items left in this heap, re-heapify it.
            if (Count > 0)
            {
                // Re-heapify the binary tree to conform to the heap property 
                var counter = 1;

                while ((counter * 2) < (data.Count))
                {
                    var child = counter * 2;

                    if (((child + 1) < (data.Count)) &&
                        (comparerToUse.Compare(data[child + 1], data[child]) < 0))
                    {
                        child++;
                    }

                    if (comparerToUse.Compare(last, data[child]) <= 0)
                    {
                        break;
                    }

                    data[counter] = data[child];
                    counter = child;
                }

                data[counter] = last;
            }

        }


        /// <summary>
        /// Gets the type of heap represented by this instance.
        /// </summary>
        /// <value>The type of heap.</value>
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\General\HeapExamples.cs" region="Type" lang="cs" title="The following example shows how to use the Type property."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\General\HeapExamples.vb" region="Type" lang="vbnet" title="The following example shows how to use the Type property."/>
        /// </example>
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public HeapType Type
        {
            get
            {
                return thisType;
            }
        }

        #endregion

        #region ICollection<T> Members

		

		/// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\General\HeapExamples.cs" region="IsEmpty" lang="cs" title="The following example shows how to use the IsEmpty property."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\General\HeapExamples.vb" region="IsEmpty" lang="vbnet" title="The following example shows how to use the IsEmpty property."/>
        /// </example>
        public bool IsEmpty
        {
            get
            {
                return Count == 0;
            }
        }

		

		/// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\General\HeapExamples.cs" region="Contains" lang="cs" title="The following example shows how to use the Contains method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\General\HeapExamples.vb" region="Contains" lang="vbnet" title="The following example shows how to use the Contains method."/>
        /// </example>
        public bool Contains(T item)
        {
            return data.Contains(item);
        }

		/// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\General\HeapExamples.cs" region="CopyTo" lang="cs" title="The following example shows how to use the CopyTo method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\General\HeapExamples.vb" region="CopyTo" lang="vbnet" title="The following example shows how to use the CopyTo method."/>
        /// </example>
        public void CopyTo(T[] array, int arrayIndex)
        {
            #region Validation

            Guard.ArgumentNotNull(array, "array");

            if ((array.Length - arrayIndex) < Count)
            {
                throw new ArgumentException(Constants.NotEnoughSpaceInTheTargetArray, "array");
            }

            #endregion

            for (var i = 1; i < data.Count; i++)
            {
                array[arrayIndex++] = data[i];
            }
        }

		/// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\General\HeapExamples.cs" region="Count" lang="cs" title="The following example shows how to use the Count property."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\General\HeapExamples.vb" region="Count" lang="vbnet" title="The following example shows how to use the Count property."/>
        /// </example>
        public int Count
        {
            get
            {
                return data.Count - 1;
            }
        }

		/// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\General\HeapExamples.cs" region="Add" lang="cs" title="The following example shows how to use the Add method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\General\HeapExamples.vb" region="Add" lang="vbnet" title="The following example shows how to use the Add method."/>
        /// </example>
        public void Add(T item)
        {
          AddItem(item);
        }

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <remarks>
        /// 	<b>Notes to Inheritors: </b>
        /// Derived classes can override this method to change the behavior of the <see cref="Add"/> method.
        /// </remarks>
		protected virtual void AddItem(T item)
		{
			// Add a dummy to the end of the list (it will be replaced)
			data.Add(default(T));

			var counter = data.Count - 1;

			while ((counter > 1) && (comparerToUse.Compare(data[counter / 2], item) > 0))
			{
				data[counter] = data[counter / 2];
				counter = counter / 2;
			}

			data[counter] = item;
		}

		

		/// <inheritdoc />
        /// <exception cref="NotSupportedException">Always.</exception>
        [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <example>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\General\HeapExamples.cs" region="GetEnumerator" lang="cs" title="The following example shows how to use the GetEnumerator method."/>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\General\HeapExamples.vb" region="GetEnumerator" lang="vbnet" title="The following example shows how to use the GetEnumerator method."/>
        /// </example>
        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 1; i < data.Count; i++)
            {
                yield return data[i];
            }
        }

		/// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\General\HeapExamples.cs" region="Clear" lang="cs" title="The following example shows how to use the Clear method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\General\HeapExamples.vb" region="Clear" lang="vbnet" title="The following example shows how to use the Clear method."/>
        /// </example>
        public void Clear()
        {
            ClearItems();            
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
			data.RemoveRange(1, data.Count - 1); // Clears all objects in this instance except the first dummy one.
		}

        #endregion

        #region ICollection<T> Members

		/// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\General\HeapExamples.cs" region="IsReadOnly" lang="cs" title="The following example shows how to use the IsReadOnly property."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\General\HeapExamples.vb" region="IsReadOnly" lang="vbnet" title="The following example shows how to use the IsReadOnly property."/>
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

        #endregion
    }
}
