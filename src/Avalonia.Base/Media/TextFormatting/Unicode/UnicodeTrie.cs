// RichTextKit
// Copyright © 2019 Topten Software. All Rights Reserved.
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
// Ported from: https://github.com/foliojs/unicode-trie
// Copied from: https://github.com/toptensoftware/RichTextKit

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.Media.TextFormatting.Unicode
{
    internal ref struct UnicodeTrie
    {
        public UnicodeTrie(ReadOnlySpan<uint> data, int highStart, uint errorValue)
        {
            Data = data;
            HighStart = highStart;
            ErrorValue = errorValue;
        }

        public ReadOnlySpan<uint> Data { get; }

        public int HighStart { get; }

        public uint ErrorValue { get; }

        /// <summary>
        /// Get the value for a code point as stored in the trie.
        /// </summary>
        /// <param name="codePoint">The code point.</param>
        /// <returns>The <see cref="uint"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Get(uint codePoint)
        {
            uint index;
            ref uint dataBase = ref MemoryMarshal.GetReference(Data);

            if (codePoint is < 0x0d800 or (> 0x0dbff and <= 0x0ffff))
            {
                // Ordinary BMP code point, excluding leading surrogates.
                // BMP uses a single level lookup.  BMP index starts at offset 0 in the Trie2 index.
                // 16 bit data is stored in the index array itself.
                index = Data[(int)(codePoint >> UnicodeTrieBuilder.SHIFT_2)];
                index = (index << UnicodeTrieBuilder.INDEX_SHIFT) + (codePoint & UnicodeTrieBuilder.DATA_MASK);
                return Unsafe.Add(ref dataBase, (nint)index);
            }

            if (codePoint <= 0xffff)
            {
                // Lead Surrogate Code Point.  A Separate index section is stored for
                // lead surrogate code units and code points.
                //   The main index has the code unit data.
                //   For this function, we need the code point data.
                // Note: this expression could be refactored for slightly improved efficiency, but
                //       surrogate code points will be so rare in practice that it's not worth it.
                index = Data[(int)(UnicodeTrieBuilder.LSCP_INDEX_2_OFFSET + ((codePoint - 0xd800) >> UnicodeTrieBuilder.SHIFT_2))];
                index = (index << UnicodeTrieBuilder.INDEX_SHIFT) + (codePoint & UnicodeTrieBuilder.DATA_MASK);
                return Unsafe.Add(ref dataBase, (nint)index);
            }

            if (codePoint < HighStart)
            {
                // Supplemental code point, use two-level lookup.
                index = UnicodeTrieBuilder.INDEX_1_OFFSET - UnicodeTrieBuilder.OMITTED_BMP_INDEX_1_LENGTH + (codePoint >> UnicodeTrieBuilder.SHIFT_1);
                index = Data[(int)index];
                index += (codePoint >> UnicodeTrieBuilder.SHIFT_2) & UnicodeTrieBuilder.INDEX_2_MASK;
                index = Data[(int)index];
                index = (index << UnicodeTrieBuilder.INDEX_SHIFT) + (codePoint & UnicodeTrieBuilder.DATA_MASK);
                return Unsafe.Add(ref dataBase, (nint)index);
            }

            if (codePoint <= 0x10ffff)
            {
                return Data[Data.Length - UnicodeTrieBuilder.DATA_GRANULARITY];
            }

            // Fall through.  The code point is outside of the legal range of 0..0x10ffff.
            return ErrorValue;
        }
    }
}
