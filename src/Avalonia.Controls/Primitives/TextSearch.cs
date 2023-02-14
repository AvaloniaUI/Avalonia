using Avalonia.Interactivity;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Allows to customize text searching in <see cref="SelectingItemsControl"/>.
    /// </summary>
    public static class TextSearch
    {
        /// <summary>
        /// Defines the Text attached property.
        /// This text will be considered during text search in <see cref="SelectingItemsControl"/> (such as <see cref="ComboBox"/>)
        /// </summary>
        public static readonly AttachedProperty<string?> TextProperty
            = AvaloniaProperty.RegisterAttached<Interactive, string?>("Text", typeof(TextSearch));

        /// <summary>
        /// Sets the <see cref="TextProperty"/> for a control.
        /// </summary>
        /// <param name="control">The control</param>
        /// <param name="text">The search text to set</param>
        public static void SetText(Control control, string? text)
        {
            control.SetValue(TextProperty, text);
        }

        /// <summary>
        /// Gets the <see cref="TextProperty"/> of a control.
        /// </summary>
        /// <param name="control">The control</param>
        /// <returns>The property value</returns>
        public static string? GetText(Control control)
        {
            return control.GetValue(TextProperty);
        }
    }
}
