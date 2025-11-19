using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
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
public class WinFormsAvaloniaControlHost : WinFormsControl, IMessageFilter
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
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
        // EmbeddableControlRoot requires Avalonia to be initialized which is not done by the Windows Forms designer.
        if (!DesignMode)
        {
            _root = new();
            _root.Content = _content;
            _root.Prepare();
            _root.StartRendering();
            _root.GotFocus += RootGotFocus;

            FixPosition();

            UnmanagedMethods.SetParent(WindowHandle, Handle);
        }

        base.OnHandleCreated(e);

        System.Windows.Forms.Application.AddMessageFilter(this);
    }

    /// <inheritdoc />
    protected override void OnHandleDestroyed(EventArgs e)
    {
        System.Windows.Forms.Application.RemoveMessageFilter(this);
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
        if (DesignMode)
        {
            const string message = "Avalonia control is disabled in design mode.";

            using var pen = new Pen(SystemBrushes.ControlDark);
            var outline = ClientSize - new SizeF(pen.Width, pen.Width);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(pen, 0, 0, outline.Width, outline.Height);
            e.Graphics.DrawLine(pen, 0, 0, outline.Width, outline.Height);
            e.Graphics.DrawLine(pen, 0, outline.Height, outline.Width, 0);

            var messageSize = e.Graphics.MeasureString(message, Font, ClientSize);
            var messageLocation = new PointF(ClientSize.Width / 2 - messageSize.Width / 2, ClientSize.Height / 2 - messageSize.Height / 2);
            var messageArea = new RectangleF(messageLocation, messageSize);
            e.Graphics.DrawString(message, Font, SystemBrushes.ControlText, messageArea);
        }
    }

    public bool PreFilterMessage(ref Message m)
    {
        var message = (UnmanagedMethods.WindowsMessage)m.Msg;

        switch (message)
        {
            case UnmanagedMethods.WindowsMessage.WM_LBUTTONDOWN:
            case UnmanagedMethods.WindowsMessage.WM_MBUTTONDOWN:
            case UnmanagedMethods.WindowsMessage.WM_RBUTTONDOWN:
            case UnmanagedMethods.WindowsMessage.WM_NCLBUTTONDOWN:
            case UnmanagedMethods.WindowsMessage.WM_NCMBUTTONDOWN:
            case UnmanagedMethods.WindowsMessage.WM_NCRBUTTONDOWN:
                if (_root?.PlatformImpl is WindowImpl impl && !impl.IsOurWindow(m.HWnd))
                {
                    impl.Deactivated?.Invoke();
                }
                break;
        }

        return false;
    }
}
