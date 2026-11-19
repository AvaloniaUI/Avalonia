namespace Avalonia.Controls
{
    /// <summary>
    /// Determines how <see cref="FlexBasis"/> affects the size of the flex item
    /// </summary>
    public enum FlexBasisKind
    {
        /// <summary>
        /// Uses the measured Width and Height of the <see cref="FlexPanel"/> to determine the initial size of the item.
        /// </summary>
        Auto,

        /// <summary>
        /// The initial size of the item is set to the <see cref="FlexBasis"/> value.
        /// </summary>
        Absolute,

        /// <summary>
        /// Indicates the <see cref="FlexBasis"/> value is a fraction of the container's main-axis size, where 1.0 means 100%.
        /// </summary>
        Relative,
    }
}
