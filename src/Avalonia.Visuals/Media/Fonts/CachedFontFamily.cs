// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Holds a quantity of <see cref="FontResource"/> that belongs to a specific <see cref="FontFamilyKey"/>
    /// </summary>
    public class CachedFontFamily
    {
        private readonly FontResourceCollection _fontResourceCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedFontFamily"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="fontResourceCollection">The font resource collection.</param>
        public CachedFontFamily(FontFamilyKey key, FontResourceCollection fontResourceCollection)
        {
            Key = key;
            _fontResourceCollection = fontResourceCollection;
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public FontFamilyKey Key { get; }

        /// <summary>
        /// Gets the font resources.
        /// </summary>
        /// <value>
        /// The font resources.
        /// </value>
        public IEnumerable<FontResource> FontResources => _fontResourceCollection.FontResources;
    }
}