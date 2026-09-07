using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Peers;
using AAP = Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Automation.Interop;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode :
        UIA.IGridProvider,
        UIA.IGridItemProvider,
        UIA.ITableProvider,
        UIA.ITableItemProvider
    {
        UIA.IRawElementProviderSimple? UIA.IGridProvider.GetItem(int row, int column)
        {
            var peer = InvokeSync<AAP.IGridProvider, AutomationPeer?>(p => p.GetItem(row, column));
            return peer is null ? null : GetOrCreate(peer);
        }

        int UIA.IGridProvider.GetRowCount() => InvokeSync<AAP.IGridProvider, int>(p => p.RowCount);

        int UIA.IGridProvider.GetColumnCount() => InvokeSync<AAP.IGridProvider, int>(p => p.ColumnCount);

        int UIA.IGridItemProvider.GetRow() => InvokeSync<AAP.IGridItemProvider, int>(p => p.Row);

        int UIA.IGridItemProvider.GetColumn() => InvokeSync<AAP.IGridItemProvider, int>(p => p.Column);

        int UIA.IGridItemProvider.GetRowSpan() => InvokeSync<AAP.IGridItemProvider, int>(p => p.RowSpan);

        int UIA.IGridItemProvider.GetColumnSpan() => InvokeSync<AAP.IGridItemProvider, int>(p => p.ColumnSpan);

        UIA.IRawElementProviderSimple UIA.IGridItemProvider.GetContainingGrid()
        {
            var peer = InvokeSync<AAP.IGridItemProvider, AutomationPeer?>(p => p.ContainingGrid);
            return peer is null ? null! : GetOrCreate(peer)!;
        }

        UIA.IRawElementProviderSimple[] UIA.ITableProvider.GetRowHeaders()
            => ToNodeArray(InvokeSync<AAP.ITableProvider, IReadOnlyList<AutomationPeer>>(p => p.GetRowHeaders()));

        UIA.IRawElementProviderSimple[] UIA.ITableProvider.GetColumnHeaders()
            => ToNodeArray(InvokeSync<AAP.ITableProvider, IReadOnlyList<AutomationPeer>>(p => p.GetColumnHeaders()));

        UIA.RowOrColumnMajor UIA.ITableProvider.GetRowOrColumnMajor()
            => (UIA.RowOrColumnMajor)(int)InvokeSync<AAP.ITableProvider, AAP.RowOrColumnMajor>(
                p => p.RowOrColumnMajor);

        UIA.IRawElementProviderSimple[] UIA.ITableItemProvider.GetRowHeaderItems()
            => ToNodeArray(InvokeSync<AAP.ITableItemProvider, IReadOnlyList<AutomationPeer>>(p => p.GetRowHeaderItems()));

        UIA.IRawElementProviderSimple[] UIA.ITableItemProvider.GetColumnHeaderItems()
            => ToNodeArray(InvokeSync<AAP.ITableItemProvider, IReadOnlyList<AutomationPeer>>(p => p.GetColumnHeaderItems()));

        private static UIA.IRawElementProviderSimple[] ToNodeArray(IReadOnlyList<AutomationPeer> peers)
            => peers.Select(peer => (UIA.IRawElementProviderSimple)GetOrCreate(peer)!).ToArray();
    }
}
