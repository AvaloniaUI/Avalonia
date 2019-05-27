// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;

namespace Avalonia.Controls
{
    internal class MinRatioIndexComparer : IComparer
    {
        private readonly DefinitionBase[] definitions;

        internal MinRatioIndexComparer(DefinitionBase[] definitions)
        {
            Contract.Requires<NullReferenceException>(definitions != null);
            this.definitions = definitions;
        }

        public int Compare(object x, object y)
        {
            int? indexX = x as int?;
            int? indexY = y as int?;

            DefinitionBase definitionX = null;
            DefinitionBase definitionY = null;

            if (indexX != null)
            {
                definitionX = definitions[indexX.Value];
            }
            if (indexY != null)
            {
                definitionY = definitions[indexY.Value];
            }

            int result;

            if (!Grid.CompareNullRefs(definitionY, definitionX, out result))
            {
                result = definitionY.MeasureSize.CompareTo(definitionX.MeasureSize);
            }

            return result;
        }
    }
}