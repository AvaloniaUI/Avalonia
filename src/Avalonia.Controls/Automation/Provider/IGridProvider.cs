using Avalonia.Automation.Peers;

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Exposes methods and properties to support access by a UI Automation client to controls
    /// or elements that act as containers for a collection of child elements organized in a
    /// two-dimensional logical coordinate system that can be traversed by row and column.
    /// </summary>
    public interface IGridProvider
    {
        /// <summary>
        /// Gets the total number of rows in the grid.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IGridProvider.RowCount</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityRows</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        int RowCount { get; }

        /// <summary>
        /// Gets the total number of columns in the grid.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IGridProvider.ColumnCount</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityColumns</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        int ColumnCount { get; }

        /// <summary>
        /// Retrieves the element at the specified cell. A cell that spans multiple coordinates
        /// is returned for each coordinate it covers.
        /// </summary>
        /// <param name="row">The zero-based row index of the cell.</param>
        /// <param name="column">The zero-based column index of the cell.</param>
        /// <returns>
        /// The automation peer of the element at the cell, or null when the coordinates are
        /// outside the grid or the cell is empty.
        /// </returns>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IGridProvider.GetItem</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        AutomationPeer? GetItem(int row, int column);
    }
}
