// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using OmniXaml;
using Avalonia.Markup.Xaml.Data;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class RelativeSourceExtension : MarkupExtension
    {
        public RelativeSourceExtension()
        {
        }

        public RelativeSourceExtension(RelativeSourceMode mode)
        {
            Mode = mode;
        }

        public override object ProvideValue(MarkupExtensionContext extensionContext)
        {
            return new RelativeSource
            {
                Mode = Mode,
            };
        }

        public RelativeSourceMode Mode { get; set; }
    }
}