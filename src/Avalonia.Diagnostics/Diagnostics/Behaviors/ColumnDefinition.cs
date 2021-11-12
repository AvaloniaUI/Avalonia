namespace Avalonia.Diagnostics.Behaviors
{
    /// <summary>
    /// See discussion https://github.com/AvaloniaUI/Avalonia/discussions/6773
    /// </summary>
    static class ColumnDefinition
    {
        private readonly static Avalonia.Controls.GridLength ZeroWidth =
           new Avalonia.Controls.GridLength(0, Avalonia.Controls.GridUnitType.Pixel);

        private readonly static AttachedProperty<Avalonia.Controls.GridLength?> LastWidthProperty =
            AvaloniaProperty.RegisterAttached<Avalonia.Controls.ColumnDefinition, Avalonia.Controls.GridLength?>("LastWidth"
                , typeof(ColumnDefinition)
                , default);

        public readonly static AttachedProperty<bool> IsVisibleProperty =
             AvaloniaProperty.RegisterAttached<Avalonia.Controls.ColumnDefinition, bool>("IsVisible"
                 , typeof(ColumnDefinition)
                 , true
                 , coerce: (element, visibility) =>
                     {

                         var lastWidth = element.GetValue(LastWidthProperty);
                         if (visibility == true && lastWidth is { })
                         {
                             element.SetValue(Avalonia.Controls.ColumnDefinition.WidthProperty, lastWidth);
                         }
                         else if (visibility == false)
                         {
                             element.SetValue(LastWidthProperty, element.GetValue(Avalonia.Controls.ColumnDefinition.WidthProperty));
                             element.SetValue(Avalonia.Controls.ColumnDefinition.WidthProperty, ZeroWidth);
                         }
                         return visibility;
                     }
                 );

        public static bool GetIsVisible(Avalonia.Controls.ColumnDefinition columnDefinition)
        {
            return columnDefinition.GetValue(IsVisibleProperty);
        }

        public static void SetIsVisible(Avalonia.Controls.ColumnDefinition columnDefinition, bool visibility)
        {
            columnDefinition.SetValue(IsVisibleProperty, visibility);
        }
    }
}
