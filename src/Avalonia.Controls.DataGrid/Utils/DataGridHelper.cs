namespace Avalonia.Controls
{
    internal static class DataGridHelper
    {
        internal static void SyncColumnProperty<T>(AvaloniaObject column, AvaloniaObject content, AvaloniaProperty<T> property)
        {
            SyncColumnProperty(column, content, property, property);
        }

        internal static void SyncColumnProperty<T>(AvaloniaObject column, AvaloniaObject content, AvaloniaProperty<T> contentProperty, AvaloniaProperty<T> columnProperty)
        {
            if (!column.IsSet(columnProperty))
            {
                content.ClearValue(contentProperty);
            }
            else
            {
                content.SetValue(contentProperty, column.GetValue(columnProperty));
            }
        }
    }
}
