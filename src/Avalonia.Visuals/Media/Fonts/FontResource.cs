// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Media.Fonts
{
    internal class FontResource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FontResource"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        public FontResource(Uri source)
        {
            Source = source;
        }

        /// <summary>
        /// Gets the source.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public Uri Source { get; }
    }
}