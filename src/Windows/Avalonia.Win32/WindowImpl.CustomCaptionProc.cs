using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.VisualTree;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    internal partial class WindowImpl
    {
        private HitTestValues HitTestNCA(IntPtr hWnd, IntPtr wParam, IntPtr lParam)
        {
            // Get the point coordinates for the hit test (screen space).
            var ptMouse = PointFromLParam(lParam);

            // Get the window rectangle.
            GetWindowRect(hWnd, out var rcWindow);

            // Get the frame rectangle, adjusted for the style without a caption.
            var rcFrame = new RECT();
            var borderThickness = new RECT();

            var isMaximized = GetWindowPlacement(hWnd, out var placement) && placement.ShowCmd == ShowWindowCommand.ShowMaximized;
            if (!isMaximized)
            {
                var style = (WindowStyles)GetWindowLong(_hwnd, (int)WindowLongParam.GWL_STYLE);
                if (style.HasAllFlags(WindowStyles.WS_THICKFRAME))
                {
                    var adjuster = CreateWindowRectAdjuster();
                    adjuster.Adjust(ref rcFrame, style & ~WindowStyles.WS_CAPTION, 0);
                    adjuster.Adjust(ref borderThickness, style, 0);

                    borderThickness.left *= -1;
                    borderThickness.top *= -1;
                }
            }

            if (_extendTitleBarHint >= 0)
            {
                borderThickness.top = (int)(_extendedMargins.Top * RenderScaling);
            }

            // Determine if the hit test is for resizing. Default middle (1,1).
            ushort uRow = 1;
            ushort uCol = 1;
            bool onResizeBorder = false;

            // Determine if the point is at the left or right of the window.
            if (ptMouse.X >= rcWindow.left && ptMouse.X < rcWindow.left + borderThickness.left)
            {
                uCol = 0; // left side
            }
            else if (ptMouse.X < rcWindow.right && ptMouse.X >= rcWindow.right - borderThickness.right)
            {
                uCol = 2; // right side
            }

            // Determine if the point is at the top or bottom of the window.
            if (ptMouse.Y >= rcWindow.top && ptMouse.Y < rcWindow.top + borderThickness.top)
            {
                onResizeBorder = (ptMouse.Y < (rcWindow.top - rcFrame.top));

                // Two cases where we have a valid row 0 hit test:
                // - window resize border (top resize border hit)
                // - area below resize border that is actual titlebar (caption hit).
                if (onResizeBorder || uCol == 1)
                {
                    uRow = 0;
                }
            }
            else if (ptMouse.Y < rcWindow.bottom && ptMouse.Y >= rcWindow.bottom - borderThickness.bottom)
            {
                uRow = 2;
            }

            var captionAreaHitTest = WindowState == WindowState.FullScreen ? HitTestValues.HTNOWHERE : HitTestValues.HTCAPTION;
            ReadOnlySpan<HitTestValues> hitZones = stackalloc HitTestValues[]
            {
                HitTestValues.HTTOPLEFT, onResizeBorder ? HitTestValues.HTTOP : captionAreaHitTest,
                HitTestValues.HTTOPRIGHT, HitTestValues.HTLEFT, HitTestValues.HTNOWHERE, HitTestValues.HTRIGHT,
                HitTestValues.HTBOTTOMLEFT, HitTestValues.HTBOTTOM, HitTestValues.HTBOTTOMRIGHT
            };

            var zoneIndex = uRow * 3 + uCol;

            return hitZones[zoneIndex];
        }

        protected virtual IntPtr CustomCaptionProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool callDwp)
        {
            RawPointerEventArgs? e = null;
            IntPtr lRet = IntPtr.Zero;

            callDwp = !DwmDefWindowProc(hWnd, msg, wParam, lParam, ref lRet);

            switch ((WindowsMessage)msg)
            {
                case WindowsMessage.WM_DWMCOMPOSITIONCHANGED:
                    // TODO handle composition changed.
                    break;

                case WindowsMessage.WM_NCHITTEST:
                    if (lRet == IntPtr.Zero)
                    {
                        var hittestResult = HitTestNCA(hWnd, wParam, lParam);
                        if (hittestResult is HitTestValues.HTNOWHERE or HitTestValues.HTCAPTION)
                        {
                            var visualHittestResult = HitTestVisual(lParam);
                            if (visualHittestResult != HitTestValues.HTNOWHERE)
                            {
                                hittestResult = visualHittestResult;
                            }
                        }

                        if (hittestResult != HitTestValues.HTNOWHERE)
                        {
                            lRet = (IntPtr)hittestResult;
                            callDwp = false;
                        }
                    }
                    break;

                // Normally, Avalonia doesn't handles non-client input as a special NonClientLeftButtonDown, ignoring move and up events.
                // What makes it a problem, Avalonia has to mark templated caption buttons as a non-client area.
                // Meaning, these buttons no longer can accept normal client input.
                // These messages are needed to explicitly fake this normal client input from non-client messages.
                // For both WM_NCMOUSE and WM_NCPOINTERUPDATE
                case WindowsMessage.WM_NCMOUSEMOVE when !IsMouseInPointerEnabled:
                case WindowsMessage.WM_NCLBUTTONDOWN when !IsMouseInPointerEnabled:
                case WindowsMessage.WM_NCLBUTTONUP when !IsMouseInPointerEnabled:
                    if (lRet == IntPtr.Zero)
                    {
                        var shouldRedirect = ShouldRedirectNonClientInput(hWnd, wParam, lParam);

                        if (shouldRedirect)
                        {
                            // Track non-client mouse to receive WM_NCMOUSELEAVE
                            if (!_trackingNonClientMouse)
                            {
                                var tm = new TRACKMOUSEEVENT
                                {
                                    cbSize = Marshal.SizeOf<TRACKMOUSEEVENT>(),
                                    dwFlags = TME_LEAVE | TME_NONCLIENT,
                                    hwndTrack = _hwnd,
                                    dwHoverTime = 0,
                                };
                                TrackMouseEvent(ref tm);
                                _trackingNonClientMouse = true;
                            }

                            e = new RawPointerEventArgs(
                                _mouseDevice,
                                unchecked((uint)GetMessageTime()),
                                Owner,
                                (WindowsMessage)msg switch
                                {
                                    WindowsMessage.WM_NCMOUSEMOVE => RawPointerEventType.Move,
                                    WindowsMessage.WM_NCLBUTTONDOWN => RawPointerEventType.LeftButtonDown,
                                    WindowsMessage.WM_NCLBUTTONUP => RawPointerEventType.LeftButtonUp,
                                    _ => throw new ArgumentOutOfRangeException(nameof(msg), msg, null)
                                },
                                PointToClient(PointFromLParam(lParam)),
                                RawInputModifiers.None);
                        }
                        else if (_trackingNonClientMouse && (WindowsMessage)msg == WindowsMessage.WM_NCMOUSEMOVE)
                        {
                            // Mouse moved in NC area but not over caption buttons - send leave event
                            _trackingNonClientMouse = false;
                            e = new RawPointerEventArgs(
                                _mouseDevice,
                                unchecked((uint)GetMessageTime()),
                                Owner,
                                RawPointerEventType.LeaveWindow,
                                new Point(-1, -1),
                                RawInputModifiers.None);
                        }
                    }
                    break;
                case WindowsMessage.WM_NCMOUSELEAVE when !IsMouseInPointerEnabled:
                    _trackingNonClientMouse = false;
                    e = new RawPointerEventArgs(
                        _mouseDevice,
                        unchecked((uint)GetMessageTime()),
                        Owner,
                        RawPointerEventType.LeaveWindow,
                        new Point(-1, -1),
                        RawInputModifiers.None);
                    break;
                case WindowsMessage.WM_NCPOINTERUPDATE when _wmPointerEnabled:
                case WindowsMessage.WM_NCPOINTERDOWN when _wmPointerEnabled:
                case WindowsMessage.WM_NCPOINTERUP when _wmPointerEnabled:
                    if (lRet == IntPtr.Zero
                        && ShouldRedirectNonClientInput(hWnd, wParam, lParam))
                    {
                        uint timestamp = 0;
                        GetDevicePointerInfo(wParam, out var device, out var info, out var point, out var modifiers, ref timestamp);
                        var eventType = (WindowsMessage)msg switch
                        {
                            WindowsMessage.WM_NCPOINTERUPDATE => RawPointerEventType.Move,
                            WindowsMessage.WM_NCPOINTERDOWN => RawPointerEventType.LeftButtonDown,
                            WindowsMessage.WM_NCPOINTERUP => RawPointerEventType.LeftButtonUp,
                            _ => throw new ArgumentOutOfRangeException(nameof(msg), msg, null)
                        };
                        e = CreatePointerArgs(device, timestamp, eventType, point, modifiers, info.pointerId);
                    }
                    break;
            }

            if (e is not null && Input is not null)
            {
                Input(e);
                if (e.Handled)
                {
                    callDwp = false;
                    return IntPtr.Zero;
                }
            }

            return lRet;
        }

        private HitTestValues HitTestVisual(IntPtr lParam)
        {
            var position = PointToClient(PointFromLParam(lParam));
            if (_owner is Window window)
            {
                var visual = window.GetVisualAt(position, x =>
                {
                    if (x is IInputElement ie && (!ie.IsHitTestVisible || !ie.IsEffectivelyVisible))
                    {
                        return false;
                    }

                    return true;
                });

                if (visual != null)
                {
                    var hitTest = Win32Properties.GetNonClientHitTestResult(visual);
                    return (HitTestValues)hitTest;
                }
            }
            return HitTestValues.HTNOWHERE;
        }

        private bool ShouldRedirectNonClientInput(IntPtr hWnd, IntPtr wParam, IntPtr lParam)
        {
            // We touched frame borders or caption, don't redirect.
            if (HitTestNCA(hWnd, wParam, lParam) is not (HitTestValues.HTNOWHERE or HitTestValues.HTCAPTION))
                return false;

            // Redirect only for buttons.
            return HitTestVisual(lParam)
                is HitTestValues.HTMINBUTTON
                or HitTestValues.HTMAXBUTTON
                or HitTestValues.HTCLOSE
                or HitTestValues.HTHELP
                or HitTestValues.HTMENU
                or HitTestValues.HTSYSMENU;
        }
    }
}
