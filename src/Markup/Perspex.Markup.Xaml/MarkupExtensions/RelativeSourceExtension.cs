// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using OmniXaml;
using Perspex.Markup.Xaml.Data;

namespace Perspex.Markup.Xaml.MarkupExtensions
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