// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Loads <see cref="FontResource"/> that can be identified by a given <see cref="FontFamilyKey"/>
    /// </summary>
    public interface IFontResourceLoader
    {
        /// <summary>
        /// Returns a quanity of <see cref="FontResource"/> that belongs to a given <see cref="FontFamilyKey"/>
        /// </summary>
        /// <param name="fontFamilyKey"></param>
        /// <returns></returns>
        IEnumerable<FontResource> GetFontResources(FontFamilyKey fontFamilyKey);
    }
}