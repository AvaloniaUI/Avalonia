/*  
  Copyright 2007-2013 The NGenerics Team
 (https://github.com/ngenerics/ngenerics/wiki/Team)

 This program is licensed under the GNU Lesser General Public License (LGPL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://www.gnu.org/copyleft/lesser.html.
*/

namespace NGenerics.DataStructures.Queues
{
    /// <summary>
    /// Specifies the Priority Queue type (min or max).
    /// </summary>
    public enum PriorityQueueType
    {
        /// <summary>
        /// Specify a Minimum <see cref="PriorityQueue{TValue, TPriority}"/>.
        /// </summary>
        Minimum = 0,

        /// <summary>
        /// Specify a Maximum <see cref="PriorityQueue{TValue, TPriority}"/>.
        /// </summary>
        Maximum = 1
    }
}