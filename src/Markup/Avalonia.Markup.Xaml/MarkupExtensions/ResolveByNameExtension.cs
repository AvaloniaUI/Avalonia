using System;
using Avalonia.Controls;
using Avalonia.Data.Core;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class ResolveByNameExtension
    {
        public ResolveByNameExtension(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            var namescope = serviceProvider.GetService<INameScope>();
            
            var value = namescope.FindAsync(Name);

            if(value.IsCompleted)
                return value.GetResult();

            var provideValueTarget = serviceProvider.GetService<IProvideValueTarget>();
            var target = provideValueTarget.TargetObject;
            var property = provideValueTarget.TargetProperty as IPropertyInfo;

            if (property != null) 
                value.OnCompleted(() => property.Set(target, value.GetResult()));

            return null;
        }
    }
}
