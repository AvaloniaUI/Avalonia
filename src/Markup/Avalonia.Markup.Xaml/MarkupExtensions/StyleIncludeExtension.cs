// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Styling;

#if SYSTEM_XAML
using System.Windows.Markup;
#else
using Portable.Xaml.Markup;
#endif

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
            throw new NotImplementedException();
            //var tdc = (ITypeDescriptorContext)serviceProvider;
            //return new StyleInclude(tdc.GetBaseUri()) { Source = Source };
        }

        public Uri Source { get; set; }

    }
}
