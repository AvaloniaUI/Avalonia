// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Avalonia.Controls
{
    /// <summary>
    /// MinRatioComparer.
    /// Sort by w/min (stored in MeasureSize), descending.
    /// We query the list from the back, i.e. in ascending order of w/min.
    /// </summary>
    internal class MinRatioComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            DefinitionBase definitionX = x as DefinitionBase;
            DefinitionBase definitionY = y as DefinitionBase;

            int result;

            if (!Grid.CompareNullRefs(definitionY, definitionX, out result))
            {
                result = definitionY.MeasureSize.CompareTo(definitionX.MeasureSize);
            }

            return result;
        }
    }
}