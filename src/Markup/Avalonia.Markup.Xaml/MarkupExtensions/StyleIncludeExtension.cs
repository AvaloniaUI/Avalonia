// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System.ComponentModel;
using System;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class StyleIncludeExtension
    {
        public StyleIncludeExtension()
        {
        }

        public IStyle ProvideValue(IServiceProvider serviceProvider)
        {
            return new StyleInclude(serviceProvider.GetContextBaseUri()) { Source = Source };
        }

        public Uri Source { get; set; }

    }
}
