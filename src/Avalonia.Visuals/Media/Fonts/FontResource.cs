// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Media.Fonts
{
    public class FontResource
    {
        public FontResource(Uri source)
        {
            Source = source;
        }

        public Uri Source { get; }
    }
}