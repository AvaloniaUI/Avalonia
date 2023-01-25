// RichTextKit
// Copyright © 2019-2020 Topten Software. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may 
// not use this product except in compliance with the License. You may obtain 
// a copy of the License at
// 
// https://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
// License for the specific language governing permissions and limitations 
// under the License.
// Copied from: https://github.com/toptensoftware/RichTextKit

using System.Collections.Generic;

namespace Avalonia.Utilities
{
    /// <summary>
    /// Extension methods for binary searching an IReadOnlyList collection
    /// </summary>
    internal static class BinarySearchExtension
    {
        private static int GetMedian(int low, int hi)
        {
            System.Diagnostics.Debug.Assert(low <= hi);
            System.Diagnostics.Debug.Assert(hi - low >= 0, "Length overflow!");
            return low + (hi - low >> 1);
        }

        /// <summary>
        /// Performs a binary search on the entire contents of an IReadOnlyList
        /// </summary>
        /// <typeparam name="T">The list element type</typeparam>
        /// <param name="list">The list to be searched</param>
        /// <param name="value">The value to search for</param>
        /// <param name="comparer">The comparer</param>
        /// <returns>The index of the found item; otherwise the bitwise complement of the index of the next larger item</returns>
        public static int BinarySearch<T>(this IReadOnlyList<T> list, T value, IComparer<T> comparer)
        {
            return list.BinarySearch(0, list.Count, value, comparer);
        }

        /// <summary>
        /// Performs a binary search on a a subset of an IReadOnlyList
        /// </summary>
        /// <typeparam name="T">The list element type</typeparam>
        /// <param name="list">The list to be searched</param>
        /// <param name="index">The start of the range to be searched</param>
        /// <param name="length">The length of the range to be searched</param>
        /// <param name="value">The value to search for</param>
        /// <param name="comparer">A comparer</param>
        /// <returns>The index of the found item; otherwise the bitwise complement of the index of the next larger item</returns>
        public static int BinarySearch<T>(this IReadOnlyList<T> list, int index, int length, T value, IComparer<T> comparer)
        {
            // Based on this: https://referencesource.microsoft.com/#mscorlib/system/array.cs,957
            var lo = index;
            var hi = index + length - 1;
            while (lo <= hi)
            {
                var i = GetMedian(lo, hi);
                var c = comparer.Compare(list[i], value);
                if (c == 0)
                    return i;
                if (c < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }
            return ~lo;
        }
    }
}
