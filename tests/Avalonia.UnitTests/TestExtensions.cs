using System;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Styling;

namespace Avalonia.UnitTests
{
    public static class TestExtensions
    {
        public static void ApplyTemplate(this IContentPresenter presenter) => ((Layoutable)presenter).ApplyTemplate();
        public static void ApplyTemplate(this IItemsPresenter presenter) => ((Layoutable)presenter).ApplyTemplate();
        
        public static IObservable<T> GetObservable<T>(this ITemplatedControl control, AvaloniaProperty<T> property)
        {
            return ((AvaloniaObject)control).GetObservable(property);
        }
    }
}
