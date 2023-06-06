using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    internal class TrayIconImpl : ITrayIconImpl
    {
        private static readonly IntPtr s_emptyIcon = new System.Drawing.Bitmap(32, 32).GetHicon();
        private readonly int _uniqueId;
        private static int s_nextUniqueId;
        private static nint s_taskBarMonitor;

        private bool _iconAdded;
        private IconImpl? _iconImpl;
        private bool _iconStale;
        private Icon? _icon;
        private string? _tooltipText;
        private readonly Win32NativeToManagedMenuExporter _exporter;
        private static readonly Dictionary<int, TrayIconImpl> s_trayIcons = new();
        private bool _disposedValue;
        private static readonly uint WM_TASKBARCREATED = RegisterWindowMessage("TaskbarCreated");

        public TrayIconImpl()
        {
            FindTaskBarMonitor();

            _exporter = new Win32NativeToManagedMenuExporter();

            _uniqueId = ++s_nextUniqueId;

            s_trayIcons.Add(_uniqueId, this);
        }

        public Action? OnClicked { get; set; }

        public INativeMenuExporter MenuExporter => _exporter;

        internal static void ProcWnd(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case (uint)CustomWindowsMessage.WM_TRAYMOUSE:
                    if (s_trayIcons.TryGetValue(wParam.ToInt32(), out var value))
                    {
                        value.WndProc(hWnd, msg, wParam, lParam);
                    }
                    break;
                case (uint)WindowsMessage.WM_DISPLAYCHANGE:
                    FindTaskBarMonitor();
                    foreach (var tray in s_trayIcons.Values)
                    {
                        if (tray._iconAdded)
                        {
                            tray._iconStale = true;
                            tray.UpdateIcon();
                        }
                    }
                    break;
                default:
                    if (msg == WM_TASKBARCREATED)
                    {
                        FindTaskBarMonitor();
                        foreach (var tray in s_trayIcons.Values)
                        {
                            if (tray._iconAdded)
                            {
                                tray.UpdateIcon(true);
                                tray.UpdateIcon();
                            }
                        }
                    }
                    break;
            }
        }

        /// <inheritdoc />
        public void SetIcon(IWindowIconImpl? icon)
        {
            _iconImpl = icon as IconImpl;
            _iconStale = true;
            UpdateIcon();
        }

        /// <inheritdoc />
        public void SetIsVisible(bool visible)
        {
            UpdateIcon(!visible);
        }

        /// <inheritdoc />
        public void SetToolTipText(string? text)
        {
            _tooltipText = text;
            UpdateIcon(!_iconAdded);
        }

        private static void FindTaskBarMonitor()
        {
            var taskBarData = new APPBARDATA();
            if (SHAppBarMessage(AppBarMessage.ABM_GETTASKBARPOS, ref taskBarData) != 0)
            {
                s_taskBarMonitor = MonitorFromPoint(new() { X = taskBarData.rc.left, Y = taskBarData.rc.top }, MONITOR.MONITOR_DEFAULTTOPRIMARY);
            }
        }

        private void UpdateIcon(bool remove = false)
        {
            Icon? newIcon = null;
            if (_iconStale && _iconImpl is not null)
            {
                var scaling = 1.0;
                if ((HRESULT)GetDpiForMonitor(s_taskBarMonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out var dpiY) == HRESULT.S_OK)
                {
                    Debug.Assert(dpiX == dpiY);
                    scaling = dpiX / 96.0;
                }

                newIcon = _iconImpl.LoadSmallIcon(scaling);
            }

            var iconData = new NOTIFYICONDATA
            {
                hWnd = Win32Platform.Instance.Handle,
                uID = _uniqueId
            };

            if (!remove)
            {
                iconData.uFlags = NIF.TIP | NIF.MESSAGE | NIF.ICON;
                iconData.uCallbackMessage = (int)CustomWindowsMessage.WM_TRAYMOUSE;
                iconData.hIcon = (_iconStale ? newIcon : _icon)?.Handle ?? s_emptyIcon;
                iconData.szTip = _tooltipText ?? "";

                if (!_iconAdded)
                {
                    Shell_NotifyIcon(NIM.ADD, iconData);
                    _iconAdded = true;
                }
                else
                {
                    Shell_NotifyIcon(NIM.MODIFY, iconData);
                }
            }
            else
            {
                iconData.uFlags = 0;
                Shell_NotifyIcon(NIM.DELETE, iconData);
                _iconAdded = false;
            }

            if (_iconStale)
            {
                _icon?.Dispose();
                _icon = newIcon;
                _iconStale = false;
            }
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == (uint)CustomWindowsMessage.WM_TRAYMOUSE)
            {
                // Determine the type of message and call the matching event handlers
                switch (lParam.ToInt32())
                {
                    case (int)WindowsMessage.WM_LBUTTONUP:
                        OnClicked?.Invoke();
                        break;

                    case (int)WindowsMessage.WM_RBUTTONUP:
                        OnRightClicked();
                        break;
                }

                return IntPtr.Zero;
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void OnRightClicked()
        {
            var menuItems = _exporter.GetMenu();
            if (null == menuItems || menuItems.Count == 0)
            {
                return;
            }

            var _trayMenu = new TrayPopupRoot()
            {
                SystemDecorations = SystemDecorations.None,
                SizeToContent = SizeToContent.WidthAndHeight,
                Background = null,
                TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
                Content = new TrayIconMenuFlyoutPresenter()
                {
                    ItemsSource = menuItems
                }
            };

            GetCursorPos(out POINT pt);

            _trayMenu.Position = new PixelPoint(pt.X, pt.Y);

            _trayMenu.Show();
        }

        /// <summary>
        /// Custom Win32 window messages for the NotifyIcon
        /// </summary>
        private enum CustomWindowsMessage : uint
        {
            WM_TRAYICON = WindowsMessage.WM_APP + 1024,
            WM_TRAYMOUSE = WindowsMessage.WM_USER + 1024,
        }

        private class TrayIconMenuFlyoutPresenter : MenuFlyoutPresenter
        {
            protected override Type StyleKeyOverride => typeof(MenuFlyoutPresenter);

            public override void Close()
            {
                // DefaultMenuInteractionHandler calls this
                var host = this.FindLogicalAncestorOfType<TrayPopupRoot>();
                if (host != null)
                {
                    SelectedIndex = -1;
                    host.Close();
                }
            }
        }

        private class TrayPopupRoot : Window
        {
            private readonly ManagedPopupPositioner _positioner;

            public TrayPopupRoot()
            {
                _positioner = new ManagedPopupPositioner(new TrayIconManagedPopupPositionerPopupImplHelper(MoveResize));
                Topmost = true;

                Deactivated += TrayPopupRoot_Deactivated;

                ShowInTaskbar = false;

                ShowActivated = true;
            }

            private void TrayPopupRoot_Deactivated(object? sender, EventArgs e)
            {
                Close();
            }

            private void MoveResize(PixelPoint position, Size size, double scaling)
            {
                if (PlatformImpl is { } platformImpl)
                {
                    platformImpl.Move(position);
                    platformImpl.Resize(size, WindowResizeReason.Layout);
                }
            }

            protected override void ArrangeCore(Rect finalRect)
            {
                base.ArrangeCore(finalRect);

                _positioner.Update(new PopupPositionerParameters
                {
                    Anchor = PopupAnchor.TopLeft,
                    Gravity = PopupGravity.BottomRight,
                    AnchorRectangle = new Rect(Position.ToPoint(Screens.Primary?.Scaling ?? 1.0), new Size(1, 1)),
                    Size = finalRect.Size,
                    ConstraintAdjustment = PopupPositionerConstraintAdjustment.FlipX | PopupPositionerConstraintAdjustment.FlipY,
                });
            }

            private class TrayIconManagedPopupPositionerPopupImplHelper : IManagedPopupPositionerPopup
            {
                private readonly Action<PixelPoint, Size, double> _moveResize;
                private readonly Window _hiddenWindow;

                public TrayIconManagedPopupPositionerPopupImplHelper(Action<PixelPoint, Size, double> moveResize)
                {
                    _moveResize = moveResize;
                    _hiddenWindow = new Window();
                }

                public IReadOnlyList<ManagedPopupPositionerScreenInfo> Screens =>
                    _hiddenWindow.Screens.All
                        .Select(s => new ManagedPopupPositionerScreenInfo(s.Bounds.ToRect(1), s.Bounds.ToRect(1)))
                        .ToArray();

                public Rect ParentClientAreaScreenGeometry
                {
                    get
                    {
                        if (_hiddenWindow.Screens.Primary is { } screen)
                        {
                            var point = screen.Bounds.TopLeft;
                            var size = screen.Bounds.Size;
                            return new Rect(point.X, point.Y, size.Width * screen.Scaling, size.Height * screen.Scaling);
                        }
                        return default;
                    }
                }

                public void MoveAndResize(Point devicePoint, Size virtualSize)
                {
                    _moveResize(new PixelPoint((int)devicePoint.X, (int)devicePoint.Y), virtualSize, Scaling);
                }

                public double Scaling => _hiddenWindow.Screens.Primary?.Scaling ?? 1.0;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                UpdateIcon(true);

                _disposedValue = true;
            }
        }

        ~TrayIconImpl()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
