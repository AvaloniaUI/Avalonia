/*  
  Copyright 2007-2013 The NGenerics Team
 (https://github.com/ngenerics/ngenerics/wiki/Team)

 This program is licensed under the GNU Lesser General Public License (LGPL).  You should 
 have received a copy of the license along with the source code.  If not, an online copy
 of the license can be found at http://www.gnu.org/copyleft/lesser.html.
*/


using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NGenerics.Patterns.Visitor;

namespace NGenerics.DataStructures.Trees
{

    /// <summary>
    /// An interface for Search Trees that mimic a dictionary.
    /// </summary>
    /// <typeparam name="T">The type of element to hold in the tree.</typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public interface ISearchTree<T> : ICollection<T>  {
        /// <summary>
        /// Gets the largest item in the tree.
        /// </summary>
        /// <value>The largest item in the tree.</value>
        /// <exception cref="InvalidOperationException">The <see cref="ISearchTree{T}"/> is empty.</exception>
        T Maximum { get; }

        /// <summary>
        /// Gets the smallest item in the tree.
        /// </summary>
        /// <value>The smallest item in the tree.</value>
        /// <exception cref="InvalidOperationException">The <see cref="ISearchTree{T}"/> is empty.</exception>
        T Minimum { get; }

        /// <summary>
        /// Performs a depth first traversal on the search tree.
        /// </summary>
        /// <param name="visitor">The visitor to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="visitor"/> is a null reference (<c>Nothing</c> in Visual Basic).</exception>
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="DepthFirstTraversal" lang="cs" title="The following example shows how to use the DepthFirstTraversal method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="DepthFirstTraversal" lang="vbnet" title="The following example shows how to use the DepthFirstTraversal method."/>
        /// </example>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        void DepthFirstTraversal(OrderedVisitor<T> visitor);

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        /// <example>
        /// <code source="..\..\Source\Examples\ExampleLibraryCSharp\DataStructures\Trees\BinarySearchTreeBaseExamples.cs" region="GetOrderedEnumerator" lang="cs" title="The following example shows how to use the GetOrderedEnumerator method."/>
        /// <code source="..\..\Source\Examples\ExampleLibraryVB\DataStructures\Trees\BinarySearchTreeBaseExamples.vb" region="GetOrderedEnumerator" lang="vbnet" title="The following example shows how to use the GetOrderedEnumerator method."/>
        /// </example>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IEnumerator<T> GetOrderedEnumerator();
    }
}
