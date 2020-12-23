namespace Avalonia.Controls.Primitives
{
    /// <summary>    
    /// Specifies the type of Avalonia.Controls.Primitives.ScrollBar.Scroll event
    /// that occurred.
    /// </summary>
    public enum ScrollEventType
    {
        /// <summary>    
        /// Specifies that the Avalonia.Controls.Primitives.Thumb moved a specified
        /// distance, as determined by the value of Avalonia.Controls.Primitives.RangeBase.SmallChange.
        /// The Avalonia.Controls.Primitives.Thumb moved to the left for a horizontal
        /// Avalonia.Controls.Primitives.ScrollBar or upward for a vertical Avalonia.Controls.Primitives.ScrollBar.
        /// </summary>
        SmallDecrement = 0,
        /// <summary>    
        /// Specifies that the Avalonia.Controls.Primitives.Thumb moved a specified
        /// distance, as determined by the value of Avalonia.Controls.Primitives.RangeBase.SmallChange.
        /// The Avalonia.Controls.Primitives.Thumb moved to the right for a horizontal
        /// Avalonia.Controls.Primitives.ScrollBar or downward for a vertical Avalonia.Controls.Primitives.ScrollBar.
        /// </summary>
        SmallIncrement = 1,
        /// <summary>    
        /// Specifies that the Avalonia.Controls.Primitives.Thumb moved a specified
        /// distance, as determined by the value of Avalonia.Controls.Primitives.RangeBase.LargeChange.
        /// The Avalonia.Controls.Primitives.Thumb moved to the left for a horizontal
        /// Avalonia.Controls.Primitives.ScrollBar or upward for a vertical Avalonia.Controls.Primitives.ScrollBar.
        /// </summary>
        LargeDecrement = 2,
        /// <summary>    
        /// Specifies that the Avalonia.Controls.Primitives.Thumb moved a specified
        /// distance, as determined by the value of Avalonia.Controls.Primitives.RangeBase.LargeChange.
        /// The Avalonia.Controls.Primitives.Thumb moved to the right for a horizontal
        /// Avalonia.Controls.Primitives.ScrollBar or downward for a vertical Avalonia.Controls.Primitives.ScrollBar.
        /// </summary>
        LargeIncrement = 3,
        /// <summary>    
        /// The Avalonia.Controls.Primitives.Thumb was dragged and caused a Avalonia.UIElement.MouseMove
        /// event. A Avalonia.Controls.Primitives.ScrollBar.Scroll event of this Avalonia.Controls.Primitives.ScrollEventType
        /// may occur more than one time when the Avalonia.Controls.Primitives.Thumb
        /// is dragged in the Avalonia.Controls.Primitives.ScrollBar.
        /// </summary>
        ThumbTrack = 4,
        /// <summary>    
        /// Specifies that the Avalonia.Controls.Primitives.Thumb was dragged to a
        /// new position and is now no longer being dragged by the user.
        /// </summary>
        EndScroll = 5
    }
}
