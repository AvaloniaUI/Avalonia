namespace Avalonia.Automation.Provider
{
    public enum ScrollAmount
    {
        LargeDecrement,
        SmallDecrement,
        NoAmount,
        LargeIncrement,
        SmallIncrement,
    }

    /// <summary>
    /// Exposes methods and properties to support access by a UI Automation client to a control
    /// that acts as a scrollable container for a collection of child objects. 
    /// </summary>
    public interface IScrollProvider
    {
        /// <summary>
        /// Gets a value that indicates whether the control can scroll horizontally.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IScrollProvider.HorizontallyScrollable</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        bool HorizontallyScrollable { get; }

        /// <summary>
        /// Gets the current horizontal scroll position.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IScrollProvider.HorizontalScrollPercent</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        double HorizontalScrollPercent { get; }

        /// <summary>
        /// Gets the current horizontal view size.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IScrollProvider.HorizontalViewSize</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        double HorizontalViewSize { get; }

        /// <summary>
        /// Gets a value that indicates whether the control can scroll vertically.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IScrollProvider.VerticallyScrollable</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        bool VerticallyScrollable { get; }

        /// <summary>
        /// Gets the current vertical scroll position.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IScrollProvider.VerticalScrollPercent</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        double VerticalScrollPercent { get; }

        /// <summary>
        /// Gets the vertical view size.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IScrollProvider.VerticalViewSize</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        double VerticalViewSize { get; }

        /// <summary>
        /// Scrolls the visible region of the content area horizontally and vertically.
        /// </summary>
        /// <param name="horizontalAmount">The horizontal increment specific to the control.</param>
        /// <param name="verticalAmount">The vertical increment specific to the control.</param>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IScrollProvider.Scroll</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount);

        /// <summary>
        /// Sets the horizontal and vertical scroll position as a percentage of the total content
        /// area within the control.
        /// </summary>
        /// <param name="horizontalPercent">
        /// The horizontal position as a percentage of the content area's total range.
        /// <see cref="ScrollPatternIdentifiers.NoScroll"/> should be passed in if the control
        /// cannot be scrolled in this direction.
        /// </param>
        /// <param name="verticalPercent">
        /// The vertical position as a percentage of the content area's total range.
        /// <see cref="ScrollPatternIdentifiers.NoScroll"/> should be passed in if the control
        /// cannot be scrolled in this direction.
        /// </param>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IScrollProvider.SetScrollPercent</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        void SetScrollPercent(double horizontalPercent, double verticalPercent);
    }
}
