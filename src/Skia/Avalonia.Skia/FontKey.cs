// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using SkiaSharp;

namespace Avalonia.Skia
{
    internal struct FontKey
    {
        public readonly SKFontStyleSlant Slant;
        public readonly SKFontStyleWeight Weight;

        public FontKey(SKFontStyleWeight weight, SKFontStyleSlant slant)
        {
            Slant = slant;
            Weight = weight;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + (int)Slant;
            hash = hash * 31 + (int)Weight;

            return hash;
        }

        public override bool Equals(object other)
        {
            return other is FontKey ? Equals((FontKey)other) : false;
        }

        public bool Equals(FontKey other)
        {
            return Slant == other.Slant &&
                   Weight == other.Weight;
        }

        // Equals and GetHashCode ommitted
    }
}
