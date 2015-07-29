// -----------------------------------------------------------------------
// <copyright file="IPerspexList.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Collections
{
    using System.Collections;
    using System.Collections.Generic;

    public interface IPerspexList<T> : IList<T>, IPerspexReadOnlyList<T>
    {
        void AddRange(IEnumerable<T> items);

        void InsertRange(int index, IEnumerable<T> items);

        void RemoveAll(IEnumerable<T> items);
    }
}