/*  
  Copyright 2007-2013 The NGenerics Team
 (https://github.com/ngenerics/ngenerics/wiki/Team)

 This program is licensed under the GNU Lesser General Public License (LGPL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://www.gnu.org/copyleft/lesser.html.
*/



namespace NGenerics.DataStructures.General
{
    /// <summary>
    /// The type of <see cref="Heap{T}"/> to implemented.
    /// </summary>
    public enum HeapType
    {
        /// <summary>
        /// Makes the heap a Minimum-Heap - the smallest item is kept in the root of the <see cref="Heap{T}"/>.
        /// </summary>
        Minimum,

        /// <summary>
        /// Makes the heap a Maximum-Heap - the largest item is kept in the root of the <see cref="Heap{T}"/>.
        /// </summary>
        Maximum
    }
}