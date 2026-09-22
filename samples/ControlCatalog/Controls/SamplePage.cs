using System;
using Avalonia;
using Avalonia.Controls;

namespace ControlCatalog.Controls
{
    /// <summary>
    /// The standard catalog page: a scrolling page whose header is shown by the shell navigation bar and whose
    /// <see cref="Description"/> is rendered above the content. Pages stack <see cref="SampleSection"/>s inside it.
    /// A page whose content scrolls itself sets <c>ScrollViewer.VerticalScrollBarVisibility="Disabled"</c>, so the
    /// content gets the page height instead of growing to its full extent.
    /// </summary>
    public class SamplePage : ContentPage
    {
        public static readonly StyledProperty<string?> DescriptionProperty =
            AvaloniaProperty.Register<SamplePage, string?>(nameof(Description));

        /// <summary>
        /// One or two sentences saying what the control is for. Shown under the page title.
        /// </summary>
        public string? Description
        {
            get => GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        // Pages derive from this class, and a theme is looked up by the exact type.
        protected override Type StyleKeyOverride => typeof(SamplePage);
    }
}
