namespace Avalonia.Input
{
    /// <summary>
    /// Provides options to customize the behavior when identifying the next element to focus
    /// during a navigation operation.
    /// </summary>
    public sealed class FindNextElementOptions
    {
        /// <summary>
        /// Gets or sets the currently focused element, used as a starting point of the search.
        /// If null, <see cref="FocusManager.GetFocusedElement()"/> is used.
        /// </summary>
        public IInputElement? FocusedElement { get; init; }

        /// <summary>
        /// Gets or sets the root <see cref="InputElement"/> within which the search for the next
        /// focusable element will be conducted.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property defines the boundary for focus navigation operations. It determines the root element
        /// in the visual tree under which the focusable item search is performed. If not specified, the search
        /// will default to the current scope.
        /// </para>
        /// <para>
        /// This option is only used with <see cref="NavigationDirection.Up"/>, <see cref="NavigationDirection.Down"/>,
        /// <see cref="NavigationDirection.Left"/>, and <see cref="NavigationDirection.Right"/>. It is ignored for other
        /// directions.
        /// </para>
        /// </remarks>
        public InputElement? SearchRoot { get; init; }

        /// <summary>
        /// Gets or sets the rectangular region within the visual hierarchy that will be excluded
        /// from consideration during focus navigation.
        /// </summary>
        /// <remarks>
        /// This option is only used with <see cref="NavigationDirection.Up"/>, <see cref="NavigationDirection.Down"/>,
        /// <see cref="NavigationDirection.Left"/>, and <see cref="NavigationDirection.Right"/>. It is ignored for other
        /// directions.
        /// </remarks>
        public Rect ExclusionRect { get; init; }

        /// <summary>
        /// Gets or sets a rectangular region that serves as a hint for focus navigation.
        /// This property specifies a rectangle, relative to the coordinate system of the search root,
        /// which can be used as a preferred or prioritized target when navigating focus.
        /// It can be null if no specific hint region is provided.
        /// </summary>
        /// <remarks>
        /// This option is only used with <see cref="NavigationDirection.Up"/>, <see cref="NavigationDirection.Down"/>,
        /// <see cref="NavigationDirection.Left"/>, and <see cref="NavigationDirection.Right"/>. It is ignored for other
        /// directions.
        /// </remarks>
        public Rect? FocusHintRectangle { get; init; }

        /// <summary>
        /// Specifies an optional override for the navigation strategy used in XY focus navigation.
        /// This property allows customizing the focus movement behavior when navigating between UI elements.
        /// </summary>
        /// <remarks>
        /// This option is only used with <see cref="NavigationDirection.Up"/>, <see cref="NavigationDirection.Down"/>,
        /// <see cref="NavigationDirection.Left"/>, and <see cref="NavigationDirection.Right"/>. It is ignored for other
        /// directions.
        /// </remarks>
        public XYFocusNavigationStrategy? NavigationStrategyOverride { get; init; }

        /// <summary>
        /// Specifies whether occlusivity (overlapping of elements or obstructions)
        /// should be ignored during focus navigation. When set to <c>true</c>,
        /// the navigation logic disregards obstructions that may block a potential
        /// focus target, allowing elements behind such obstructions to be considered.
        /// </summary>
        /// <remarks>
        /// This option is only used with <see cref="NavigationDirection.Up"/>, <see cref="NavigationDirection.Down"/>,
        /// <see cref="NavigationDirection.Left"/>, and <see cref="NavigationDirection.Right"/>. It is ignored for other
        /// directions.
        /// </remarks>
        public bool IgnoreOcclusivity { get; init; }
    }
}
