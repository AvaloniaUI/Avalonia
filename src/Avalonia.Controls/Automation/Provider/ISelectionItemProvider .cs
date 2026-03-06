namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Exposes methods and properties to support access by a UI Automation client to individual,
    /// selectable child controls of containers that implement <see cref="ISelectionProvider"/>.
    /// </summary>
    public interface ISelectionItemProvider
    {
        /// <summary>
        /// Gets a value that indicates whether an item is selected.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ISelectionItemProvider.IsSelected</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.isAccessibilitySelected</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        bool IsSelected { get; }

        /// <summary>
        /// Gets the UI Automation provider that implements <see cref="ISelectionProvider"/> and
        /// acts as the container for the calling object.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ISelectionItemProvider.SelectionContainer</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        ISelectionProvider? SelectionContainer { get; }

        /// <summary>
        /// Adds the current element to the collection of selected items.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ISelectionItemProvider.AddToSelection</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>
        ///       <c>NSAccessibilityProtocol.accessibilityPerformPick</c> (not implemented).
        ///       <c>NSAccessibilityProtocol.setAccessibilitySelected</c> (not implemented).
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        void AddToSelection();

        /// <summary>
        /// Removes the current element from the collection of selected items.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ISelectionItemProvider.RemoveFromSelection</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>
        ///       <c>NSAccessibilityProtocol.setAccessibilitySelected</c> (not implemented).
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        void RemoveFromSelection();

        /// <summary>
        /// Clears any existing selection and then selects the current element.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ISelectionItemProvider.Select</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        void Select();
    }
}
