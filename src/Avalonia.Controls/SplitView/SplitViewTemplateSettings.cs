namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Provides calculated values for use with the <see cref="SplitView"/>'s control theme or template.
    /// </summary>
    /// <remarks>
    /// This class is NOT intended for general use outside of control templates.
    /// </remarks>
    public class SplitViewTemplateSettings : AvaloniaObject
    {
        internal SplitViewTemplateSettings() { }

        public static readonly StyledProperty<double> ClosedPaneWidthProperty =
            AvaloniaProperty.Register<SplitViewTemplateSettings,
                double>(nameof(ClosedPaneWidth),
                0d);

        public static readonly StyledProperty<GridLength> PaneColumnGridLengthProperty =
            AvaloniaProperty.Register<SplitViewTemplateSettings, GridLength>(
                nameof(PaneColumnGridLength));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1032", Justification = "This property is supposed to be a styled readonly property.")]
        public double ClosedPaneWidth
        {
            get => GetValue(ClosedPaneWidthProperty);
            internal set => SetValue(ClosedPaneWidthProperty, value);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1032", Justification = "This property is supposed to be a styled readonly property.")]
        public GridLength PaneColumnGridLength
        {
            get => GetValue(PaneColumnGridLengthProperty);
            internal set => SetValue(PaneColumnGridLengthProperty, value);
        }
    }
}
