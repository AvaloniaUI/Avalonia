using System.Collections.Generic;
using Avalonia.Automation.Peers;

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Specifies whether data in a table should be read primarily by row or by column.
    /// </summary>
    public enum RowOrColumnMajor
    {
        /// <summary>
        /// Data in the table should be read row by row.
        /// </summary>
        RowMajor,

        /// <summary>
        /// Data in the table should be read column by column.
        /// </summary>
        ColumnMajor,

        /// <summary>
        /// The best way to present the data is indeterminate.
        /// </summary>
        Indeterminate,
    }

    /// <summary>
    /// Exposes methods and properties to support access by a UI Automation client to controls
    /// or elements that act as containers whose child elements are organized in rows and
    /// columns and can carry row or column header information. Implementations also implement
    /// <see cref="IGridProvider"/>.
    /// </summary>
    public interface ITableProvider : IGridProvider
    {
        /// <summary>
        /// Gets whether the table's data is best presented row by row or column by column.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ITableProvider.RowOrColumnMajor</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        RowOrColumnMajor RowOrColumnMajor { get; }

        /// <summary>
        /// Retrieves the automation peers of the elements acting as row headers of the table.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ITableProvider.GetRowHeaders</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        IReadOnlyList<AutomationPeer> GetRowHeaders();

        /// <summary>
        /// Retrieves the automation peers of the elements acting as column headers of the table.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ITableProvider.GetColumnHeaders</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityColumnHeaderUIElements</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        IReadOnlyList<AutomationPeer> GetColumnHeaders();
    }
}
