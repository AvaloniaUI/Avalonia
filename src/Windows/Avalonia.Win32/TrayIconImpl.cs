using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;

#nullable enable

namespace Avalonia.Win32
{
    public class TrayIconImpl : ITrayIconImpl
    {
        private readonly int _uniqueId = 0;
        private static int _nextUniqueId = 0;
        private bool _iconAdded;
        private IconImpl? _icon;
        private string? _tooltipText;
        private readonly Win32NativeToManagedMenuExporter _exporter;
        private static Dictionary<int, TrayIconImpl> s_trayIcons = new Dictionary<int, TrayIconImpl>();
        private bool _disposedValue;

        public TrayIconImpl()
        {
            _exporter = new Win32NativeToManagedMenuExporter();

            _uniqueId = ++_nextUniqueId;

            s_trayIcons.Add(_uniqueId, this);
        }

        public Action? OnClicked { get; set; }

        public INativeMenuExporter MenuExporter => _exporter;

        internal static void ProcWnd(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == (int)CustomWindowsMessage.WM_TRAYMOUSE && s_trayIcons.ContainsKey(wParam.ToInt32()))
            {
                s_trayIcons[wParam.ToInt32()].WndProc(hWnd, msg, wParam, lParam);
            }
        }

        public void SetIcon(IWindowIconImpl? icon)
        {
            _icon = icon as IconImpl;
            UpdateIcon();
        }

        public void SetIsVisible(bool visible)
        {
            UpdateIcon(!visible);
        }

        public void SetToolTipText(string? text)
        {
            _tooltipText = text;
            UpdateIcon(!_iconAdded);
        }

        private void UpdateIcon(bool remove = false)
        {
            var iconData = new NOTIFYICONDATA()
            {
                hWnd = Win32Platform.Instance.Handle,
                uID = _uniqueId,
                uFlags = NIF.TIP | NIF.MESSAGE,
                uCallbackMessage = (int)CustomWindowsMessage.WM_TRAYMOUSE,
                hIcon = _icon?.HIcon ?? new IconImpl(new System.Drawing.Bitmap(32, 32)).HIcon,
                szTip = _tooltipText ?? ""
            };

            if (!remove)
            {
                iconData.uFlags |= NIF.ICON;

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
                Shell_NotifyIcon(NIM.DELETE, iconData);
                _iconAdded = false;
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

                    default:
                        break;
                }

                return IntPtr.Zero;
            }
            else
            {
                return DefWindowProc(hWnd, msg, wParam, lParam);
            }
        }

        private void OnRightClicked()
        {
            var _trayMenu = new TrayPopupRoot()
            {
                SystemDecorations = SystemDecorations.None,
                SizeToContent = SizeToContent.WidthAndHeight,
                Background = null,
                TransparencyLevelHint = WindowTransparencyLevel.Transparent,
                Content = new TrayIconMenuFlyoutPresenter()
                {
                    Items = _exporter.GetMenu()
                }
            };

            GetCursorPos(out POINT pt);

            _trayMenu.Position = new PixelPoint(pt.X, pt.Y);

            _trayMenu.Show();
        }

        /// <summary>
        /// Custom Win32 window messages for the NotifyIcon
        /// </summary>
        enum CustomWindowsMessage : uint
        {
            WM_TRAYICON = WindowsMessage.WM_APP + 1024,
            WM_TRAYMOUSE = WindowsMessage.WM_USER + 1024
        }

        class TrayIconMenuFlyoutPresenter : MenuFlyoutPresenter, IStyleable
        {
            Type IStyleable.StyleKey => typeof(MenuFlyoutPresenter);

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

        class TrayPopupRoot : Window
        {
            private ManagedPopupPositioner _positioner;

            public TrayPopupRoot()
            {
                _positioner = new ManagedPopupPositioner(new TrayIconManagedPopupPositionerPopupImplHelper(MoveResize));
                Topmost = true;

                Deactivated += TrayPopupRoot_Deactivated;

                ShowInTaskbar = false;
            }

            private void TrayPopupRoot_Deactivated(object sender, EventArgs e)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Close();
                });
            }

            private void MoveResize(PixelPoint position, Size size, double scaling)
            {
                PlatformImpl.Move(position);
                PlatformImpl.Resize(size, PlatformResizeReason.Layout);
            }

            private void TrayPopupRoot_LostFocus(object sender, Interactivity.RoutedEventArgs e)
            {
                Close();
            }

            protected override void ArrangeCore(Rect finalRect)
            {
                base.ArrangeCore(finalRect);

                _positioner.Update(new PopupPositionerParameters
                {
                    Anchor = PopupAnchor.TopLeft,
                    Gravity = PopupGravity.BottomRight,
                    AnchorRectangle = new Rect(Position.ToPoint(1) / Screens.Primary.PixelDensity, new Size(1, 1)),
                    Size = finalRect.Size,
                    ConstraintAdjustment = PopupPositionerConstraintAdjustment.FlipX | PopupPositionerConstraintAdjustment.FlipY,
                });
            }

            class TrayIconManagedPopupPositionerPopupImplHelper : IManagedPopupPositionerPopup
            {
                public delegate void MoveResizeDelegate(PixelPoint position, Size size, double scaling);
                private readonly MoveResizeDelegate _moveResize;
                private Window _hiddenWindow;

                public TrayIconManagedPopupPositionerPopupImplHelper(MoveResizeDelegate moveResize)
                {
                    _moveResize = moveResize;
                    _hiddenWindow = new Window();
                }

                public IReadOnlyList<ManagedPopupPositionerScreenInfo> Screens =>
                _hiddenWindow.Screens.All.Select(s => new ManagedPopupPositionerScreenInfo(
                    s.Bounds.ToRect(1), s.Bounds.ToRect(1))).ToList();

                public Rect ParentClientAreaScreenGeometry
                {
                    get
                    {
                        var point = _hiddenWindow.Screens.Primary.Bounds.TopLeft;
                        var size = _hiddenWindow.Screens.Primary.Bounds.Size;
                        return new Rect(point.X, point.Y, size.Width * _hiddenWindow.Screens.Primary.PixelDensity, size.Height * _hiddenWindow.Screens.Primary.PixelDensity);
                    }
                }

                public void MoveAndResize(Point devicePoint, Size virtualSize)
                {
                    _moveResize(new PixelPoint((int)devicePoint.X, (int)devicePoint.Y), virtualSize, _hiddenWindow.Screens.Primary.PixelDensity);
                }

                public virtual double Scaling => _hiddenWindow.Screens.Primary.PixelDensity;
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
