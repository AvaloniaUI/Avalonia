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
using NGenerics.Patterns.Visitor;
using NGenerics.Util;
using System.Diagnostics.CodeAnalysis;

namespace NGenerics.DataStructures.Trees
{
    /// <summary>
    /// An implementation of a Binary Tree data structure.
    /// </summary>
	/// <typeparam name="T">The type of elements in the <see cref="BinaryTree{T}"/>.</typeparam>
    //[Serializable]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class BinaryTree<T> : ICollection<T>, ITree<T>
    {
        #region Globals

        private BinaryTree<T> _leftSubtree;
        private BinaryTree<T> _rightSubtree;
        private T _data;

        #endregion

        #region Construction

        /// <param name="data">The data contained in this node.</param>
        public BinaryTree(T data) : this(data, null, null) { }

        /// <param name="data">The data.</param>
        /// <param name="left">The data of the left subtree.</param>
        /// <param name="right">The data of the right subtree.</param>
        public BinaryTree(T data, T left, T right) : this(data, new BinaryTree<T>(left), new BinaryTree<T>(right)) { }

        /// <param name="data">The data contained in this node.</param>
        /// <param name="left">The left subtree.</param>
        /// <param name="right">The right subtree.</param>
        public BinaryTree(T data, BinaryTree<T> left, BinaryTree<T> right)
            : this(data, left, right, true)
        {
        }



        /// <param name="data">The data contained in this node.</param>
        /// <param name="left">The left subtree.</param>
        /// <param name="right">The right subtree.</param>
        /// <param name="validateData"><see langword="true"/> to validate <paramref name="data"/>; otherwise <see langword="false"/>.</param>
        internal BinaryTree(T data, BinaryTree<T> left, BinaryTree<T> right, bool validateData)
        {
            #region Validation

            //TODO: probably not the most efficient way of doing this but SplayTree needs to use a BinaryTree with null data.
            if (validateData)
            {
                Guard.ArgumentNotNull(data, "data");
            }

            #endregion

            _leftSubtree = left;

            if (left != null)
            {
                left.Parent = this;
            }

            _rightSubtree = right;

            if (right != null)
            {
                right.Parent = this;
            }

            _data = data;
        }



        #endregion

        #region ICollection<T> Members

        /// <inheritdoc />
        public bool IsEmpty
        {
            get
            {
                return Count == 0;
            }
        }

        /// <inheritdoc />
        public bool IsFull
        {
            get
            {
                return (_leftSubtree != null) && (_rightSubtree != null);
            }
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            foreach (var thisItem in this)
            {
                if (item.Equals(thisItem))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            Guard.ArgumentNotNull(array, "array");

            foreach (var item in this)
            {
                #region Validation

                if (arrayIndex >= array.Length)
                {
                    throw new ArgumentException(Constants.NotEnoughSpaceInTheTargetArray, "array");
                }

                #endregion

                array[arrayIndex++] = item;
            }
        }

        /// <inheritdoc />
        public int Count
        {
            get
            {
                var count = 0;

                if (_leftSubtree != null)
                {
                    count++;
                }

                if (_rightSubtree != null)
                {
                    count++;
                }

                return count;
            }
        }

        /// <inheritdoc />
        public void Add(T item)
        {
            AddItem(new BinaryTree<T>(item));
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            if (_leftSubtree != null)
            {
                if (_leftSubtree._data.Equals(item))
                {
                    RemoveLeft();
                    return true;
                }
            }

            if (_rightSubtree != null)
            {
                if (_rightSubtree._data.Equals(item))
                {
                    RemoveRight();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the specified child.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <returns>A value indicating whether the child was found (and removed) from this tree.</returns>
        public bool Remove(BinaryTree<T> child)
        {
            if (_leftSubtree != null)
            {
                if (_leftSubtree == child)
                {
                    RemoveLeft();
                    return true;
                }
            }

            if (_rightSubtree != null)
            {
                if (_rightSubtree == child)
                {
                    RemoveRight();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            var stack = new Stack<BinaryTree<T>>();

            stack.Push(this);

            while (stack.Count > 0)
            {
                var tree = stack.Pop();

                yield return tree.Data;

                if (tree._leftSubtree != null)
                {
                    stack.Push(tree._leftSubtree);
                }

                if (tree._rightSubtree != null)
                {
                    stack.Push(tree._rightSubtree);
                }
            }
        }

        /// <inheritdoc />
        public virtual void Clear()
        {
            if (_leftSubtree != null)
            {
                _leftSubtree.Parent = null;
                _leftSubtree = null;
            }

            if (_rightSubtree != null)
            {
                _rightSubtree.Parent = null;
                _rightSubtree = null;
            }
        }

        #endregion

        #region ITree<T> Members

        /// <inheritdoc />
        void ITree<T>.Add(ITree<T> child)
        {
            AddItem((BinaryTree<T>)child);
        }

        /// <inheritdoc />
        ITree<T> ITree<T>.GetChild(int index)
        {
            return GetChild(index);
        }

        /// <inheritdoc />
        bool ITree<T>.Remove(ITree<T> child)
        {
            return Remove((BinaryTree<T>)child);
        }

        /// <inheritdoc />
        ITree<T> ITree<T>.FindNode(Predicate<T> condition)
        {
            return FindNode(condition);
        }

        /// <inheritdoc />
        ITree<T> ITree<T>.Parent
        {
            get
            {
                return Parent;
            }
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets the parent of the current node..
        /// </summary>
        /// <value>The parent of the current node.</value>
        public BinaryTree<T> Parent { get; private set; }

        /// <summary>
        /// Finds the node with the specified condition.  If a node is not found matching
        /// the specified condition, null is returned.
        /// </summary>
        /// <param name="condition">The condition to test.</param>
        /// <returns>The first node that matches the condition supplied.  If a node is not found, null is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="condition"/> is a null reference (<c>Nothing</c> in Visual Basic).</exception>
        public BinaryTree<T> FindNode(Predicate<T> condition)
        {
            Guard.ArgumentNotNull(condition, "condition");

            if (condition(Data))
            {
                return this;
            }

            if (_leftSubtree != null)
            {
                var ret = _leftSubtree.FindNode(condition);

                if (ret != null)
                {
                    return ret;
                }
            }

            if (_rightSubtree != null)
            {
                var ret = _rightSubtree.FindNode(condition);

                if (ret != null)
                {
                    return ret;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets or sets the left subtree.
        /// </summary>
        /// <value>The left subtree.</value>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual BinaryTree<T> Left
        {
            get
            {
                return _leftSubtree;
            }
            set
            {
                if (_leftSubtree != null)
                {
                    RemoveLeft();
                }

                if (value != null)
                {
                    if (value.Parent != null)
                    {
                        value.Parent.Remove(value);
                    }

                    value.Parent = this;
                }

                _leftSubtree = value;
            }
        }

        /// <summary>
        /// Gets or sets the right subtree.
        /// </summary>
        /// <value>The right subtree.</value>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual BinaryTree<T> Right
        {
            get
            {
                return _rightSubtree;
            }
            set
            {
                if (_rightSubtree != null)
                {
                    RemoveRight();
                }

                if (value != null)
                {
                    if (value.Parent != null)
                    {
                        value.Parent.Remove(value);
                    }

                    value.Parent = this;
                }

                _rightSubtree = value;
            }
        }

        /// <inheritdoc />
        public virtual T Data
        {
            get
            {
                return _data;
            }
            set
            {
                #region Validation

                Guard.ArgumentNotNull(value, "data");

                #endregion

                _data = value;
            }
        }

        /// <inheritdoc />
        public int Degree
        {
            get
            {
                return Count;
            }
        }

        /// <summary>
        /// Gets the child at the specified index.
        /// </summary>
        /// <param name="index">The index of the child in question.</param>
        /// <returns>The child at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> does not equal 0 or 1.</exception>
        public BinaryTree<T> GetChild(int index)
        {
            switch (index)
            {
                case 0:
                    return _leftSubtree;
                case 1:
                    return _rightSubtree;
                default:
                    throw new ArgumentOutOfRangeException("index");
            }
        }
        /// <inheritdoc />
        public virtual int Height
        {
            get
            {
                if (Degree == 0)
                {
                    return 0;
                }

                return 1 + FindMaximumChildHeight();
            }
        }


        /// <summary>
        /// Performs a depth first traversal on this tree with the specified visitor.
        /// </summary>
        /// <param name="orderedVisitor">The ordered visitor.</param>
        /// <exception cref="ArgumentNullException"><paramref name="orderedVisitor"/> is a null reference (<c>Nothing</c> in Visual Basic).</exception>
        public virtual void DepthFirstTraversal(OrderedVisitor<T> orderedVisitor)
        {
            Guard.ArgumentNotNull(orderedVisitor, "orderedVisitor");

            if (orderedVisitor.HasCompleted)
            {
                return;
            }

            // Preorder visit
            orderedVisitor.VisitPreOrder(Data);

            if (_leftSubtree != null)
            {
                _leftSubtree.DepthFirstTraversal(orderedVisitor);
            }

            // In-order visit
            orderedVisitor.VisitInOrder(_data);

            if (_rightSubtree != null)
            {
                _rightSubtree.DepthFirstTraversal(orderedVisitor);
            }

            // PostOrder visit
            orderedVisitor.VisitPostOrder(Data);
        }

        /// <summary>
        /// Performs a breadth first traversal on this tree with the specified visitor.
        /// </summary>
        /// <param name="visitor">The visitor.</param>
        /// <exception cref="ArgumentNullException"><paramref name="visitor"/> is a null reference (<c>Nothing</c> in Visual Basic).</exception>
        public virtual void BreadthFirstTraversal(IVisitor<T> visitor)
        {
            Guard.ArgumentNotNull(visitor, "visitor");

            var queue = new Queue<BinaryTree<T>>();

            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                if (visitor.HasCompleted)
                {
                    break;
                }

                var binaryTree = queue.Dequeue();
                visitor.Visit(binaryTree.Data);

                for (var i = 0; i < binaryTree.Degree; i++)
                {
                    var child = binaryTree.GetChild(i);

                    if (child != null)
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }


        /// <inheritdoc />
        public virtual bool IsLeafNode
        {
            get
            {
                return Degree == 0;
            }
        }

        /// <summary>
        /// Removes the left child.
        /// </summary>
        public virtual void RemoveLeft()
        {
            if (_leftSubtree != null)
            {
                _leftSubtree.Parent = null;
                _leftSubtree = null;
            }
        }

        /// <summary>
        /// Removes the left child.
        /// </summary>
        public virtual void RemoveRight()
        {
            if (_rightSubtree != null)
            {
                _rightSubtree.Parent = null;
                _rightSubtree = null;
            }
        }

        /// <summary>
        /// Adds an item to the <see cref="ICollection{T}"/>.
        /// </summary>
        /// <param name="subtree">The subtree.</param>
        /// <exception cref="NotSupportedException">The <see cref="ICollection{T}"/> is read-only.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="BinaryTree{T}"/> is full.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="subtree"/> is null (Nothing in Visual Basic).</exception>
        public void Add(BinaryTree<T> subtree)
        {
            Guard.ArgumentNotNull(subtree, "subtree");

            AddItem(subtree);
        }

        /// <summary>
        /// Adds an item to the <see cref="ICollection{T}"/>.
        /// </summary>
        /// <param name="subtree">The subtree.</param>
        /// <remarks>
        /// 	<b>Notes to Inheritors: </b>
        /// Derived classes can override this method to change the behavior of the <see cref="Clear"/> method.
        /// </remarks>
		protected virtual void AddItem(BinaryTree<T> subtree)
        {
            if (_leftSubtree == null)
            {
                if (subtree.Parent != null)
                {
                    subtree.Parent.Remove(subtree);
                }

                _leftSubtree = subtree;
                subtree.Parent = this;
            }
            else if (_rightSubtree == null)
            {
                if (subtree.Parent != null)
                {
                    subtree.Parent.Remove(subtree);
                }

                _rightSubtree = subtree;
                subtree.Parent = this;
            }
            else
            {
                throw new InvalidOperationException("This binary tree is full.");
            }
        }


        #endregion

        #region Private Members

        /// <summary>
        /// Finds the maximum height between the child nodes.
        /// </summary>
		/// <returns>The maximum height of the tree between all paths from this node and all leaf nodes.</returns>
        protected virtual int FindMaximumChildHeight()
        {
            var leftHeight = 0;
            var rightHeight = 0;

            if (_leftSubtree != null)
            {
                leftHeight = _leftSubtree.Height;
            }

            if (_rightSubtree != null)
            {
                rightHeight = _rightSubtree.Height;
            }

            return leftHeight > rightHeight ? leftHeight : rightHeight;
        }

        #endregion

        #region Operator Overloads

        /// <summary>
        /// Gets the <see cref="BinaryTree{T}"/> at the specified index.
        /// </summary>
        public BinaryTree<T> this[int index]
        {
            get
            {
                return GetChild(index);
            }
        }

        #endregion

        #region ICollection<T> Members

        /// <inheritdoc />
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

        #region Object Members

        /// <inheritdoc />
        public override string ToString()
        {
            return _data.ToString();
        }

        #endregion
    }
}