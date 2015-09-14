// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

/*  
  Copyright 2007-2013 The NGenerics Team
 (https://github.com/ngenerics/ngenerics/wiki/Team)

 This program is licensed under the GNU Lesser General Public License (LGPL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://www.gnu.org/copyleft/lesser.html.
*/


/*
 * The insertion and deletion code is based on the code found at http://eternallyconfuzzled.com/tuts/redblack.htm.
 * It's an excellent tutorial - if you want to understand Red Black trees, look there first.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NGenerics.DataStructures.Trees
{
    /// <summary>
    /// An implementation of a Red-Black tree.
    /// </summary>
    /// <typeparam name="T">The type of element to keep in the tree.</typeparam>
	[SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    //[Serializable]
    public class RedBlackTree<T> : BinarySearchTreeBase<T>
    {
        #region Construction

        /// <inheritdoc />
        public RedBlackTree()
        {
            // Do nothing - the default Comparer will be used by the base class.
        }


        /// <inheritdoc/>
        public RedBlackTree(IComparer<T> comparer)
            : base(comparer)
        {
            // Do nothing else.
        }

        /// <inheritdoc/>
        public RedBlackTree(Comparison<T> comparison)
            : base(comparison)
        {
            // Do nothing else.
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="item">The item.</param>
        protected override void AddItem(T item)
        {
            #region Validation

            if (Equals(item, null))
            {
                throw new ArgumentNullException("item");
            }

            #endregion

            var root = (RedBlackTreeNode<T>)Tree;

            var newRoot = InsertNode(root, item);
            newRoot.Color = NodeColor.Black;
            Tree = newRoot;
        }

        /// <summary>
        /// Removes the element with the specified key from the <see cref="IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>
        /// 	<c>true</c> if the element is successfully removed; otherwise, <c>false</c>.  This method also returns false if key was not found in the original <see cref="IDictionary{TKey,TValue}"/>.
        /// </returns>
        /// <inheritdoc/>
        protected override bool RemoveItem(T item)
        {
            if (Tree != null)
            {
                var startNode = new RedBlackTreeNode<T>(default(T));

                var childNode = startNode;
                startNode.Right = (RedBlackTreeNode<T>)Tree;

                RedBlackTreeNode<T> parent = null;
                RedBlackTreeNode<T> foundNode = null;

                var direction = true;

                while (childNode[direction] != null)
                {
                    var lastDirection = direction;

                    var grandParent = parent;
                    parent = childNode;
                    childNode = childNode[direction];

                    var comparisonValue = Comparer.Compare(childNode.Data, item);

                    if (comparisonValue == 0)
                    {
                        foundNode = childNode;
                    }

                    direction = comparisonValue < 0;

                    if ((IsBlack(childNode)) && (IsBlack(childNode[direction])))
                    {
                        if (IsRed(childNode[!direction]))
                        {
                            parent = parent[lastDirection] = SingleRotation(childNode, direction);
                        }
                        else if (IsBlack(childNode[direction]))
                        {
                            var sibling = parent[!lastDirection];

                            if (sibling != null)
                            {
                                if ((IsBlack(sibling.Left)) && (IsBlack(sibling.Right)))
                                {
                                    parent.Color = NodeColor.Black;
                                    sibling.Color = NodeColor.Red;
                                    childNode.Color = NodeColor.Red;
                                }
                                else
                                {
                                    var parentDirection = grandParent.Right == parent;

                                    if (IsRed(sibling[lastDirection]))
                                    {
                                        grandParent[parentDirection] = DoubleRotation(parent, lastDirection);
                                    }
                                    else if (IsRed(sibling[!lastDirection]))
                                    {
                                        grandParent[parentDirection] = SingleRotation(parent, lastDirection);
                                    }

                                    childNode.Color = grandParent[parentDirection].Color = NodeColor.Red;
                                    grandParent[parentDirection].Left.Color = NodeColor.Black;
                                    grandParent[parentDirection].Right.Color = NodeColor.Black;
                                }
                            }
                        }
                    }
                }

                if (foundNode != null)
                {
                    foundNode.Data = childNode.Data;
                    parent[parent.Right == childNode] = childNode[childNode.Left == null];
                }

                Tree = startNode.Right;

                if (Tree != null)
                {
                    ((RedBlackTreeNode<T>)Tree).Color = NodeColor.Black;
                }

                if (foundNode != null)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Determines whether the specified node is red.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>
        /// 	<c>true</c> if the specified node is red; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsRed(RedBlackTreeNode<T> node)
        {
            return (node != null) && (node.Color == NodeColor.Red);
        }

        /// <summary>
        /// Determines whether the specified node is black.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>
        /// 	<c>true</c> if the specified node is black; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsBlack(RedBlackTreeNode<T> node)
        {
            return (node == null) || (node.Color == NodeColor.Black);
        }

        /// <summary>
        /// A recursive implementation of insertion of a node into the tree.
        /// </summary>
        /// <param name="node">The start node.</param>
        /// <param name="item">The item.</param>
        /// <returns>The node created in the insertion.</returns>
        private RedBlackTreeNode<T> InsertNode(RedBlackTreeNode<T> node, T item)
        {
            if (node == null)
            {
                node = new RedBlackTreeNode<T>(item);
            }
            else if (Comparer.Compare(item, node.Data) != 0)
            {
                var direction = Comparer.Compare(node.Data, item) < 0;

                node[direction] = InsertNode(node[direction], item);

                if (IsRed(node[direction]))
                {
                    if (IsRed(node[!direction]))
                    {
                        node.Color = NodeColor.Red;
                        node.Left.Color = NodeColor.Black;
                        node.Right.Color = NodeColor.Black;
                    }
                    else
                    {
                        if (IsRed(node[direction][direction]))
                        {
                            node = SingleRotation(node, !direction);
                        }
                        else if (IsRed(node[direction][!direction]))
                        {
                            node = DoubleRotation(node, !direction);
                        }
                    }
                }
            }
            else
            {
                throw new ArgumentException(alreadyContainedInTheTree);
            }

            return node;
        }

        /// <summary>
        /// Perform a single rotation on the node provided..
        /// </summary>
        /// <param name="node">The node on which to focus the rotation.</param>
        /// <param name="direction">The direction of the rotation.  If direction is equal to true, a right rotation is performed.  Other wise, a left rotation.</param>
        /// <returns>The new root of the cluster.</returns>
        private static RedBlackTreeNode<T> SingleRotation(RedBlackTreeNode<T> node, bool direction)
        {
            var childSibling = node[!direction];

            node[!direction] = childSibling[direction];
            childSibling[direction] = node;

            node.Color = NodeColor.Red;
            childSibling.Color = NodeColor.Black;

            return childSibling;
        }

        /// <summary>
        /// Perform a double rotation on the node provided..
        /// </summary>
        /// <param name="node">The node on which to focus the rotation.</param>
        /// <param name="direction">The direction of the rotation.  If direction is equal to true, a right rotation is performed.  Other wise, a left rotation.</param>
        /// <returns>The new root of the cluster.</returns>
        private static RedBlackTreeNode<T> DoubleRotation(RedBlackTreeNode<T> node, bool direction)
        {
            node[!direction] = SingleRotation(node[!direction], !direction);
            return SingleRotation(node, direction);
        }

        #endregion
    }
}
