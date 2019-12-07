// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Utility;

namespace Avalonia.Media.Text.Unicode
{
    public static class CodepointReader
    {
        public static int Read(ReadOnlySlice<char> text, ref int pos)
        {
            var cp = Peek(text, pos, out var count);

            pos += count;

            return cp;
        }

        public static int Peek(ReadOnlySlice<char> text, int pos, out int count)
        {
            ushort hi, low;
            var code = text[pos];
            count = 1;

            //# High surrogate
            if (0xD800 <= code && code <= 0xDBFF)
            {
                hi = code;

                if (pos == text.End)
                {
                    return hi;
                }

                low = text[pos + 1];

                if (0xDC00 <= low && low <= 0xDFFF)
                {
                    count = 2;
                    return (hi - 0xD800) * 0x400 + (low - 0xDC00) + 0x10000;
                }

                return hi;
            }

            //# Low surrogate
            if (0xDC00 <= code && code <= 0xDFFF)
            {
                if (pos == 0)
                {
                    return code;
                }

                hi = text[pos - 1];
                low = code;

                if (0xD800 <= hi && hi <= 0xDBFF)
                {
                    count = 2;
                    return (hi - 0xD800) * 0x400 + (low - 0xDC00) + 0x10000;
                }

                return low;
            }

            return code;
        }
    }
}
