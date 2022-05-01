namespace Avalonia.Input
{
    /// <summary>
    /// Provides options to help identify the next element that can programmatically
    /// receive focus
    /// </summary>
    public class FindNextElementOptions
    {
        /// <summary>
        /// Gets or sets the focus navigation strategy used to identify the best candidate
        /// element to receive focus
        /// </summary>
        /// <remarks>
        /// In UWP, a separate enum 'XYFocusNavigationStrategyOverride' exists, but holds same
        /// values as XYFocusNavigationStrategy - so just using that one
        /// </remarks>
        public XYFocusNavigationStrategy XYFocusNavigationStrategyOverride { get; set; }

        /// <summary>
        /// Gets or sets the object that must be the root from which to identify the next
        /// focus candidate to receive navigation focus
        /// </summary>
        public IInputElement? SearchRoot { get; set; }

        /// <summary>
        /// Gets or sets a bounding rectangle used to identify the focus candidate most
        /// likely to receive navigation focus
        /// </summary>
        /// <remarks>
        /// The coordinates of the Rect must be in TopLevel global coordinates. Use 
        /// IVisual.TransformToVisual(IVisual to) to convert if necessary.
        /// </remarks>
        public Rect HintRect { get; set; }

        /// <summary>
        /// Gets or sets a bounding rectangle where all overlapping navigation candidates
        /// are excluded from navigation focus
        /// </summary>
        /// <remarks>
        /// The coordinates of the Rect must be in TopLevel global coordinates. Use 
        /// IVisual.TransformToVisual(IVisual to) to convert if necessary.
        /// </remarks>
        public Rect ExclusionRect { get; set; }
    }
}
