using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the alignment mode along the cross-axis of <see cref="FlexPanel"/> child items.
    /// </summary>
    [SuppressMessage("Naming", "CA1717:Only FlagsAttribute enums should have plural names")]
    public enum AlignItems
    {
        /// <summary>
        /// Items are aligned to the cross-axis start margin edge of the line.
        /// </summary>
        FlexStart,
        
        /// <summary>
        /// Items are aligned to the cross-axis end margin edge of the line.
        /// </summary>
        FlexEnd,
        
        /// <summary>
        /// Items are aligned to the cross-axis center of the line.
        /// </summary>
        /// <remarks>
        /// If the cross size of the line is less than that of the child item,
        /// it will overflow equally in both directions.
        /// </remarks>
        Center,
        
        /// <summary>
        /// Items are stretched to fill the cross size of the line.
        /// </summary>
        /// <remarks>
        /// This is the default value.
        /// </remarks>
        Stretch
    }
}
