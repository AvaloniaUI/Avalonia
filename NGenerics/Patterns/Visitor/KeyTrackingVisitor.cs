/*  
  Copyright 2007-2013 The NGenerics Team
 (https://github.com/ngenerics/ngenerics/wiki/Team)

 This program is licensed under the GNU Lesser General Public License (LGPL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://www.gnu.org/copyleft/lesser.html.
*/


using System.Collections.Generic;

namespace NGenerics.Patterns.Visitor
{
    /// <summary>
    /// A visitor that tracks (stores) keys from KeyValuePAirs in the order they were visited.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys for the items to be visited.</typeparam>
    /// <typeparam name="TValue">The type of the values for the items to be visited.</typeparam>
    public sealed class KeyTrackingVisitor<TKey, TValue> : IVisitor<KeyValuePair<TKey, TValue>>
    {
        #region Globals

        private readonly List<TKey> tracks;

        #endregion

        #region Construction

        /// <inheritdoc/>
        public KeyTrackingVisitor()
        {
            tracks = new List<TKey>();
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets the tracking list.
        /// </summary>
        /// <value>The tracking list.</value>
        public IList<TKey> TrackingList
        {
            get
            {
                return tracks;
            }
        }

        #endregion

        #region IVisitor<KeyValuePair<TKey,TValue>> Members


        /// <inheritdoc />
        public void Visit(KeyValuePair<TKey, TValue> obj)
        {
            tracks.Add(obj.Key);
        }

        /// <inheritdoc />
        public bool HasCompleted
        {
            get
            {
                return false;
            }
        }

        #endregion
    }
}