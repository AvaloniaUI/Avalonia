// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Win32.Input;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    public partial class WindowImpl
    {
        // Hit test the frame for resizing and moving.
        HitTestValues HitTestNCA(IntPtr hWnd, IntPtr wParam, IntPtr lParam)
        {
            // Get the point coordinates for the hit test.
            var ptMouse = PointFromLParam(lParam);

            // Get the window rectangle.            
            GetWindowRect(hWnd, out var rcWindow);

            // Get the frame rectangle, adjusted for the style without a caption.
            RECT rcFrame = new RECT();
            AdjustWindowRectEx(ref rcFrame, (uint)(WindowStyles.WS_OVERLAPPEDWINDOW & ~WindowStyles.WS_CAPTION), false, 0);

            RECT border_thickness = new RECT();
            if (GetStyle().HasFlag(WindowStyles.WS_THICKFRAME))
            {
                AdjustWindowRectEx(ref border_thickness, (uint)(GetStyle()), false, 0);
                border_thickness.left *= -1;
                border_thickness.top *= -1;
            }
            else if (GetStyle().HasFlag(WindowStyles.WS_BORDER))
            {
                border_thickness = new RECT { bottom = 1, left = 1, right = 1, top = 1 };
            }

            // Determine if the hit test is for resizing. Default middle (1,1).
            ushort uRow = 1;
            ushort uCol = 1;
            bool fOnResizeBorder = false;

            // Determine if the point is at the top or bottom of the window.
            if (ptMouse.Y >= rcWindow.top && ptMouse.Y < rcWindow.top + border_thickness.top)
            {
                fOnResizeBorder = (ptMouse.Y < (rcWindow.top - rcFrame.top));
                uRow = 0;
            }
            else if (ptMouse.Y < rcWindow.bottom && ptMouse.Y >= rcWindow.bottom - border_thickness.bottom)
            {
                uRow = 2;
            }

            // Determine if the point is at the left or right of the window.
            if (ptMouse.X >= rcWindow.left && ptMouse.X < rcWindow.left + border_thickness.left)
            {
                uCol = 0; // left side
            }
            else if (ptMouse.X < rcWindow.right && ptMouse.X >= rcWindow.right - border_thickness.right)
            {
                uCol = 2; // right side
            }

            // Hit test (HTTOPLEFT, ... HTBOTTOMRIGHT)
            HitTestValues[][] hitTests = new[]
            {
                new []{ HitTestValues.HTTOPLEFT,    fOnResizeBorder ? HitTestValues.HTTOP : HitTestValues.HTCAPTION,    HitTestValues.HTTOPRIGHT },
                new []{ HitTestValues.HTLEFT,      HitTestValues.HTNOWHERE,     HitTestValues.HTRIGHT },
                new []{ HitTestValues.HTBOTTOMLEFT, HitTestValues.HTBOTTOM, HitTestValues.HTBOTTOMRIGHT },
            };

            return hitTests[uRow][uCol];
        }

        protected virtual unsafe IntPtr CustomCaptionProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool callDwp)
        {
            IntPtr lRet = IntPtr.Zero;

            callDwp = !DwmDefWindowProc(hWnd, msg, wParam, lParam, ref lRet);            
            
            switch ((WindowsMessage)msg)
            {
                //case WindowsMessage.WM_ACTIVATE:
                //    {
                //        if (!_isClientAreaExtended)
                //        {
                //            ExtendClientArea();
                //        }
                //        lRet = IntPtr.Zero;
                //        callDwp = true;
                //        break;
                //    }

                case WindowsMessage.WM_NCCALCSIZE:
                    {
                        if (ToInt32(wParam) == 1)
                        {
                            lRet = IntPtr.Zero;
                            callDwp = false;
                        }
                        break;
                    }

                case WindowsMessage.WM_NCHITTEST:
                    if (lRet == IntPtr.Zero)
                    {
                        lRet = (IntPtr)HitTestNCA(hWnd, wParam, lParam);

                        uint timestamp = unchecked((uint)GetMessageTime());

                        if (((HitTestValues)lRet) == HitTestValues.HTCAPTION)
                        {
                            var position = PointToClient(PointFromLParam(lParam));
                            
                            var visual = (_owner as Window).Renderer.HitTestFirst(position, _owner as Window, x =>
                            {
                                if(x is IInputElement ie && !ie.IsHitTestVisible)
                                {
                                    return false;
                                }

                                return true;
                            });

                            if(visual != null)
                            {                                
                                lRet = (IntPtr)HitTestValues.HTCLIENT;
                            }
                                                       
                        }

                        if (((HitTestValues)lRet) != HitTestValues.HTNOWHERE)
                        {
                            callDwp = false;
                        }
                    }
                    break;
            }

            return lRet;
        }
    }

    public partial class WindowImpl
    {
        protected virtual unsafe IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            IntPtr lRet = IntPtr.Zero;
            bool callDwp = true;

            if (DwmIsCompositionEnabled(out bool enabled) == 0)
            {
                lRet = CustomCaptionProc(hWnd, msg, wParam, lParam, ref callDwp);
            }

            if (callDwp)
            {
                lRet = AppWndProc(hWnd, msg, wParam, lParam);
            }

            return lRet;
        }
    }

    public partial class WindowImpl
    {
        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation",
            Justification = "Using Win32 naming for consistency.")]
        protected virtual unsafe IntPtr AppWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            const double wheelDelta = 120.0;
            uint timestamp = unchecked((uint)GetMessageTime());

            RawInputEventArgs e = null;

            switch ((WindowsMessage)msg)
            {
                case WindowsMessage.WM_ACTIVATE:
                {
                    var wa = (WindowActivate)(ToInt32(wParam) & 0xffff);

                    switch (wa)
                    {
                        case WindowActivate.WA_ACTIVE:
                        case WindowActivate.WA_CLICKACTIVE:
                        {
                            Activated?.Invoke();
                            break;
                        }

                        case WindowActivate.WA_INACTIVE:
                        {
                            Deactivated?.Invoke();
                            break;
                        }
                    }

                    return IntPtr.Zero;
                }

                case WindowsMessage.WM_NCCALCSIZE:
                {
                    if (ToInt32(wParam) == 1 && !HasFullDecorations)
                    {
                        return IntPtr.Zero;
                    }

                    break;
                }

                case WindowsMessage.WM_CLOSE:
                {
                    bool? preventClosing = Closing?.Invoke();
                    if (preventClosing == true)
                    {
                        return IntPtr.Zero;
                    }

                    break;
                }

                case WindowsMessage.WM_DESTROY:
                {
                    //Window doesn't exist anymore
                    _hwnd = IntPtr.Zero;
                    //Remove root reference to this class, so unmanaged delegate can be collected
                    s_instances.Remove(this);
                    Closed?.Invoke();

                    _mouseDevice.Dispose();
                    _touchDevice?.Dispose();
                    //Free other resources
                    Dispose();
                    return IntPtr.Zero;
                }

                case WindowsMessage.WM_DPICHANGED:
                {
                    var dpi = ToInt32(wParam) & 0xffff;
                    var newDisplayRect = Marshal.PtrToStructure<RECT>(lParam);
                    _scaling = dpi / 96.0;
                    ScalingChanged?.Invoke(_scaling);
                    SetWindowPos(hWnd,
                        IntPtr.Zero,
                        newDisplayRect.left,
                        newDisplayRect.top,
                        newDisplayRect.right - newDisplayRect.left,
                        newDisplayRect.bottom - newDisplayRect.top,
                        SetWindowPosFlags.SWP_NOZORDER |
                        SetWindowPosFlags.SWP_NOACTIVATE);
                    return IntPtr.Zero;
                }

                case WindowsMessage.WM_KEYDOWN:
                case WindowsMessage.WM_SYSKEYDOWN:
                {
                    e = new RawKeyEventArgs(
                        WindowsKeyboardDevice.Instance,
                        timestamp,
                        _owner,
                        RawKeyEventType.KeyDown,
                        KeyInterop.KeyFromVirtualKey(ToInt32(wParam), ToInt32(lParam)),
                        WindowsKeyboardDevice.Instance.Modifiers);
                    break;
                }

                case WindowsMessage.WM_MENUCHAR:
                {
                    // mute the system beep
                    return (IntPtr)((int)MenuCharParam.MNC_CLOSE << 16);
                }

                case WindowsMessage.WM_KEYUP:
                case WindowsMessage.WM_SYSKEYUP:
                {
                    e = new RawKeyEventArgs(
                        WindowsKeyboardDevice.Instance,
                        timestamp,
                        _owner,
                        RawKeyEventType.KeyUp,
                        KeyInterop.KeyFromVirtualKey(ToInt32(wParam), ToInt32(lParam)),
                        WindowsKeyboardDevice.Instance.Modifiers);
                    break;
                }
                case WindowsMessage.WM_CHAR:
                {
                    // Ignore control chars
                    if (ToInt32(wParam) >= 32)
                    {
                        e = new RawTextInputEventArgs(WindowsKeyboardDevice.Instance, timestamp, _owner,
                            new string((char)ToInt32(wParam), 1));
                    }

                    break;
                }

                case WindowsMessage.WM_LBUTTONDOWN:
                case WindowsMessage.WM_RBUTTONDOWN:
                case WindowsMessage.WM_MBUTTONDOWN:
                case WindowsMessage.WM_XBUTTONDOWN:
                {
                    if (ShouldIgnoreTouchEmulatedMessage())
                    {
                        break;
                    }

                    e = new RawPointerEventArgs(
                        _mouseDevice,
                        timestamp,
                        _owner,
                        (WindowsMessage)msg switch
                        {
                            WindowsMessage.WM_LBUTTONDOWN => RawPointerEventType.LeftButtonDown,
                            WindowsMessage.WM_RBUTTONDOWN => RawPointerEventType.RightButtonDown,
                            WindowsMessage.WM_MBUTTONDOWN => RawPointerEventType.MiddleButtonDown,
                            WindowsMessage.WM_XBUTTONDOWN =>
                            HighWord(ToInt32(wParam)) == 1 ?
                                RawPointerEventType.XButton1Down :
                                RawPointerEventType.XButton2Down
                        },
                        DipFromLParam(lParam), GetMouseModifiers(wParam));
                    break;
                }

                case WindowsMessage.WM_LBUTTONUP:
                case WindowsMessage.WM_RBUTTONUP:
                case WindowsMessage.WM_MBUTTONUP:
                case WindowsMessage.WM_XBUTTONUP:
                {
                    if (ShouldIgnoreTouchEmulatedMessage())
                    {
                        break;
                    }

                    e = new RawPointerEventArgs(
                        _mouseDevice,
                        timestamp,
                        _owner,
                        (WindowsMessage)msg switch
                        {
                            WindowsMessage.WM_LBUTTONUP => RawPointerEventType.LeftButtonUp,
                            WindowsMessage.WM_RBUTTONUP => RawPointerEventType.RightButtonUp,
                            WindowsMessage.WM_MBUTTONUP => RawPointerEventType.MiddleButtonUp,
                            WindowsMessage.WM_XBUTTONUP =>
                            HighWord(ToInt32(wParam)) == 1 ?
                                RawPointerEventType.XButton1Up :
                                RawPointerEventType.XButton2Up,
                        },
                        DipFromLParam(lParam), GetMouseModifiers(wParam));
                    break;
                }

                case WindowsMessage.WM_MOUSEMOVE:
                {
                    if (ShouldIgnoreTouchEmulatedMessage())
                    {
                        break;
                    }

                    if (!_trackingMouse)
                    {
                        var tm = new TRACKMOUSEEVENT
                        {
                            cbSize = Marshal.SizeOf<TRACKMOUSEEVENT>(),
                            dwFlags = 2,
                            hwndTrack = _hwnd,
                            dwHoverTime = 0,
                        };

                        TrackMouseEvent(ref tm);
                    }

                    e = new RawPointerEventArgs(
                        _mouseDevice,
                        timestamp,
                        _owner,
                        RawPointerEventType.Move,
                        DipFromLParam(lParam), GetMouseModifiers(wParam));

                    break;
                }

                case WindowsMessage.WM_MOUSEWHEEL:
                {
                    e = new RawMouseWheelEventArgs(
                        _mouseDevice,
                        timestamp,
                        _owner,
                        PointToClient(PointFromLParam(lParam)),
                        new Vector(0, (ToInt32(wParam) >> 16) / wheelDelta), GetMouseModifiers(wParam));
                    break;
                }

                case WindowsMessage.WM_MOUSEHWHEEL:
                {
                    e = new RawMouseWheelEventArgs(
                        _mouseDevice,
                        timestamp,
                        _owner,
                        PointToClient(PointFromLParam(lParam)),
                        new Vector(-(ToInt32(wParam) >> 16) / wheelDelta, 0), GetMouseModifiers(wParam));
                    break;
                }

                case WindowsMessage.WM_MOUSELEAVE:
                {
                    _trackingMouse = false;
                    e = new RawPointerEventArgs(
                        _mouseDevice,
                        timestamp,
                        _owner,
                        RawPointerEventType.LeaveWindow,
                        new Point(-1, -1), WindowsKeyboardDevice.Instance.Modifiers);
                    break;
                }

                case WindowsMessage.WM_NCLBUTTONDOWN:
                case WindowsMessage.WM_NCRBUTTONDOWN:
                case WindowsMessage.WM_NCMBUTTONDOWN:
                case WindowsMessage.WM_NCXBUTTONDOWN:
                {
                    e = new RawPointerEventArgs(
                        _mouseDevice,
                        timestamp,
                        _owner,
                        (WindowsMessage)msg switch
                        {
                            WindowsMessage.WM_NCLBUTTONDOWN => RawPointerEventType
                                .NonClientLeftButtonDown,
                            WindowsMessage.WM_NCRBUTTONDOWN => RawPointerEventType.RightButtonDown,
                            WindowsMessage.WM_NCMBUTTONDOWN => RawPointerEventType.MiddleButtonDown,
                            WindowsMessage.WM_NCXBUTTONDOWN =>
                            HighWord(ToInt32(wParam)) == 1 ?
                                RawPointerEventType.XButton1Down :
                                RawPointerEventType.XButton2Down,
                        },
                        PointToClient(PointFromLParam(lParam)), GetMouseModifiers(wParam));
                    break;
                }
                case WindowsMessage.WM_TOUCH:
                {
                    var touchInputCount = wParam.ToInt32();

                    var pTouchInputs = stackalloc TOUCHINPUT[touchInputCount];
                    var touchInputs = new Span<TOUCHINPUT>(pTouchInputs, touchInputCount);

                    if (GetTouchInputInfo(lParam, (uint)touchInputCount, pTouchInputs, Marshal.SizeOf<TOUCHINPUT>()))
                    {
                        foreach (var touchInput in touchInputs)
                        {
                            Input?.Invoke(new RawTouchEventArgs(_touchDevice, touchInput.Time,
                                _owner,
                                touchInput.Flags.HasFlagCustom(TouchInputFlags.TOUCHEVENTF_UP) ?
                                    RawPointerEventType.TouchEnd :
                                    touchInput.Flags.HasFlagCustom(TouchInputFlags.TOUCHEVENTF_DOWN) ?
                                        RawPointerEventType.TouchBegin :
                                        RawPointerEventType.TouchUpdate,
                                PointToClient(new PixelPoint(touchInput.X / 100, touchInput.Y / 100)),
                                WindowsKeyboardDevice.Instance.Modifiers,
                                touchInput.Id));
                        }

                        CloseTouchInputHandle(lParam);
                        return IntPtr.Zero;
                    }

                    break;
                }
                case WindowsMessage.WM_NCPAINT:
                {
                    if (!HasFullDecorations)
                    {
                        return IntPtr.Zero;
                    }

                    break;
                }

                case WindowsMessage.WM_NCACTIVATE:
                {
                    if (!HasFullDecorations)
                    {
                        return new IntPtr(1);
                    }

                    break;
                }

                case WindowsMessage.WM_PAINT:
                {
                    using (_rendererLock.Lock())
                    {
                        if (BeginPaint(_hwnd, out PAINTSTRUCT ps) != IntPtr.Zero)
                        {
                            var f = Scaling;
                            var r = ps.rcPaint;
                            Paint?.Invoke(new Rect(r.left / f, r.top / f, (r.right - r.left) / f,
                                (r.bottom - r.top) / f));
                            EndPaint(_hwnd, ref ps);
                        }
                    }

                    return IntPtr.Zero;
                }

                case WindowsMessage.WM_SIZE:
                {
                    using (_rendererLock.Lock())
                    {
                        // Do nothing here, just block until the pending frame render is completed on the render thread
                    }

                    var size = (SizeCommand)wParam;

                    if (Resized != null &&
                        (size == SizeCommand.Restored ||
                         size == SizeCommand.Maximized))
                    {
                        var clientSize = new Size(ToInt32(lParam) & 0xffff, ToInt32(lParam) >> 16);
                        Resized(clientSize / Scaling);
                    }

                    var windowState = size == SizeCommand.Maximized ?
                        WindowState.Maximized :
                        (size == SizeCommand.Minimized ? WindowState.Minimized : WindowState.Normal);

                    if (windowState != _lastWindowState)
                    {
                        _lastWindowState = windowState;
                        WindowStateChanged?.Invoke(windowState);
                    }

                    return IntPtr.Zero;
                }

                case WindowsMessage.WM_MOVE:
                {
                    PositionChanged?.Invoke(new PixelPoint((short)(ToInt32(lParam) & 0xffff),
                        (short)(ToInt32(lParam) >> 16)));
                    return IntPtr.Zero;
                }

                case WindowsMessage.WM_GETMINMAXINFO:
                {
                    MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    
                    _maxTrackSize = mmi.ptMaxTrackSize;

                    if (_minSize.Width > 0)
                    {
                        mmi.ptMinTrackSize.X =
                            (int)((_minSize.Width * Scaling) + BorderThickness.Left + BorderThickness.Right);
                    }

                    if (_minSize.Height > 0)
                    {
                        mmi.ptMinTrackSize.Y =
                            (int)((_minSize.Height * Scaling) + BorderThickness.Top + BorderThickness.Bottom);
                    }

                    if (!double.IsInfinity(_maxSize.Width) && _maxSize.Width > 0)
                    {
                        mmi.ptMaxTrackSize.X =
                            (int)((_maxSize.Width * Scaling) + BorderThickness.Left + BorderThickness.Right);
                    }

                    if (!double.IsInfinity(_maxSize.Height) && _maxSize.Height > 0)
                    {
                        mmi.ptMaxTrackSize.Y =
                            (int)((_maxSize.Height * Scaling) + BorderThickness.Top + BorderThickness.Bottom);
                    }

                    Marshal.StructureToPtr(mmi, lParam, true);
                    return IntPtr.Zero;
                }

                case WindowsMessage.WM_DISPLAYCHANGE:
                {
                    (Screen as ScreenImpl)?.InvalidateScreensCache();
                    return IntPtr.Zero;
                }
            }

