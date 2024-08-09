using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;

namespace IntegrationTestApp.Embedding;

internal class NativeTextBox : NativeControlHost
{
    private ContextMenu? _contextMenu;
    private INativeTextBoxImpl? _impl;

    public NativeTextBox()
    {
        ToolTip.SetTip(this, "Avalonia ToolTip");
        ToolTip.SetShowDelay(this, 1000);
        ToolTip.SetServiceEnabled(this, false);
    }

    public static INativeTextBoxFactory? Factory { get; set; }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (Factory is null)
            return base.CreateNativeControlCore(parent);

        _impl = Factory.CreateControl(parent);
        _impl.ContextMenuRequested += OnContextMenuRequested;
        _impl.Hovered += OnHovered;
        _impl.PointerExited += OnPointerExited;
        return _impl.Handle;
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        base.DestroyNativeControlCore(control);
    }

    private void OnContextMenuRequested(object? sender, EventArgs e)
    {
        _contextMenu ??= new ContextMenu
        {
            Items = { new MenuItem { Header = "Custom Menu Item" } }
        };

        ToolTip.SetIsOpen(this, false);
        _contextMenu.Open(this);
    }

    private void OnHovered(object? sender, EventArgs e)
    {
        if (_contextMenu?.IsOpen != true)
            ToolTip.SetIsOpen(this, true);
    }

    private void OnPointerExited(object? sender, EventArgs e)
    {
        ToolTip.SetIsOpen(this, false);
    }
}
