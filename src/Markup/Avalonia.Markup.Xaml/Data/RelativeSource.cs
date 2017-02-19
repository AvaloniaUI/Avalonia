// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Markup.Xaml.Data
{
    public enum RelativeSourceMode
    {
        Self,
        DataContext,
        TemplatedParent,
    }

    public class RelativeSource
    {
        public RelativeSource()
        {
        }

        public RelativeSource(RelativeSourceMode mode)
        {
            Mode = mode;
        }

        public RelativeSourceMode Mode { get; set; }
    }
}