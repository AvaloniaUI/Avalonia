namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Provides calculated values for use with the <see cref="SplitView"/>'s control theme or template.
    /// This class is NOT intended for general use.
    /// </summary>
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

        public double ClosedPaneWidth
        {
            get => GetValue(ClosedPaneWidthProperty);
            internal set => SetValue(ClosedPaneWidthProperty, value);
        }

        public GridLength PaneColumnGridLength
        {
            get => GetValue(PaneColumnGridLengthProperty);
            internal set => SetValue(PaneColumnGridLengthProperty, value);
        }
    }
}
