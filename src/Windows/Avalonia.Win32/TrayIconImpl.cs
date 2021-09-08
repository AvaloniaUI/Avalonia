using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    /// <summary>
    /// Custom Win32 window messages for the NotifyIcon
    /// </summary>
    public enum CustomWindowsMessage : uint
    {
        WM_TRAYICON = (uint)WindowsMessage.WM_APP + 1024,
        WM_TRAYMOUSE = (uint)WindowsMessage.WM_USER + 1024
    }

    public class TrayIconManagedPopupPositionerPopupImplHelper : IManagedPopupPositionerPopup
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

    public class TrayPopupRoot : Window
    {
        private ManagedPopupPositioner _positioner;

        public TrayPopupRoot()
        {
            _positioner = new ManagedPopupPositioner(new TrayIconManagedPopupPositionerPopupImplHelper(MoveResize));
            Topmost = true;

            LostFocus += TrayPopupRoot_LostFocus;
        }

        private void TrayPopupRoot_LostFocus(object sender, Interactivity.RoutedEventArgs e)
        {
            Close();
        }

        private void MoveResize(PixelPoint position, Size size, double scaling)
        {
            PlatformImpl.Move(position);
            PlatformImpl.Resize(size, PlatformResizeReason.Layout);
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
                ConstraintAdjustment = PopupPositionerConstraintAdjustment.FlipX | PopupPositionerConstraintAdjustment.FlipY | PopupPositionerConstraintAdjustment.SlideX | PopupPositionerConstraintAdjustment.SlideY,
            });
        }
    }

    public class TrayIconImpl : ITrayIconImpl
    {
        private readonly int _uniqueId = 0;
        private static int _nextUniqueId = 0;
        private WndProc _wndProcDelegate;
        private IntPtr _hwnd;
        private bool _iconAdded;
        private IconImpl _icon;

        public TrayIconImpl()
        {
            _uniqueId = ++_nextUniqueId;

            CreateMessageWindow();

            UpdateIcon();
        }


        ~TrayIconImpl()
        {
            UpdateIcon(false);
        }

        private void CreateMessageWindow()
        {
            // Ensure that the delegate doesn't get garbage collected by storing it as a field.
            _wndProcDelegate = new UnmanagedMethods.WndProc(WndProc);

            UnmanagedMethods.WNDCLASSEX wndClassEx = new UnmanagedMethods.WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<UnmanagedMethods.WNDCLASSEX>(),
                lpfnWndProc = _wndProcDelegate,
                hInstance = UnmanagedMethods.GetModuleHandle(null),
                lpszClassName = "AvaloniaMessageWindow " + Guid.NewGuid(),
            };

            ushort atom = UnmanagedMethods.RegisterClassEx(ref wndClassEx);

            if (atom == 0)
            {
                throw new Win32Exception();
            }

            _hwnd = UnmanagedMethods.CreateWindowEx(0, atom, null, 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            if (_hwnd == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
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
                hWnd = _hwnd,
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
                    UnmanagedMethods.Shell_NotifyIcon(NIM.ADD, iconData);
                    _iconAdded = true;
                }
                else
                {
                    UnmanagedMethods.Shell_NotifyIcon(NIM.MODIFY, iconData);
                }
            }
            else
            {
                UnmanagedMethods.Shell_NotifyIcon(NIM.DELETE, iconData);
                _iconAdded = false;
            }
        }

        private void OnRightClicked()
        {
            UnmanagedMethods.GetCursorPos(out UnmanagedMethods.POINT pt);
            var cursor = new PixelPoint(pt.X, pt.Y);

            var trayMenu = new TrayPopupRoot()
            {
                Position = cursor,
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

            trayMenu.Show();
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
            }

            return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
