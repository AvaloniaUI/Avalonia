using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents a tab in a <see cref="PivotHeader"/>.
    /// </summary>
    [TemplatePart("PART_LayoutRoot", typeof(Border))]
    public class PivotHeaderItem : ListBoxItem
    {
        public static readonly StyledProperty<PivotHeaderPlacement> PivotHeaderPlacementProperty =
            PivotHeader.PivotHeaderPlacementProperty.AddOwner<PivotHeaderItem>();

        public PivotHeaderPlacement PivotHeaderPlacement
        {
            get { return GetValue(PivotHeaderPlacementProperty); }
        }
    }
}
