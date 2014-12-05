/*  
  Copyright 2007-2013 The NGenerics Team
 (https://github.com/ngenerics/ngenerics/wiki/Team)

 This program is licensed under the GNU Lesser General Public License (LGPL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://www.gnu.org/copyleft/lesser.html.
*/


namespace NGenerics.Patterns.Visitor
{
    /// <summary>
    /// Provides an interface for visitors.
    /// </summary>
    /// <typeparam name="T">The type of objects to be visited.</typeparam>
    public interface IVisitor<T>
    {
        /// <summary>
        /// Gets a value indicating whether this instance is done performing it's work..
        /// </summary>
        /// <value><c>true</c> if this instance is done; otherwise, <c>false</c>.</value>
        bool HasCompleted { get; }

        /// <summary>
        /// Visits the specified object.
        /// </summary>
        /// <param name="obj">The object to visit.</param>
        void Visit(T obj);
    }
}