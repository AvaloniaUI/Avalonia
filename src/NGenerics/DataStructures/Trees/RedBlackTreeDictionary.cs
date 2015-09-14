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
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using NGenerics.Comparers;
using NGenerics.Patterns.Visitor;

namespace NGenerics.DataStructures.Trees
{
    /// <summary>
    /// An implementation of a Red-Black tree.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the <see cref="RedBlackTree{TKey,TValue}"/>.</typeparam>
    /// <typeparam name="TValue">The type of the values in the <see cref="RedBlackTree{TKey,TValue}"/>.</typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
#if (!SILVERLIGHT && !WINDOWSPHONE)
    //[Serializable]
#endif
    public class RedBlackTree<TKey, TValue> : RedBlackTree<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>  // BinarySearchTreeBase<TKey, TValue>
    {
        #region Construction
        /// <inheritdoc />
        public RedBlackTree() : base(new KeyValuePairComparer<TKey, TValue>())
        {
            // Do nothing - the default Comparer will be used by the base class.
        }


        /// <inheritdoc/>
        public RedBlackTree(IComparer<TKey> comparer)
            : base(new KeyValuePairComparer<TKey, TValue>(comparer))
        {
            // Do nothing else.
        }

        /// <inheritdoc/>
        public RedBlackTree(Comparison<TKey> comparison)
            : base(new KeyValuePairComparer<TKey, TValue>(comparison))
        {
            // Do nothing else.
        }

        #endregion

        #region Private Members

        private RedBlackTreeNode<KeyValuePair<TKey, TValue>> FindNode(TKey key)
        {
            return base.FindNode(new KeyValuePair<TKey, TValue>(key, default(TValue))) as RedBlackTreeNode<KeyValuePair<TKey, TValue>>;
        }

        private bool Contains(KeyValuePair<TKey, TValue> item, bool checkValue)
        {
            var node = FindNode(item);

            if ((node != null) && !checkValue)
            {
                return true;
            }

            return node != null && Equals(item.Value, node.Data.Value);
        }

        #endregion

        #region IDictionary<TKey,TValue> Members

        /// <summary>
        /// Removes the element with the specified key from the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false.  This method also returns false if <paramref name="key"/> was not found in the original <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="key"/> is null.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// The <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.
        /// </exception>
        public bool Remove(TKey key)
        {
            return Remove(new KeyValuePair<TKey, TValue>(key, default(TValue)));
        }

        /// <summary>
        /// Adds an element with the provided key and value to the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="key"/> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// An element with the same key already exists in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// The <see cref="T:System.Collections.Generic.IDictionary`2"/> is read-only.
        /// </exception>
        public void Add(TKey key, TValue value)
        {
            if (Equals(key, null))
            {
                throw new ArgumentNullException("key");
            }

            Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.</param>
        /// <returns>
        /// true if the <see cref="T:System.Collections.Generic.IDictionary`2"/> contains an element with the key; otherwise, false.
        /// </returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="key"/> is null.
        /// </exception>
        public bool ContainsKey(TKey key)
        {
            return Contains(new KeyValuePair<TKey, TValue>(key, default(TValue)), false);
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <value></value>
        /// <example>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="Keys" lang="cs" title="The following example shows how to use the Keys property."/>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="Keys" lang="vbnet" title="The following example shows how to use the Keys property."/>
        /// </example>
        public ICollection<TKey> Keys
        {
            get
            {
                // Get the keys in sorted order
                var visitor = new KeyTrackingVisitor<TKey, TValue>();
                var inOrderVisitor = new InOrderVisitor<KeyValuePair<TKey, TValue>>(visitor);
                DepthFirstTraversal(inOrderVisitor);
                return new ReadOnlyCollection<TKey>(visitor.TrackingList);
            }
        }


        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="TryGetValue" lang="cs" title="The following example shows how to use the TryGetValue method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="TryGetValue" lang="vbnet" title="The following example shows how to use the TryGetValue method."/>
        /// </example>
        public bool TryGetValue(TKey key, out TValue value)
        {
            var node = FindNode(new KeyValuePair<TKey, TValue>(key, default(TValue)));

            if (node == null)
            {
                value = default(TValue);
                return false;
            }

            value = node.Data.Value;
            return true;
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <value></value>
        /// <example>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="Values" lang="cs" title="The following example shows how to use the Values property."/>
        /// 	<code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="Values" lang="vbnet" title="The following example shows how to use the Values property."/>
        /// </example>
        public ICollection<TValue> Values
        {
            get
            {
                var visitor = new ValueTrackingVisitor<TKey, TValue>();
                var inOrderVisitor = new InOrderVisitor<KeyValuePair<TKey, TValue>>(visitor);

                DepthFirstTraversal(inOrderVisitor);

                return new ReadOnlyCollection<TValue>(visitor.TrackingList);
            }
        }


        /// <summary>
        /// Gets or sets the value with the specified key.
        /// </summary>
        /// <value>The key of the item to set or get.</value>
        public TValue this[TKey key]
        {
            get
            {
                var node = FindNode(key);

                if (node == null)
                {
                    throw new KeyNotFoundException("key");
                }

                return node.Data.Value;
            }
            set
            {
                var node = FindNode(key);

                if (node == null)
                {
                    throw new KeyNotFoundException("key");
                }

                node.Data = new KeyValuePair<TKey, TValue>(key, value);
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        /// <inheritdoc />
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="Contains" lang="cs" title="The following example shows how to use the Contains method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="Contains" lang="vbnet" title="The following example shows how to use the Contains method."/>
        /// </example>
        public override bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return Contains(item, true);
        }

        #endregion
    }
}