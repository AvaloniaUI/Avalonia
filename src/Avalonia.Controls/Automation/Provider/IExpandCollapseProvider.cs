using Avalonia.Controls;

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Exposes methods and properties to support UI Automation client access to controls that
    /// visually expand to display content and collapse to hide content.
    /// </summary>
    public interface IExpandCollapseProvider
    {
        /// <summary>
        /// Gets the state, expanded or collapsed, of the control.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IExpandCollapseProvider.ExpandCollapseState</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.isAccessibilityExpanded</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        ExpandCollapseState ExpandCollapseState { get; }

        /// <summary>
        /// Gets a value indicating whether expanding the element shows a menu of items to the user,
        /// such as drop-down list.
        /// </summary>
        /// <remarks>
        /// Used in OSX to allow <c>accessibilityPerformShowMenu</c> to open expandable controls such as a
        /// <see cref="ComboBox"/>; in macOS, a combo box drop-down is considered a menu.
        /// 
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description>No mapping.</description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>
        ///       When true, <c>NSAccessibilityProtocol.accessibilityPerformShowMenu</c> will cause the
        ///       <see cref="Expand"/> method to be triggered.
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        bool ShowsMenu { get; }

        /// <summary>
        /// Displays all child nodes, controls, or content of the control.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IExpandCollapseProvider.Expand</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>
        ///       Called by setting <c>NSAccessibilityProtocol.setAccessibilityExpanded</c> to 
        ///       true, by calling <c>NSAccessibilityProtocol.accessibilityPerformPress</c>, or
        ///       by calling <c>NSAccessibilityProtocol.accessibilityPerformShowMenu</c>
        ///       when <see cref="ShowsMenu"/> is true.
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        void Expand();

        /// <summary>
        /// Hides all nodes, controls, or content that are descendants of the control.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IExpandCollapseProvider.Collapse</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>
        ///       Called by setting <c>NSAccessibilityProtocol.setAccessibilityExpanded</c> to
        ///       false.
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        void Collapse();
    }
}
