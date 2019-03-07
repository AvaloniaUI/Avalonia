// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls
{
    internal class Range<T>
    {
        public Range(int lowerBound, int upperBound, T value)
        {
            LowerBound = lowerBound;
            UpperBound = upperBound;
            Value = value;
        }

        public int Count
        {
            get
            {
                return UpperBound - LowerBound + 1;
            }
        }

        public int LowerBound
        {
            get;
            set;
        }

        public int UpperBound
        {
            get;
            set;
        }

        public T Value
        {
            get;
            set;
        }

        public bool ContainsIndex(int index)
        {
            return (LowerBound <= index) && (UpperBound >= index);
        }

        public bool ContainsValue(object value)
        {
            if (Value == null)
            {
                return value == null;
            }
            else
            {
                return Value.Equals(value);
            }
        }

        public Range<T> Copy()
        {
            return new Range<T>(LowerBound, UpperBound, Value);
        }
    }
}