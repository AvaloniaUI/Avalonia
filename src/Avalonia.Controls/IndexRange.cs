// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls
{
    internal readonly struct IndexRange
    {
        public IndexRange(int begin, int end)
        {
            // Accept out of order begin/end pairs, just swap them.
            if (begin > end)
            {
                int temp = begin;
                begin = end;
                end = temp;
            }

            Begin = begin;
            End = end;
        }

        public int Begin { get; }
        public int End { get; }

        public bool Contains(int index)
        {
            return index >= Begin && index <= End;
        }

        public bool Split(int splitIndex, out IndexRange before, out IndexRange after)
        {
            bool afterIsValid;

            before = new IndexRange(Begin, splitIndex);

            if (splitIndex < End)
            {
                after = new IndexRange(splitIndex + 1, End);
                afterIsValid = true;
            }
            else
            {
                after = new IndexRange();
                afterIsValid = false;
            }

            return afterIsValid;
        }

        public bool Intersects(IndexRange other)
        {
            return (Begin <= other.End) && (End >= other.Begin);
        }    
    }
}
