// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Markup.Xaml.Data
{
    public enum RelativeSourceMode
    {
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