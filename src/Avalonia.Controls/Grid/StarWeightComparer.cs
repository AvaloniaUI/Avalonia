// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Avalonia.Controls
{
    /// <summary>
    /// StarWeightComparer.
    /// Sort by *-weight (stored in MeasureSize), ascending.
    /// </summary>
    internal class StarWeightComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            DefinitionBase definitionX = x as DefinitionBase;
            DefinitionBase definitionY = y as DefinitionBase;

            int result;

            if (!Grid.CompareNullRefs(definitionX, definitionY, out result))
            {
                result = definitionX.MeasureSize.CompareTo(definitionY.MeasureSize);
            }

            return result;
        }
    }
}