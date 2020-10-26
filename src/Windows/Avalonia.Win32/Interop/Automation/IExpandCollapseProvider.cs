using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("76d12d7e-b227-4417-9ce2-42642ffa896a")]
    internal enum ExpandCollapseState
    {
        Collapsed,
        Expanded,
        PartiallyExpanded,
        LeafNode
    }

    [ComVisible(true)]
    [Guid("d847d3a5-cab0-4a98-8c32-ecb45c59ad24")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IExpandCollapseProvider
    {
        void Expand();
        void Collapse();
        ExpandCollapseState ExpandCollapseState { get; }
    }
}
