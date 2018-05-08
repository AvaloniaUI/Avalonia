// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Represents a font resource
    /// </summary>
    internal class FontResource
    {
        public FontResource(Uri source)
        {
            Source = source;
        }

        /// <summary>
        /// Source of the font resource.
        /// </summary>
        public Uri Source { get; }
    }
}