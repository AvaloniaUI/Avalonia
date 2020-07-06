using System;
using Avalonia.Controls;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class ResolveByNameExtension
    {   
        public string Name { get; set; }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            var namescope = serviceProvider.GetService<INameScope>();
            var provideValueTarget = serviceProvider.GetService<IProvideValueTarget>();

            var value = namescope.FindAsync(Name);

            if(value.IsCompleted)
            {
                return value.GetResult();
            }
            else
            {
                value.OnCompleted(() =>
                {
                    if(provideValueTarget is AvaloniaObject ao)
                    {
                        ao.SetValue(provideValueTarget.TargetProperty as AvaloniaProperty, value);
                    }
                });

                return null;
            }
        }
    }
}
