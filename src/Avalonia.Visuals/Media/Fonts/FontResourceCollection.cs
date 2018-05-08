// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Represents a collection of <see cref="FontResource"/> that is identified by a unique <see cref="FontFamilyKey"/>
    /// </summary>
    internal class FontResourceCollection
    {
        private Dictionary<Uri, FontResource> _fontResources;
        private readonly IFontResourceLoader _fontResourceLoader = new FontResourceLoader();

        public FontResourceCollection(FontFamilyKey key)
        {
            Key = key;
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
        public IEnumerable<FontResource> FontResources
        {
            get
            {
                if (_fontResources == null)
                {
                    _fontResources = CreateFontResources();
                }

                return _fontResources.Values;
            }
        }

        /// <summary>
        /// Creates the font resources.
        /// </summary>
        /// <returns></returns>
        private Dictionary<Uri, FontResource> CreateFontResources()
        {
            return _fontResourceLoader.GetFontResources(Key).ToDictionary(x => x.Source);
        }
    }
}