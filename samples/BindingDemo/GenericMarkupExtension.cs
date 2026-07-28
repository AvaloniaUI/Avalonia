using System;
using Avalonia.Markup.Xaml;

namespace BindingDemo;

internal class GenericMarkupExtension<T> : MarkupExtension
{
    public T? Value { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return $"{Value?.GetType().Name}: {Value}";
    }
}
