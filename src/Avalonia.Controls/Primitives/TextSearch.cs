using Avalonia.Controls.Utils;
using Avalonia.Data;
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
        /// This text will be considered during text search in <see cref="SelectingItemsControl"/> (such as <see cref="ComboBox"/>).
        /// This property is usually applied to an item container directly.
        /// </summary>
        public static readonly AttachedProperty<string?> TextProperty
            = AvaloniaProperty.RegisterAttached<Interactive, string?>("Text", typeof(TextSearch));

        /// <summary>
        /// Defines the TextBinding attached property.
        /// The binding will be applied to each item during text search in <see cref="SelectingItemsControl"/> (such as <see cref="ComboBox"/>).
        /// </summary>
        public static readonly AttachedProperty<BindingBase?> TextBindingProperty
            = AvaloniaProperty.RegisterAttached<Interactive, BindingBase?>("TextBinding", typeof(TextSearch));

        // TODO12: Control should be Interactive to match the property definition.
        /// <summary>
        /// Sets the value of the <see cref="TextProperty"/> attached property to a given <see cref="Control"/>.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="text">The search text to set.</param>
        public static void SetText(Control control, string? text)
            => control.SetValue(TextProperty, text);

        // TODO12: Control should be Interactive to match the property definition.
        /// <summary>
        /// Gets the value of the <see cref="TextProperty"/> attached property from a given <see cref="Control"/>.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The search text.</returns>
        public static string? GetText(Control control)
            => control.GetValue(TextProperty);

        /// <summary>
        /// Sets the value of the <see cref="TextBindingProperty"/> attached property to a given <see cref="Interactive"/>.
        /// </summary>
        /// <param name="interactive">The interactive element.</param>
        /// <param name="value">The search text binding to set.</param>
        public static void SetTextBinding(Interactive interactive, BindingBase? value)
            => interactive.SetValue(TextBindingProperty, value);

        /// <summary>
        /// Gets the value of the <see cref="TextBindingProperty"/> attached property from a given <see cref="Interactive"/>.
        /// </summary>
        /// <param name="interactive">The interactive element.</param>
        /// <returns>The search text binding.</returns>
        [AssignBinding]
        public static BindingBase? GetTextBinding(Interactive interactive)
            => interactive.GetValue(TextBindingProperty);

        /// <summary>
        /// <para>Gets the effective text of a given item.</para>
        /// <para>
        ///   This method uses the first non-empty text from the following list:
        ///   <list>
        ///     <item><see cref="TextSearch.TextProperty"/> (if the item is a control)</item>
        ///     <item><see cref="TextSearch.TextBindingProperty"/></item>
        ///     <item><see cref="ItemsControl.DisplayMemberBinding"/></item>
        ///     <item><see cref="IContentControl.Content"/>.<see cref="object.ToString"/> (if the item is a <see cref="IContentControl"/>)</item>
        ///     <item><see cref="object.ToString"/></item>
        ///   </list>
        /// </para>
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="textBindingEvaluator">A <see cref="BindingEvaluator{T}"/> used to get the item's text from a binding.</param>
        /// <returns>The item's text.</returns>
        internal static string GetEffectiveText(object? item, BindingEvaluator<string?>? textBindingEvaluator)
        {
            if (item is null)
                return string.Empty;

            string? text;

            if (item is Interactive interactive)
            {
                text = interactive.GetValue(TextProperty);
                if (!string.IsNullOrEmpty(text))
                    return text;
            }

            if (textBindingEvaluator is not null)
            {
                text = textBindingEvaluator.Evaluate(item);
                if (!string.IsNullOrEmpty(text))
                    return text;
            }

            if (item is IContentControl contentControl)
                return contentControl.Content?.ToString() ?? string.Empty;

            return item.ToString() ?? string.Empty;
        }
    }
}
