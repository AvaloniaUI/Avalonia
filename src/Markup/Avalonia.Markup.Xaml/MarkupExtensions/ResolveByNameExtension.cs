using System;
using Avalonia.Controls;
using Avalonia.Data.Core;

#nullable  enable

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class ResolveByNameExtension
    {
        public ResolveByNameExtension(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public object? ProvideValue(IServiceProvider serviceProvider)
        {
            var nameScope = serviceProvider.GetService<INameScope>();
            
            var value = nameScope.FindAsync(Name);

            if(value.IsCompleted)
                return value.GetResult();

            var provideValueTarget = serviceProvider.GetService<IProvideValueTarget>();
            var target = provideValueTarget.TargetObject;

            if (provideValueTarget.TargetProperty is IPropertyInfo property) 
                value.OnCompleted(() => property.Set(target, value.GetResult()));

            return AvaloniaProperty.UnsetValue;
        }
    }
}
