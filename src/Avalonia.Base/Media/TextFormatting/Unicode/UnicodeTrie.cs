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
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Avalonia.Media.TextFormatting.Unicode
{
    internal class UnicodeTrie
    {
        private readonly uint[] _data;
        private readonly int _highStart;
        private readonly uint _errorValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnicodeTrie"/> class.
        /// </summary>
        /// <param name="rawData">The uncompressed trie data.</param>
        public UnicodeTrie(ReadOnlySpan<byte> rawData)
        {
            var header = UnicodeTrieHeader.Parse(rawData);
            int length = header.DataLength;
            uint[] data = new uint[length / sizeof(uint)];

            MemoryMarshal.Cast<byte, uint>(rawData.Slice(rawData.Length - length))
                .CopyTo(data);

            _highStart = header.HighStart;
           _errorValue = header.ErrorValue;
            _data = data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnicodeTrie"/> class.
        /// </summary>
        /// <param name="stream">The stream containing the data.</param>
        public UnicodeTrie(Stream stream)
        {
            // Read the header info
            using (var br = new BinaryReader(stream, Encoding.UTF8, true))
            {
                _highStart = br.ReadInt32();
                _errorValue = br.ReadUInt32();
                _data = new uint[br.ReadInt32() / sizeof(uint)];
            }

            // Read the data in compressed format.
            using (var br = new BinaryReader(stream, Encoding.UTF8, true))
            {
                for (int i = 0; i < _data.Length; i++)
                {
                    _data[i] = br.ReadUInt32();
                }
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="UnicodeTrie"/> class.
        /// </summary>
        /// <param name="data">The uncompressed trie data.</param>
        /// <param name="highStart">The start of the last range which ends at U+10ffff.</param>
        /// <param name="errorValue">The value for out-of-range code points and illegal UTF-8.</param>
        public UnicodeTrie(uint[] data, int highStart, uint errorValue)
        {
            _data = data;
            _highStart = highStart;
            _errorValue = errorValue;
        }

        /// <summary>
        /// Saves the <see cref="UnicodeTrie"/> to the stream in a compressed format.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        internal void Save(Stream stream)
        {
            // Write the header info
            using (var bw = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                bw.Write(_highStart);
                bw.Write(_errorValue);
                bw.Write(_data.Length * sizeof(uint));
            }

            // Write the data.
            using (var bw = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                for (int i = 0; i < _data.Length; i++)
                {
                    bw.Write(_data[i]);
                }
            }
        }

        /// <summary>
        /// Get the value for a code point as stored in the trie.
        /// </summary>
        /// <param name="codePoint">The code point.</param>
        /// <returns>The <see cref="uint"/> value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Get(uint codePoint)
        {
            uint index;
            ref uint dataBase = ref MemoryMarshal.GetReference(_data.AsSpan());

            if (codePoint is < 0x0d800 or (> 0x0dbff and <= 0x0ffff))
            {
                // Ordinary BMP code point, excluding leading surrogates.
                // BMP uses a single level lookup.  BMP index starts at offset 0 in the Trie2 index.
                // 16 bit data is stored in the index array itself.
                index = _data[codePoint >> UnicodeTrieBuilder.SHIFT_2];
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
                index = _data[UnicodeTrieBuilder.LSCP_INDEX_2_OFFSET + ((codePoint - 0xd800) >> UnicodeTrieBuilder.SHIFT_2)];
                index = (index << UnicodeTrieBuilder.INDEX_SHIFT) + (codePoint & UnicodeTrieBuilder.DATA_MASK);
                return Unsafe.Add(ref dataBase, (nint)index);
            }

            if (codePoint < _highStart)
            {
                // Supplemental code point, use two-level lookup.
                index = UnicodeTrieBuilder.INDEX_1_OFFSET - UnicodeTrieBuilder.OMITTED_BMP_INDEX_1_LENGTH + (codePoint >> UnicodeTrieBuilder.SHIFT_1);
                index = _data[index];
                index += (codePoint >> UnicodeTrieBuilder.SHIFT_2) & UnicodeTrieBuilder.INDEX_2_MASK;
                index = _data[index];
                index = (index << UnicodeTrieBuilder.INDEX_SHIFT) + (codePoint & UnicodeTrieBuilder.DATA_MASK);
                return Unsafe.Add(ref dataBase, (nint)index);
            }

            if (codePoint <= 0x10ffff)
            {
                return Unsafe.Add(ref dataBase, (nint)(_data.Length - UnicodeTrieBuilder.DATA_GRANULARITY));
            }

            // Fall through.  The code point is outside of the legal range of 0..0x10ffff.
            return _errorValue;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct UnicodeTrieHeader
        {
            public int HighStart
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get;
            }

            public uint ErrorValue
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get;
            }

            public int DataLength
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static UnicodeTrieHeader Parse(ReadOnlySpan<byte> data)
                => MemoryMarshal.Cast<byte, UnicodeTrieHeader>(data)[0];
        }
    }
}
