using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    internal class TrayIconImpl : ITrayIconImpl
    {
        private static Win32Icon? s_emptyIcon;
        private readonly int _uniqueId;
        private static int s_nextUniqueId;
        private static nint s_taskBarMonitor;

        private bool _iconAdded;
        private IconImpl? _iconImpl;
        private bool _iconStale;
        private Win32Icon? _icon;
        private string? _tooltipText;
        private readonly Win32NativeToManagedMenuExporter _exporter;
        private static readonly Dictionary<int, TrayIconImpl> s_trayIcons = new();
        private bool _disposedValue;
        private static readonly uint WM_TASKBARCREATED = RegisterWindowMessage("TaskbarCreated");

        private readonly Dictionary<int, NativeMenuItem> _menuItemMap = new();
        private static TrayIconImpl? s_activeMenuOwner;
        private int _nextMenuId = 1000;

        internal static void ChangeWindowMessageFilter(IntPtr hWnd)
        {
            ChangeWindowMessageFilterEx(hWnd, WM_TASKBARCREATED, MessageFilterFlag.MSGFLT_ALLOW, IntPtr.Zero);
        }

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
                case (uint)WindowsMessage.WM_COMMAND:
                    if (s_activeMenuOwner != null)
                    {
                        int cmdId = wParam.ToInt32() & 0xFFFF;
                        s_activeMenuOwner.HandleMenuCommand(cmdId);
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
            _iconImpl = (IconImpl?)icon;
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
            Win32Icon? newIcon = null;
            if (_iconStale && _iconImpl is not null)
            {
                var scaling = GetTaskBarMonScalingOrDefault();
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
                iconData.hIcon = (_iconStale ? newIcon : _icon)?.Handle ?? GetOrCreateEmptyIcon().Handle;
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

        private static Win32Icon GetOrCreateEmptyIcon()
        {
            if (s_emptyIcon is null)
            {
                using var bitmap = new WriteableBitmap(
                    new PixelSize(32, 32), new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Unpremul);
                s_emptyIcon = new Win32Icon(bitmap);
            }

            return s_emptyIcon;
        }

        private double GetTaskBarMonScalingOrDefault()
        {
            if (ShCoreAvailable && Win32Platform.WindowsVersion > PlatformConstants.Windows8_1)
            {
                uint dpiX, dpiY;

                if ((HRESULT)GetDpiForMonitor(
                        s_taskBarMonitor,
                        MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI,
                        out dpiX,
                        out dpiY)  == HRESULT.S_OK)
                {
                    Debug.Assert(dpiX == dpiY);
                    return dpiX / 96.0;
                }
            }

            return 1.0;
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
            var menu = _exporter.GetNativeMenu();
            if (menu == null || menu.Items.Count == 0)
            {
                return;
            }

            // Raise Opening event so listeners can modify items before display
            if (menu is INativeMenuExporterEventsImplBridge bridge)
                bridge.RaiseOpening();

            _menuItemMap.Clear();
            _nextMenuId = 1000;

            IntPtr hMenu = BuildNativeMenu(menu);
            if (hMenu == IntPtr.Zero)
                return;

            GetCursorPos(out POINT pt);

            s_activeMenuOwner = this;
            SetForegroundWindow(Win32Platform.Instance.Handle);
            TrackPopupMenu(hMenu, TPM_RIGHTBUTTON, pt.X, pt.Y, 0, Win32Platform.Instance.Handle, IntPtr.Zero);
            s_activeMenuOwner = null;

            DestroyMenu(hMenu);

            // Raise Closed event after menu dismissed
            if (menu is INativeMenuExporterEventsImplBridge bridge2)
                bridge2.RaiseClosed();
        }

        private IntPtr BuildNativeMenu(NativeMenu menu)
        {
            IntPtr hMenu = CreatePopupMenu();

            foreach (var item in menu.Items)
            {
                if (item is NativeMenuItem { IsVisible: false })
                    continue;

                if (item is NativeMenuItemSeparator)
                {
                    AppendMenuW(hMenu, MF_SEPARATOR, IntPtr.Zero, null);
                }
                else if (item is NativeMenuItem menuItem)
                {
                    if (menuItem.Menu is { Items.Count: > 0 })
                    {
                        // Submenu
                        IntPtr hSubMenu = BuildNativeMenu(menuItem.Menu);
                        uint flags = MF_POPUP;
                        if (!menuItem.IsEnabled)
                            flags |= MF_DISABLED | MF_GRAYED;
                        if (!AppendMenuW(hMenu, flags, hSubMenu, menuItem.Header ?? ""))
                            DestroyMenu(hSubMenu);
                    }
                    else
                    {
                        int cmdId = _nextMenuId++;
                        uint flags = MF_STRING;
                        if (!menuItem.IsEnabled)
                            flags |= MF_DISABLED | MF_GRAYED;
                        if (menuItem.IsChecked)
                            flags |= MF_CHECKED;
                        AppendMenuW(hMenu, flags, (IntPtr)cmdId, menuItem.Header ?? "");
                        _menuItemMap[cmdId] = menuItem;
                    }
                }
            }

            return hMenu;
        }

        private void HandleMenuCommand(int cmdId)
        {
            if (_menuItemMap.TryGetValue(cmdId, out var menuItem))
            {
                if (menuItem is INativeMenuItemExporterEventsImplBridge bridge)
                    bridge.RaiseClicked();
            }
        }

        // P/Invoke for native menu APIs
        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AppendMenuW(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string? lpNewItem);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y,
            int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyMenu(IntPtr hMenu);

        private const uint MF_STRING = 0x00000000;
        private const uint MF_SEPARATOR = 0x00000800;
        private const uint MF_DISABLED = 0x00000002;
        private const uint MF_GRAYED = 0x00000001;
        private const uint MF_CHECKED = 0x00000008;
        private const uint MF_POPUP = 0x00000010;
        private const uint TPM_RIGHTBUTTON = 0x0002;

        /// <summary>
        /// Custom Win32 window messages for the NotifyIcon
        /// </summary>
        private enum CustomWindowsMessage : uint
        {
            WM_TRAYICON = WindowsMessage.WM_APP + 1024,
            WM_TRAYMOUSE = WindowsMessage.WM_USER + 1024,
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
