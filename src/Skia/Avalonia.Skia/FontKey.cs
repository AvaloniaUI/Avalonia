// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;

namespace Avalonia.Skia
{
    internal readonly struct FontKey : IEquatable<FontKey>
    {
        public readonly FontStyle Style;
        public readonly FontWeight Weight;

        public FontKey(FontWeight weight, FontStyle style)
        {
            Style = style;
            Weight = weight;
        }

        public override int GetHashCode()
        {
            var hash = 17;
            hash = hash * 31 + (int)Style;
            hash = hash * 31 + (int)Weight;

            return hash;
        }

        public override bool Equals(object other)
        {
            return other is FontKey key && Equals(key);
        }

        public bool Equals(FontKey other)
        {
            return Style == other.Style &&
                   Weight == other.Weight;
        }
    }
}