#if USE_MANAGED_DRAG
            if (_managedDrag.PreprocessInputEvent(ref e))
                return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
#endif

            if (e != null && Input != null)
            {
                Input(e);

                if (e.Handled)
                {
                    return IntPtr.Zero;
                }
            }

            using (_rendererLock.Lock())
            {
                return DefWindowProc(hWnd, msg, wParam, lParam);
            }
        }

        private static int ToInt32(IntPtr ptr)
        {
            if (IntPtr.Size == 4)
                return ptr.ToInt32();

            return (int)(ptr.ToInt64() & 0xffffffff);
        }

        private static int HighWord(int param) => param >> 16;

        private Point DipFromLParam(IntPtr lParam)
        {
            return new Point((short)(ToInt32(lParam) & 0xffff), (short)(ToInt32(lParam) >> 16)) / Scaling;
        }

        private PixelPoint PointFromLParam(IntPtr lParam)
        {
            return new PixelPoint((short)(ToInt32(lParam) & 0xffff), (short)(ToInt32(lParam) >> 16));
        }

        private bool ShouldIgnoreTouchEmulatedMessage()
        {
            if (!_multitouch)
            {
                return false;
            }

            // MI_WP_SIGNATURE
            // https://docs.microsoft.com/en-us/windows/win32/tablet/system-events-and-mouse-messages
            const long marker = 0xFF515700L;

            var info = GetMessageExtraInfo().ToInt64();
            return (info & marker) == marker;
        }

        private static RawInputModifiers GetMouseModifiers(IntPtr wParam)
        {
            var keys = (ModifierKeys)ToInt32(wParam);
            var modifiers = WindowsKeyboardDevice.Instance.Modifiers;

            if (keys.HasFlagCustom(ModifierKeys.MK_LBUTTON))
            {
                modifiers |= RawInputModifiers.LeftMouseButton;
            }

            if (keys.HasFlagCustom(ModifierKeys.MK_RBUTTON))
            {
                modifiers |= RawInputModifiers.RightMouseButton;
            }

            if (keys.HasFlagCustom(ModifierKeys.MK_MBUTTON))
            {
                modifiers |= RawInputModifiers.MiddleMouseButton;
            }

            if (keys.HasFlagCustom(ModifierKeys.MK_XBUTTON1))
            {
                modifiers |= RawInputModifiers.XButton1MouseButton;
            }

            if (keys.HasFlagCustom(ModifierKeys.MK_XBUTTON2))
            {
                modifiers |= RawInputModifiers.XButton2MouseButton;
            }

            return modifiers;
        }
    }
}
