using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    public class TrayIconImpl : ITrayIconImpl
    {
        private readonly int _uniqueId = 0;
        private static int _nextUniqueId = 0;
        private bool _iconAdded;
        private IconImpl _icon;

        private static Dictionary<int, TrayIconImpl> s_trayIcons = new Dictionary<int, TrayIconImpl>();

        internal static void ProcWnd(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == (int)CustomWindowsMessage.WM_TRAYMOUSE && s_trayIcons.ContainsKey(wParam.ToInt32()))
            {
                s_trayIcons[wParam.ToInt32()].WndProc(hWnd, msg, wParam, lParam);
            }
        }

        public TrayIconImpl()
        {
            _uniqueId = ++_nextUniqueId;

            s_trayIcons.Add(_uniqueId, this);

            UpdateIcon();
        }


        ~TrayIconImpl()
        {
            UpdateIcon(false);
        }

        public void SetIcon(IWindowIconImpl icon)
        {
            _icon = icon as IconImpl;
            UpdateIcon();
        }

        public void SetIsVisible(bool visible)
        {
            if (visible)
            {

            }
        }

        public void SetToolTipText(string text)
        {
            throw new NotImplementedException();
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
                szTip = "Tool tip text here."
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
                        break;

                    case (int)WindowsMessage.WM_LBUTTONDBLCLK:
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

        private static void OnRightClicked()
        {
            var _trayMenu = new TrayPopupRoot()
            {
                SystemDecorations = SystemDecorations.None,
                SizeToContent = SizeToContent.WidthAndHeight,
                Background = null,
                TransparencyLevelHint = WindowTransparencyLevel.Transparent,
                Content = new MenuFlyoutPresenter()
                {
                    Items = new List<MenuItem>
                                {
                                    new MenuItem {  Header = "Item 1"},
                                    new MenuItem {  Header = "Item 2"},
                                    new MenuItem {  Header = "Item 3"},
                                    new MenuItem {  Header = "Item 4"},
                                    new MenuItem {  Header = "Item 5"}
                                }
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
    }
}
