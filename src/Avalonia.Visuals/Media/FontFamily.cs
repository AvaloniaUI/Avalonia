// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media.Fonts;

namespace Avalonia.Media
{
    public class FontFamily
    {
        public FontFamily(string name = "Courier New")
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public FontFamily(string name, Uri source) : this(name)
        {
            Key = new FontFamilyKey(source);
        }

        public string Name { get; }

        public FontFamilyKey Key { get; }

        public override string ToString()
        {
            if (Key != null)
            {
                return Key + "#" + Name;
            }

            return Name;
        }
    }
}
