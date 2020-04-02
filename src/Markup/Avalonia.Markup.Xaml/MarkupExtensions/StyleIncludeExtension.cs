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
