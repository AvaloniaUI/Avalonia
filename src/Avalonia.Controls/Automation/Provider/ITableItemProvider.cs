using System.Collections.Generic;
using Avalonia.Automation.Peers;

namespace Avalonia.Automation.Provider
{
    /// <summary>
    /// Exposes methods and properties to support access by a UI Automation client to child
    /// elements of a container that implements <see cref="ITableProvider"/>. Implementations
    /// also implement <see cref="IGridItemProvider"/>.
    /// </summary>
    public interface ITableItemProvider : IGridItemProvider
    {
        /// <summary>
        /// Retrieves the automation peers of the row headers associated with the cell.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ITableItemProvider.GetRowHeaderItems</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        IReadOnlyList<AutomationPeer> GetRowHeaderItems();

        /// <summary>
        /// Retrieves the automation peers of the column headers associated with the cell.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>ITableItemProvider.GetColumnHeaderItems</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        IReadOnlyList<AutomationPeer> GetColumnHeaderItems();
    }
}
