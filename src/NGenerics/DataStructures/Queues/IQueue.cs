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
using System.Diagnostics.CodeAnalysis;

namespace NGenerics.DataStructures.Queues
{
    /// <summary>
    /// A queue interface.
	/// </summary>
	/// <typeparam name="T">The type of the elements in the queue.</typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    public interface IQueue<T>
    {
        /// <summary>
        /// Enqueues the item at the back of the queue.
        /// </summary>
        /// <param name="item">The item.</param>
        void Enqueue(T item);

        /// <summary>
        /// Dequeues the item at the front of the queue.
        /// </summary>
        /// <returns>The item at the front of the queue.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="Deque{T}"/> is empty.</exception>
        T Dequeue();

        /// <summary>
        /// Peeks at the item in the front of the queue, without removing it.
        /// </summary>
        /// <returns>The item at the front of the queue.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="Deque{T}"/> is empty.</exception>
        T Peek();
    }
}
