// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;

namespace Avalonia.Controls
{
    internal class RoundingErrorIndexComparer : IComparer
    {
        private readonly double[] errors;

        internal RoundingErrorIndexComparer(double[] errors)
        {
            Contract.Requires<NullReferenceException>(errors != null);
            this.errors = errors;
        }

        public int Compare(object x, object y)
        {
            int? indexX = x as int?;
            int? indexY = y as int?;

            int result;

            if (!Grid.CompareNullRefs(indexX, indexY, out result))
            {
                double errorX = errors[indexX.Value];
                double errorY = errors[indexY.Value];
                result = errorX.CompareTo(errorY);
            }

            return result;
        }
    }
}