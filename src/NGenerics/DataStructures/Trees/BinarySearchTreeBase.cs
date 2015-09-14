// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using NGenerics.Comparers;
using NGenerics.Patterns.Visitor;
using NGenerics.Util;

namespace NGenerics.DataStructures.Trees
{
    /// <summary>
    /// A base class for Binary Search Trees that store a single value in each node.
    /// </summary>
	/// <typeparam name="T"></typeparam>
    //[Serializable]
    public abstract class BinarySearchTreeBase<T> : ISearchTree<T>
    {
        #region Globals

        internal const string alreadyContainedInTheTree = "The item is already contained in the tree.";
        private BinaryTree<T> _tree;
        private readonly IComparer<T> _comparer;

        #endregion

        #region Delegates

        /// <summary>
        /// A custom comparison between some search value and the type of item that is kept in the tree.
        /// </summary>
        /// <typeparam name="TSearch">The type of the search.</typeparam>
        protected delegate int CustomComparison<TSearch>(TSearch value, T item);

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="BinarySearchTreeBase{T}"/> class.
        /// </summary>
        protected BinarySearchTreeBase()
        {
            _comparer = Comparer<T>.Default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinarySearchTreeBase{T}"/> class.
        /// </summary>
        /// <param name="comparer">The comparer to use when comparing items.</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is a null reference (<c>Nothing</c> in Visual Basic).</exception>
        protected BinarySearchTreeBase(IComparer<T> comparer)
        {
            Guard.ArgumentNotNull(comparer, "comparer");
            _comparer = comparer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinarySearchTreeBase&lt;TKey, TValue&gt;"/> class.
        /// </summary>
        /// <param name="comparison">The comparison.</param>
        protected BinarySearchTreeBase(Comparison<T> comparison)
        {
            Guard.ArgumentNotNull(comparison, "comparison");
            _comparer = new ComparisonComparer<T>(comparison);
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets the comparer.
        /// </summary>
        /// <value>The comparer.</value>
        public IComparer<T> Comparer => _comparer;

        #endregion

        #region Protected Members

        /// <summary>
        /// Finds the node containing the specified data key.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        /// The node with the specified key if found.  If the key is not in the tree, this method returns null.
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        protected virtual BinaryTree<T> FindNode(T item)
        {
            if (_tree == null)
            {
                return null;
            }

            var currentNode = _tree;

            while (currentNode != null)
            {
                var nodeResult = _comparer.Compare(item, currentNode.Data);

                if (nodeResult == 0)
                {
                    return currentNode;
                }

                currentNode = nodeResult < 0 ? currentNode.Left : currentNode.Right;
            }

            return null;
        }

        /// <summary>
        /// Finds the node that matches the custom delegate.
        /// </summary>
        /// <typeparam name="TSearch">The type of the search.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="customComparison">The custom comparison.</param>
        /// <returns>The item if  found, else null.</returns>
        protected virtual BinaryTree<T> FindNode<TSearch>(TSearch value, CustomComparison<TSearch> customComparison)
        {
            if (_tree == null)
            {
                return null;
            }

            var currentNode = _tree;

            while (currentNode != null)
            {
                var nodeResult = customComparison(value, currentNode.Data);

                if (nodeResult == 0)
                {
                    return currentNode;
                }

                currentNode = nodeResult < 0 ? currentNode.Left : currentNode.Right;
            }

            return null;
        }

        /// <summary>
        /// Removes the item from the tree.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>An indication of whether the item has been removed from the tree.</returns>
        protected abstract bool RemoveItem(T item);

        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        protected abstract void AddItem(T item);


        /// <summary>
        /// Find the maximum node.
        /// </summary>
        /// <returns>The maximum node.</returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        protected BinaryTree<T> FindMaximumNode()
        {
            #region Debug

            Debug.Assert(_tree != null);

            #endregion

            return FindMaximumNode(_tree);
        }


        /// <summary>
        /// Find the minimum node.
        /// </summary>
        /// <returns>The minimum node.</returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        protected BinaryTree<T> FindMinimumNode()
        {
            #region Debug

            Debug.Assert(_tree != null);

            #endregion

            return FindMinimumNode(_tree);
        }

        /// <summary>
        /// Finds the maximum node.
        /// </summary>
        /// <param name="startNode">The start node.</param>
        /// <returns>The maximum node below this node.</returns>
        [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        protected static BinaryTree<T> FindMaximumNode(BinaryTree<T> startNode)
        {
            #region Asserts

            Debug.Assert(startNode != null);

            #endregion

            var searchNode = startNode;

            while (searchNode.Right != null)
            {
                searchNode = searchNode.Right;
            }

            return searchNode;
        }


        /// <summary>
        /// Finds the minimum node.
        /// </summary>
        /// <param name="startNode">The start node.</param>
        /// <returns>The minimum node below this node.</returns>
        [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        protected static BinaryTree<T> FindMinimumNode(BinaryTree<T> startNode)
        {
            #region Asserts

            Debug.Assert(startNode != null);

            #endregion

            var searchNode = startNode;

            while (searchNode.Left != null)
            {
                searchNode = searchNode.Left;
            }

            return searchNode;
        }

        /// <summary>
        /// Gets or sets the <see cref="BinaryTree{T}"/> for this <see cref="BinarySearchTreeBase{TKey,TValue}"/>.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        protected BinaryTree<T> Tree
        {
            get
            {
                return _tree;
            }
            set
            {
                _tree = value;
            }
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Visits the node in an in-order fashion.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="visitor">The visitor.</param>
        private static void VisitNode(BinaryTree<T> node, OrderedVisitor<T> visitor)
        {
            if (node != null)
            {
                var pair = node.Data;

                visitor.VisitPreOrder(pair);

                VisitNode(node.Left, visitor);

                visitor.VisitInOrder(pair);

                VisitNode(node.Right, visitor);

                visitor.VisitPostOrder(pair);
            }
        }

        #endregion

        #region ISearchTree<TKey,TValue> Members

        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="Minimum" lang="cs" title="The following example shows how to use the Minimum property."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="Minimum" lang="vbnet" title="The following example shows how to use the Minimum property."/>
        /// </example>
        public virtual T Minimum
        {
            get
            {
                #region Validation

                ValidateEmpty();

                #endregion

                return FindMinimumNode().Data;
            }
        }

        private void ValidateEmpty()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("The search tree is empty.");
            }
        }

        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="Maximum" lang="cs" title="The following example shows how to use the Maximum property."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="Maximum" lang="vbnet" title="The following example shows how to use the Maximum property."/>
        /// </example>
        public virtual T Maximum
        {
            get
            {
                ValidateEmpty();

                return FindMaximumNode().Data;
            }
        }        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public void DepthFirstTraversal(OrderedVisitor<T> visitor)
        {
            Guard.ArgumentNotNull(visitor, "visitor");

            VisitNode(_tree, visitor);
        }

        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IEnumerator<T> GetOrderedEnumerator()
        {
            if (_tree != null)
            {
                var trackingVisitor = new TrackingVisitor<T>();
                var inOrderVisitor = new InOrderVisitor<T>(trackingVisitor);

                _tree.DepthFirstTraversal(inOrderVisitor);

                var trackingList = trackingVisitor.TrackingList;

                for (var i = 0; i < trackingList.Count; i++)
                {
                    yield return trackingList[i];
                }
            }
        }

        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="IsEmpty" lang="cs" title="The following example shows how to use the IsEmpty property."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="IsEmpty" lang="vbnet" title="The following example shows how to use the IsEmpty property."/>
        /// </example>
        public bool IsEmpty => Count == 0;

        /// <inheritdoc />
        public bool Remove(T item)
        {
            var itemRemoved = RemoveItem(item);

            if (itemRemoved)
            {
                Count--;
            }

            return itemRemoved;
        }

        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="Clear" lang="cs" title="The following example shows how to use the Clear method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="Clear" lang="vbnet" title="The following example shows how to use the Clear method."/>
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
            _tree = null;
            Count = 0;
        }

        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="Count" lang="cs" title="The following example shows how to use the Count property."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="Count" lang="vbnet" title="The following example shows how to use the Count property."/>
        /// </example>
        public int Count { get; private set; }

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
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="GetEnumerator" lang="cs" title="The following example shows how to use the GetEnumerator method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="GetEnumerator" lang="vbnet" title="The following example shows how to use the GetEnumerator method."/>
        /// </example>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public IEnumerator<T> GetEnumerator()
        {
            if (_tree != null)
            {
                var stack = new Stack<BinaryTree<T>>();

                stack.Push(_tree);

                while (stack.Count > 0)
                {
                    var binaryTree = stack.Pop();

                    yield return binaryTree.Data;

                    if (binaryTree.Left != null)
                    {
                        stack.Push(binaryTree.Left);
                    }

                    if (binaryTree.Right != null)
                    {
                        stack.Push(binaryTree.Right);
                    }
                }
            }
        }

        #endregion


        #region ICollection<T> Members

        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="AddKeyValuePair" lang="cs" title="The following example shows how to use the AddKeyValuePair method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="AddKeyValuePair" lang="vbnet" title="The following example shows how to use the AddKeyValuePair method."/>
        /// </example>
        public void Add(T item)
        {
            AddItem(item);
            Count++;
        }

        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="Contains" lang="cs" title="The following example shows how to use the Contains method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="Contains" lang="vbnet" title="The following example shows how to use the Contains method."/>
        /// </example>
        public virtual bool Contains(T item)
        {
            var node = FindNode(item);
            return node != null;
        }


        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="CopyTo" lang="cs" title="The following example shows how to use the CopyTo method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="CopyTo" lang="vbnet" title="The following example shows how to use the CopyTo method."/>
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

            foreach (var association in _tree)
            {
                array[arrayIndex++] = association;
            }
        }
        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="IsReadOnly" lang="cs" title="The following example shows how to use the IsReadOnly property."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="IsReadOnly" lang="vbnet" title="The following example shows how to use the IsReadOnly property."/>
        /// </example>
        public bool IsReadOnly => false;

        #endregion
    }
}