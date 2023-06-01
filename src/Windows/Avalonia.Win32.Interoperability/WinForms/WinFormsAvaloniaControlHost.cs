using System;
using System.ComponentModel;
using System.Windows.Forms;
using Avalonia.Controls.Embedding;
using Avalonia.Win32.Interop;
using WinFormsControl = System.Windows.Forms.Control;
using AvControl = Avalonia.Controls.Control;

namespace Avalonia.Win32.Interoperability;

/// <summary>
/// An element that allows you to host a Avalonia control on a Windows Forms page.
/// </summary>
[ToolboxItem(true)]
public class WinFormsAvaloniaControlHost : WinFormsControl
{
    private AvControl? _content;
    private EmbeddableControlRoot? _root;

    private IntPtr WindowHandle => _root?.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;

    /// <summary>
    /// Initializes a new instance of the <see cref="WinFormsAvaloniaControlHost"/> class.
    /// </summary>
    public WinFormsAvaloniaControlHost()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
    }

    /// <summary>
    /// Gets or sets the Avalonia control hosted by the <see cref="WinFormsAvaloniaControlHost"/> element.
    /// </summary>
    public AvControl? Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                if (_root is not null)
                {
                    _root.Content = value;
                }
            }
        }
    }

    /// <inheritdoc />
    protected override void OnHandleCreated(EventArgs e)
    {
        _root = new();
        _root.Content = _content;
        _root.Prepare();
        _root.StartRendering();
        _root.GotFocus += RootGotFocus;

        FixPosition();
        
        UnmanagedMethods.SetParent(WindowHandle, Handle);
        base.OnHandleCreated(e);
    }

    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        _root?.StopRendering();
        _root?.Dispose();
        _root = null;
        base.OnHandleDestroyed(e);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _root?.Dispose();
            _root = null;
        }
        base.Dispose(disposing);
    }

    private void RootGotFocus(object? sender, Interactivity.RoutedEventArgs e)
    {
        UnmanagedMethods.SetFocus(WindowHandle);
    }

    /// <inheritdoc />
    protected override void OnGotFocus(EventArgs e)
    {
        var handle = WindowHandle;
        if (handle != default)
            UnmanagedMethods.SetFocus(handle);
    }
        
    private void FixPosition()
    {
        var handle = WindowHandle;
        if (handle != default && Width > 0 && Height > 0)
            UnmanagedMethods.MoveWindow(handle, 0, 0, Width, Height, true);
    }
        
    /// <inheritdoc />
    protected override void OnResize(EventArgs e)
    {
        FixPosition();
        base.OnResize(e);
    }

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {

    }
}
