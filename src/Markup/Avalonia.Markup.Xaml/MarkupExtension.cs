using System;

namespace Avalonia.Markup.Xaml
{
    public abstract class MarkupExtension
    {
        public abstract object ProvideValue(IServiceProvider serviceProvider);
    }
}
