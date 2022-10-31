using System;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Styling;

namespace Avalonia.UnitTests
{
    public static class TestExtensions
    {
        public static void ApplyTemplate(this IContentPresenter presenter) => ((Layoutable)presenter).ApplyTemplate();
        public static void ApplyTemplate(this IItemsPresenter presenter) => ((Layoutable)presenter).ApplyTemplate();
    }
}
