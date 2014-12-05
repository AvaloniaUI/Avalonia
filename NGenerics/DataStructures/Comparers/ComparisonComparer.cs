/*  
  Copyright 2007-2013 The NGenerics Team
 (https://github.com/ngenerics/ngenerics/wiki/Team)

 This program is licensed under the GNU Lesser General Public License (LGPL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://www.gnu.org/copyleft/lesser.html.
*/


using System;
using System.Collections.Generic;
using NGenerics.Util;

namespace NGenerics.Comparers
{
    /// <summary>
    /// A Comparer using a Comparison delegate for comparisons between items.
    /// </summary>
    /// <typeparam name="T">The type of the objects to compare.</typeparam>
    //[Serializable]
    public sealed class ComparisonComparer<T> : IComparer<T>
	{
		#region Globals

		private Comparison<T> comparison;

		#endregion

		#region Construction

	
        /// <param name="comparison">The comparison.</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparison"/> is a null reference (<c>Nothing</c> in Visual Basic).</exception>
		public ComparisonComparer(Comparison<T> comparison)
		{
            Guard.ArgumentNotNull(comparison, "comparison");

			this.comparison = comparison;
		}

		#endregion

		#region Public Members

		/// <summary>
		/// Gets or sets the comparison used in this comparer.
		/// </summary>
        /// <value>The comparison used in this comparer.</value>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is a null reference (<c>Nothing</c> in Visual Basic).</exception>
		public Comparison<T> Comparison
		{
			get
			{
				return comparison;
			}
			set
			{

                Guard.ArgumentNotNull(value, "value");

				comparison = value;
			}
		}

		#endregion

		#region IComparer<T> Members

		/// <inheritdoc />
		public int Compare(T x, T y)
		{
			return comparison(x, y);
		}

		#endregion
	}
}