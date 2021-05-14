namespace Avalonia.Controls
{
    public enum FlyoutPlacementMode
    {
        /// <summary>
        /// Preferred location is above the target element
        /// </summary>
        Top = 0,

        /// <summary>
        /// Preferred location is below the target element
        /// </summary>
        Bottom = 1,

        /// <summary>
        /// Preferred location is to the left of the target element
        /// </summary>
        Left = 2,

        /// <summary>
        /// Preferred location is to the right of the target element
        /// </summary>
        Right = 3,

        //TODO
        // <summary>
        // Preferred location is centered on the screen
        // </summary>
        //Full = 4,

        /// <summary>
        /// Preferred location is above the target element, with the left edge of the flyout
        /// aligned with the left edge of the target element
        /// </summary>
        TopEdgeAlignedLeft = 5,

        /// <summary>
        /// Preferred location is above the target element, with the right edge of flyout aligned with right edge of the target element.
        /// </summary>
        TopEdgeAlignedRight = 6,

        /// <summary>
        /// Preferred location is below the target element, with the left edge of flyout aligned with left edge of the target element.
        /// </summary>
        BottomEdgeAlignedLeft = 7,

        /// <summary>
        /// Preferred location is below the target element, with the right edge of flyout aligned with right edge of the target element.
        /// </summary>
        BottomEdgeAlignedRight = 8,

        /// <summary>
        /// Preferred location is to the left of the target element, with the top edge of flyout aligned with top edge of the target element.
        /// </summary>
        LeftEdgeAlignedTop = 9,

        /// <summary>
        /// Preferred location is to the left of the target element, with the bottom edge of flyout aligned with bottom edge of the target element.
        /// </summary>
        LeftEdgeAlignedBottom = 10,

        /// <summary>
        /// Preferred location is to the right of the target element, with the top edge of flyout aligned with top edge of the target element.
        /// </summary>
        RightEdgeAlignedTop = 11,

        /// <summary>
        /// Preferred location is to the right of the target element, with the bottom edge of flyout aligned with bottom edge of the target element.
        /// </summary>
        RightEdgeAlignedBottom = 12,

        /// <summary>
        /// Preferred location is determined automatically.
        /// </summary>
        Auto = 13
    }
}
