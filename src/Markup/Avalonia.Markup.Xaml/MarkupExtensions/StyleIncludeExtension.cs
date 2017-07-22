// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Portable.Xaml;
using Portable.Xaml.ComponentModel;
using System.ComponentModel;
using Portable.Xaml.Markup;
using System;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    [MarkupExtensionReturnType(typeof(IStyle))]
    public class StyleIncludeExtension : MarkupExtension
    {
        public StyleIncludeExtension()
        {
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
#if !OMNIXAML
            var tdc = (ITypeDescriptorContext)serviceProvider;

            return new StyleInclude(tdc.GetBaseUri()) { Source = Source };
#else
            throw new NotImplementedException("Just to support some time Omnixaml Compile!");
#endif
        }

        public Uri Source { get; set; }

    }
}