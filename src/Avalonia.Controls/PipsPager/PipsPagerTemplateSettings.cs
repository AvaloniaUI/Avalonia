using Avalonia.Collections;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Provides calculated values for use with the <see cref="PipsPager"/>'s control theme or template.
    /// </summary>
    public class PipsPagerTemplateSettings : AvaloniaObject
    {
        private AvaloniaList<int> _pips;

        internal PipsPagerTemplateSettings()
        {
            _pips = new AvaloniaList<int>();
        }

        public static readonly DirectProperty<PipsPagerTemplateSettings, AvaloniaList<int>> PipsProperty =
            AvaloniaProperty.RegisterDirect<PipsPagerTemplateSettings, AvaloniaList<int>>(
                nameof(Pips),
                o => o.Pips);

        /// <summary>
        /// Gets the collection of pips indices.
        /// </summary>
        public AvaloniaList<int> Pips
        {
            get => _pips;
        }
    }
}
