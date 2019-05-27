// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Avalonia.Controls
{

    /// <summary>
    /// MaxRatioComparer.
    /// Sort by w/max (stored in SizeCache), ascending.
    /// We query the list from the back, i.e. in descending order of w/max.
    /// </summary>
    internal class MaxRatioComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            DefinitionBase definitionX = x as DefinitionBase;
            DefinitionBase definitionY = y as DefinitionBase;

            int result;

            if (!Grid.CompareNullRefs(definitionX, definitionY, out result))
            {
                result = definitionX.SizeCache.CompareTo(definitionY.SizeCache);
            }

            return result;
        }
    }
}