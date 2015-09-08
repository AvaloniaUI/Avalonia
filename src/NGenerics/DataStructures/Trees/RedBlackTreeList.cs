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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NGenerics.DataStructures.Trees
{
    /// <summary>
    /// A RedBlack Tree list variant.  Equivalent to <see cref="RedBlackTree{TKey,TValue}"/> where TValue is a <see cref="LinkedList{T}"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
    //[Serializable]
    internal class RedBlackTreeList<TKey, TValue> : RedBlackTree<TKey, LinkedList<TValue>>
    {
        #region Delegates

        private delegate bool NodeAction(TKey key, LinkedList<TValue> values);

        #endregion

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="RedBlackTreeList&lt;TKey, TValue&gt;"/> class.
        /// </summary>
        /// <inheritdoc/>
        public RedBlackTreeList()
        {
            // Do nothing else.
        }

        /// <inheritdoc/>
        public RedBlackTreeList(IComparer<TKey> comparer)
            : base(comparer)
        {
            // Do nothing else.
        }

        /// <inheritdoc/>
        public RedBlackTreeList(Comparison<TKey> comparison)
            : base(comparison)
        {
            // Do nothing else.
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Determines whether the specified value contains value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// 	<c>true</c> if the specified value contains value; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsValue(TValue value)
        {
            return TraverseItems((key, list) => list.Contains(value));
        }

        /// <summary>
        /// Gets the value enumerator.
        /// </summary>
        /// <returns>An enumerator to enumerate through the values contained in this instance.</returns>
        public IEnumerator<TValue> GetValueEnumerator()
        {
            var stack = new Stack<BinaryTree<KeyValuePair<TKey, LinkedList<TValue>>>>();

            if (Tree != null)
            {
                stack.Push(Tree);
            }

            while (stack.Count > 0)
            {
                var currentNode = stack.Pop();

                var list = currentNode.Data.Value;

                foreach (var item in list)
                {
                    yield return item;
                }

                if (currentNode.Left != null)
                {
                    stack.Push(currentNode.Left);
                }

                if (currentNode.Right != null)
                {
                    stack.Push(currentNode.Right);
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IEnumerator<KeyValuePair<TKey, TValue>> GetKeyEnumerator()
        {
            var stack = new Stack<BinaryTree<KeyValuePair<TKey, LinkedList<TValue>>>>();

            if (Tree != null)
            {
                stack.Push(Tree);
            }

            while (stack.Count > 0)
            {
                var currentNode = stack.Pop();

                var list = currentNode.Data.Value;

                foreach (var item in list)
                {
                    yield return new KeyValuePair<TKey, TValue>(currentNode.Data.Key, item);
                }

                if (currentNode.Left != null)
                {
                    stack.Push(currentNode.Left);
                }

                if (currentNode.Right != null)
                {
                    stack.Push(currentNode.Right);
                }
            }
        }

        /// <summary>
        /// Removes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="key">The key under which the item was found.</param>
        /// <returns>A value indicating whether the item was found or not.</returns>
        public bool Remove(TValue value, out TKey key)
        {
            var foundKey = default(TKey);

            var ret = TraverseItems(
                delegate (TKey itemKey, LinkedList<TValue> list)
                {
                    if (list.Remove(value))
                    {
                        if (list.Count == 0)
                        {
                            Remove(itemKey);
                        }

                        foundKey = itemKey;
                        return true;
                    }

                    return false;
                }
            );

            key = foundKey;
            return ret;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Traverses the items.
        /// </summary>
        /// <param name="shouldStop">A predicate that performs an action on the list, and indicates whether the enumeration of items should stop or not.</param>
        /// <returns>An indication of whether the enumeration was stopped prematurely.</returns>
        private bool TraverseItems(NodeAction shouldStop)
        {
            #region Validation

            Debug.Assert(shouldStop != null);

            #endregion

            var stack = new Stack<BinaryTree<KeyValuePair<TKey, LinkedList<TValue>>>>();

            if (Tree != null)
            {
                stack.Push(Tree);
            }

            while (stack.Count > 0)
            {
                var currentNode = stack.Pop();

                if (shouldStop(currentNode.Data.Key, currentNode.Data.Value))
                {
                    return true;
                }

                if (currentNode.Left != null)
                {
                    stack.Push(currentNode.Left);
                }

                if (currentNode.Right != null)
                {
                    stack.Push(currentNode.Right);
                }
            }

            return false;
        }

        #endregion
    }
}
