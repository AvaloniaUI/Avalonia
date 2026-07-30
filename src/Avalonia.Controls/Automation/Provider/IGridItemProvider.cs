using Avalonia.Automation.Peers;

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Exposes methods and properties to support access by a UI Automation client to individual
    /// child elements of a container that implements <see cref="IGridProvider"/>.
    /// </summary>
    public interface IGridItemProvider
    {
        /// <summary>
        /// Gets the zero-based row index of the cell.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IGridItemProvider.Row</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityRowIndexRange</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        int Row { get; }

        /// <summary>
        /// Gets the zero-based column index of the cell.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IGridItemProvider.Column</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityColumnIndexRange</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        int Column { get; }

        /// <summary>
        /// Gets the number of rows the cell spans.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IGridItemProvider.RowSpan</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityRowIndexRange</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        int RowSpan { get; }

        /// <summary>
        /// Gets the number of columns the cell spans.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IGridItemProvider.ColumnSpan</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityColumnIndexRange</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        int ColumnSpan { get; }

        /// <summary>
        /// Gets the automation peer of the grid container that holds the cell.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IGridItemProvider.ContainingGrid</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        AutomationPeer? ContainingGrid { get; }
    }
}
