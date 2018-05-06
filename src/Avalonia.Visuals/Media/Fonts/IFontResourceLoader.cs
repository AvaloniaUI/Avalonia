// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Avalonia.Media.Fonts
{
    public interface IFontResourceLoader
    {
        IEnumerable<FontResource> GetFontResources(FontFamilyKey fontFamilyKey);
    }
}