using System;
using Avalonia.Controls;
using Avalonia.Platform;

namespace IntegrationTestApp.Embedding;

internal class NativeTextBox : NativeControlHost
{
    private ContextMenu? _contextMenu;
    private INativeTextBoxImpl? _impl;
    private TextBlock _tipTextBlock;
    private string _initialText = string.Empty;

    public NativeTextBox()
    {
        _tipTextBlock = new TextBlock
        {
            Text = "Avalonia ToolTip",
            Name = "NativeTextBoxToolTip",
        };

        ToolTip.SetTip(this, _tipTextBlock);
        ToolTip.SetShowDelay(this, 1000);
        ToolTip.SetServiceEnabled(this, false);
    }

    public string Text
    {
        get => _impl?.Text ?? _initialText;
        set
        {
            if (_impl is not null)
                _impl.Text = value;
            else
                _initialText = value;
        }
    }

    public static INativeTextBoxFactory? Factory { get; set; }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (Factory is null)
            return base.CreateNativeControlCore(parent);

        _impl = Factory.CreateControl(parent);
        _impl.Text = _initialText;
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
        if (_contextMenu is null)
        {
            var menuItem = new MenuItem { Header = "Custom Menu Item" };
            menuItem.Click += (s, e) => _impl!.Text = "Context menu item clicked";

            _contextMenu = new ContextMenu
            {
                Name = "NativeTextBoxContextMenu",
                Items = { menuItem }
            };
        }

        ToolTip.SetIsOpen(this, false);
        _contextMenu.Open(this);
    }

    private void OnHovered(object? sender, EventArgs e)
    {
        ToolTip.SetIsOpen(this, true);
    }

    private void OnPointerExited(object? sender, EventArgs e)
    {
        ToolTip.SetIsOpen(this, false);
    }
}
