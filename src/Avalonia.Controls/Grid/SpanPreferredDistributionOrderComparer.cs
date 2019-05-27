// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace Avalonia.Controls
{
    internal class SpanPreferredDistributionOrderComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            DefinitionBase definitionX = x as DefinitionBase;
            DefinitionBase definitionY = y as DefinitionBase;

            int result;

            if (!Grid.CompareNullRefs(definitionX, definitionY, out result))
            {
                if (definitionX.UserSize.IsAuto)
                {
                    if (definitionY.UserSize.IsAuto)
                    {
                        result = definitionX.MinSize.CompareTo(definitionY.MinSize);
                    }
                    else
                    {
                        result = -1;
                    }
                }
                else
                {
                    if (definitionY.UserSize.IsAuto)
                    {
                        result = +1;
                    }
                    else
                    {
                        result = definitionX.PreferredSize.CompareTo(definitionY.PreferredSize);
                    }
                }
            }

            return result;
        }
    }
}