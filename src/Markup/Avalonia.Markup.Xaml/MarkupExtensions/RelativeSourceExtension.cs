// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Markup.Xaml.Data;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    using Portable.Xaml.Markup;
    using System;

    public class RelativeSourceExtension : MarkupExtension
    {
        public RelativeSourceExtension()
        {
        }

        public RelativeSourceExtension(RelativeSourceMode mode)
        {
            Mode = mode;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new RelativeSource
            {
                Mode = Mode,
            };
        }

        [ConstructorArgument("mode")]
        public RelativeSourceMode Mode { get; set; }
    }
}