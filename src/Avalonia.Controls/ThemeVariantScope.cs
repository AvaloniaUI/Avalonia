using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Decorator control that isolates controls subtree with locally defined <see cref="ThemeVariant"/>.
    /// </summary>
    public class ThemeVariantScope : Decorator
    {
        /// <summary>
        /// Defines the <see cref="Control.ActualThemeVariant"/> property.
        /// </summary>
        public static readonly StyledProperty<ThemeVariant> ActualThemeVariantProperty =
            AvaloniaProperty.Register<ThemeVariantScope, ThemeVariant>(
                nameof(ActualThemeVariant),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="RequestedThemeVariant"/> property.
        /// </summary>
        public static readonly StyledProperty<ThemeVariant?> RequestedThemeVariantProperty =
            AvaloniaProperty.Register<ThemeVariantScope, ThemeVariant?>(
                nameof(RequestedThemeVariant),
                defaultValue: ThemeVariant.Default);

        /// <summary>
        /// Gets or sets the UI theme variant that is used by the control (and its child elements) for resource determination.
        /// The UI theme you specify with ThemeVariant can override the app-level ThemeVariant.
        /// </summary>
        /// <remarks>
        /// Setting RequestedThemeVariant to <see cref="ThemeVariant.Default"/> will apply parent's actual theme variant on the current scope.
        /// </remarks>
        public ThemeVariant? RequestedThemeVariant
        {
            get => GetValue(RequestedThemeVariantProperty);
            set => SetValue(RequestedThemeVariantProperty, value);
        }
    }
}
