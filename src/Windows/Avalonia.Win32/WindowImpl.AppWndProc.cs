using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Remote;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Win32.Input;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    public partial class WindowImpl
    {
        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation",
            Justification = "Using Win32 naming for consistency.")]
        protected virtual unsafe IntPtr AppWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            const double wheelDelta = 120.0;
            uint timestamp = unchecked((uint)GetMessageTime());
            RawInputEventArgs e = null;
            var shouldTakeFocus = false;

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
                                    UpdateInputMethod(GetKeyboardLayout(0));
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
                        if (ToInt32(wParam) == 1 && !HasFullDecorations || _isClientAreaExtended)
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

                        BeforeCloseCleanup(false);

                        // Used to distinguish between programmatic and regular close requests.
                        _isCloseRequested = true;

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
                        
                        using (SetResizeReason(PlatformResizeReason.DpiChange))
                        { 
                            SetWindowPos(hWnd,
                                IntPtr.Zero,
                                newDisplayRect.left,
                                newDisplayRect.top,
                                newDisplayRect.right - newDisplayRect.left,
                                newDisplayRect.bottom - newDisplayRect.top,
                                SetWindowPosFlags.SWP_NOZORDER |
                                SetWindowPosFlags.SWP_NOACTIVATE);
                        }

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

                case WindowsMessage.WM_SYSCOMMAND:
                    // Disable system handling of Alt/F10 menu keys.
                    if ((SysCommands)wParam == SysCommands.SC_KEYMENU && HighWord(ToInt32(lParam)) <= 0)
                        return IntPtr.Zero;
                    break;

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
                        // Ignore control chars and chars that were handled in WM_KEYDOWN.
                        if (ToInt32(wParam) >= 32 && !_ignoreWmChar)
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
                        if (BelowWin8)
                        {
                            break;
                        }
                        shouldTakeFocus = ShouldTakeFocusOnClick;
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
                        if (BelowWin8)
                        {
                            break;
                        }
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
                // Mouse capture is lost
                case WindowsMessage.WM_CANCELMODE:
                    _mouseDevice.Capture(null);
                    break;

                case WindowsMessage.WM_MOUSEMOVE:
                    {
                        if (BelowWin8)
                        {
                            break;
                        }
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
                            DipFromLParam(lParam), 
                            GetMouseModifiers(wParam));

                        break;
                    }

                case WindowsMessage.WM_MOUSEWHEEL:
                    {
                        if (BelowWin8)
                        {
                            break;
                        }
                        e = new RawMouseWheelEventArgs(
                            _mouseDevice,
                            timestamp,
                            _owner,
                            PointToClient(PointFromLParam(lParam)),
                            new Vector(0, (ToInt32(wParam) >> 16) / wheelDelta), 
                            GetMouseModifiers(wParam));
                        break;
                    }

                case WindowsMessage.WM_MOUSEHWHEEL:
                    {
                        if (BelowWin8)
                        {
                            break;
                        }
                        e = new RawMouseWheelEventArgs(
                            _mouseDevice,
                            timestamp,
                            _owner,
                            PointToClient(PointFromLParam(lParam)),
                            new Vector(-(ToInt32(wParam) >> 16) / wheelDelta, 0), 
                            GetMouseModifiers(wParam));
                        break;
                    }

                case WindowsMessage.WM_MOUSELEAVE:
                    {
                        if (BelowWin8)
                        {
                            break;
                        }
                        _trackingMouse = false;
                        e = new RawPointerEventArgs(
                            _mouseDevice,
                            timestamp,
                            _owner,
                            RawPointerEventType.LeaveWindow,
                            new Point(-1, -1), 
                            WindowsKeyboardDevice.Instance.Modifiers);
                        break;
                    }

                case WindowsMessage.WM_NCLBUTTONDOWN:
                case WindowsMessage.WM_NCRBUTTONDOWN:
                case WindowsMessage.WM_NCMBUTTONDOWN:
                case WindowsMessage.WM_NCXBUTTONDOWN:
                    {
                        if (BelowWin8)
                        {
                            break;
                        }
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
                        if (BelowWin8)
                        {
                            break;
                        }
                        var touchInputCount = wParam.ToInt32();

                        var pTouchInputs = stackalloc TOUCHINPUT[touchInputCount];
                        var touchInputs = new Span<TOUCHINPUT>(pTouchInputs, touchInputCount);

                        if (GetTouchInputInfo(lParam, (uint)touchInputCount, pTouchInputs, Marshal.SizeOf<TOUCHINPUT>()))
                        {
                            foreach (var touchInput in touchInputs)
                            {
                                Input?.Invoke(new RawTouchEventArgs(_touchDevice, touchInput.Time,
                                    _owner,
                                    touchInput.Flags.HasAllFlags(TouchInputFlags.TOUCHEVENTF_UP) ?
                                        RawPointerEventType.TouchEnd :
                                        touchInput.Flags.HasAllFlags(TouchInputFlags.TOUCHEVENTF_DOWN) ?
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











                case WindowsMessage.WM_POINTERDEVICECHANGE:
                    {
                        //notifies about changes in the settings of a monitor that has a digitizer attached to it.
                        //https://docs.microsoft.com/en-us/previous-versions/windows/desktop/inputmsg/wm-pointerdevicechange
                        break;
                    }
                case WindowsMessage.WM_POINTERDEVICEINRANGE:
                case WindowsMessage.WM_POINTERDEVICEOUTOFRANGE:
                    {
                        //notifies about proximity of pointer device to the digitizer.
                        //contains pointer id and proximity.
                        //https://docs.microsoft.com/en-us/previous-versions/windows/desktop/inputmsg/wm-pointerdeviceinrange
                        break;
                    }
                case WindowsMessage.WM_NCPOINTERUPDATE:
                case WindowsMessage.WM_NCPOINTERDOWN:
                case WindowsMessage.WM_NCPOINTERUP:
                    {
                        //NC stands for non-client area - window header and window border

                        break;
                    }
                case WindowsMessage.WM_POINTERUPDATE:
                case WindowsMessage.WM_POINTERDOWN:
                case WindowsMessage.WM_POINTERUP:
                    {

                        break;
                    }
                case WindowsMessage.WM_POINTERENTER:
                    {
                        //this is not handled by WM_MOUSEENTER so I think there is no need to handle this too.
                        //but we can detect a new pointer by this message and calling IS_POINTER_NEW_WPARAM

                        //note: by using a pen there can be a pointer leave or enter inside a window coords
                        //when you are just lift up the pen above the display
                        break;
                    }
                case WindowsMessage.WM_POINTERLEAVE:
                    {
                        GetDeviceInfo(wParam, out var device, out var info);
                        var point = PointToClient(PointFromLParam(lParam));

                        e = new RawPointerEventArgs(
                            device,
                            timestamp,
                            _owner,
                            RawPointerEventType.LeaveWindow,
                            point,
                            WindowsKeyboardDevice.Instance.Modifiers);
                        break;
                    }
                case WindowsMessage.WM_POINTERACTIVATE:
                    {
                        //occurs when a pointer activates an inactive window.
                        //we should handle this and return PA_ACTIVATE or PA_NOACTIVATE
                        //https://docs.microsoft.com/en-us/previous-versions/windows/desktop/inputmsg/wm-pointeractivate
                        break;
                    }
                case WindowsMessage.WM_POINTERCAPTURECHANGED:
                    {
                        _mouseDevice.Capture(null);
                        return IntPtr.Zero;
                    }
                case WindowsMessage.WM_POINTERWHEEL:
                    {
                        GetDeviceInfo(wParam, out var device, out var info);

                        var point = PointToClient(PointFromLParam(lParam));
                        var modifiers = GetInputModifiers(info.dwKeyStates);
                        var delta = new Vector(0, GetWheelDelta(wParam) / wheelDelta);
                        e = new RawMouseWheelEventArgs(device, timestamp, _owner, point, delta, modifiers);
                        break;
                    }
                case WindowsMessage.WM_POINTERHWHEEL:
                    {
                        GetDeviceInfo(wParam, out var device, out var info);

                        var point = PointToClient(PointFromLParam(lParam));
                        var modifiers = GetInputModifiers(info.dwKeyStates);
                        var delta = new Vector(GetWheelDelta(wParam) / wheelDelta, 0);
                        e = new RawMouseWheelEventArgs(device, timestamp, _owner, point, delta, modifiers);
                        break;
                    }
                case WindowsMessage.DM_POINTERHITTEST:
                    {
                        //DM stands for direct manipulation.
                        //https://docs.microsoft.com/en-us/previous-versions/windows/desktop/directmanipulation/direct-manipulation-portal
                        break;
                    }
                case WindowsMessage.WM_TOUCHHITTESTING:
                    {
                        //This is to determine the most probable touch target.
                        //provides an input bounding box and receives hit proximity
                        //https://docs.microsoft.com/en-us/previous-versions/windows/desktop/inputmsg/wm-touchhittesting
                        //https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-touch_hit_testing_input
                        break;
                    }
                case WindowsMessage.WM_PARENTNOTIFY:
                    {
                        //This message is sent in a dialog scenarios. Contains mouse position in an old-way,
                        //but listed in the wm_pointer reference
                        //https://docs.microsoft.com/en-us/previous-versions/windows/desktop/inputmsg/wm-parentnotify
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
                    using(NonPumpingSyncContext.Use())
                    using (_rendererLock.Lock())
                    {
                        if (BeginPaint(_hwnd, out PAINTSTRUCT ps) != IntPtr.Zero)
                        {
                            var f = RenderScaling;
                            var r = ps.rcPaint;
                            Paint?.Invoke(new Rect(r.left / f, r.top / f, (r.right - r.left) / f,
                                (r.bottom - r.top) / f));
                            EndPaint(_hwnd, ref ps);
                        }
                    }

                    return IntPtr.Zero;
                }


                case WindowsMessage.WM_ENTERSIZEMOVE:
                    _resizeReason = PlatformResizeReason.User;
                    break;

                case WindowsMessage.WM_SIZE:
                    {
                        using(NonPumpingSyncContext.Use())
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
                            Resized(clientSize / RenderScaling, _resizeReason);
                        }

                        var windowState = size == SizeCommand.Maximized ?
                            WindowState.Maximized :
                            (size == SizeCommand.Minimized ? WindowState.Minimized : WindowState.Normal);

                        if (windowState != _lastWindowState)
                        {
                            _lastWindowState = windowState;

                            WindowStateChanged?.Invoke(windowState);

                            if (_isClientAreaExtended)
                            {
                                UpdateExtendMargins();

                                ExtendClientAreaToDecorationsChanged?.Invoke(true);
                            }
                        }

                        return IntPtr.Zero;
                    }

                case WindowsMessage.WM_EXITSIZEMOVE:
                    _resizeReason = PlatformResizeReason.Unspecified;
                    break;

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
                                (int)((_minSize.Width * RenderScaling) + BorderThickness.Left + BorderThickness.Right);
                        }

                        if (_minSize.Height > 0)
                        {
                            mmi.ptMinTrackSize.Y =
                                (int)((_minSize.Height * RenderScaling) + BorderThickness.Top + BorderThickness.Bottom);
                        }

                        if (!double.IsInfinity(_maxSize.Width) && _maxSize.Width > 0)
                        {
                            mmi.ptMaxTrackSize.X =
                                (int)((_maxSize.Width * RenderScaling) + BorderThickness.Left + BorderThickness.Right);
                        }

                        if (!double.IsInfinity(_maxSize.Height) && _maxSize.Height > 0)
                        {
                            mmi.ptMaxTrackSize.Y =
                                (int)((_maxSize.Height * RenderScaling) + BorderThickness.Top + BorderThickness.Bottom);
                        }

                        Marshal.StructureToPtr(mmi, lParam, true);
                        return IntPtr.Zero;
                    }

                case WindowsMessage.WM_DISPLAYCHANGE:
                    {
                        (Screen as ScreenImpl)?.InvalidateScreensCache();
                        return IntPtr.Zero;
                    }

                case WindowsMessage.WM_KILLFOCUS:
                    LostFocus?.Invoke();
                    break;

                case WindowsMessage.WM_INPUTLANGCHANGE:
                    {
                        UpdateInputMethod(lParam);
                        // call DefWindowProc to pass to all children
                        break;
                    }
                case WindowsMessage.WM_IME_SETCONTEXT:
                    {
                        // TODO if we implement preedit, disable the composition window:
                        // lParam = new IntPtr((int)(((uint)lParam.ToInt64()) & ~ISC_SHOWUICOMPOSITIONWINDOW));
                        UpdateInputMethod(GetKeyboardLayout(0));
                        break;
                    }
                case WindowsMessage.WM_IME_CHAR:
                case WindowsMessage.WM_IME_COMPOSITION:
                case WindowsMessage.WM_IME_COMPOSITIONFULL:
                case WindowsMessage.WM_IME_CONTROL:
                case WindowsMessage.WM_IME_KEYDOWN:
                case WindowsMessage.WM_IME_KEYUP:
                case WindowsMessage.WM_IME_NOTIFY:
                case WindowsMessage.WM_IME_SELECT:
                    break;
                case WindowsMessage.WM_IME_STARTCOMPOSITION:
                    Imm32InputMethod.Current.IsComposing = true;
                    break;
                case WindowsMessage.WM_IME_ENDCOMPOSITION:
                    Imm32InputMethod.Current.IsComposing = false;
                    break;
            }

#if USE_MANAGED_DRAG
            if (_managedDrag.PreprocessInputEvent(ref e))
                return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
#endif
            
            if(shouldTakeFocus)
            {
                SetFocus(_hwnd);
            }

            if (e != null && Input != null)
            {
                Input(e);

                if ((WindowsMessage)msg == WindowsMessage.WM_KEYDOWN)
                {
                    // Handling a WM_KEYDOWN message should cause the subsequent WM_CHAR message to
                    // be ignored. This should be safe to do as WM_CHAR should only be produced in
                    // response to the call to TranslateMessage/DispatchMessage after a WM_KEYDOWN
                    // is handled.
                    _ignoreWmChar = e.Handled;
                }

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

        private void GetDeviceInfo(IntPtr wParam, out IInputDevice device, out POINTER_INFO info)
        {
            var pointerId = ToPointerId(wParam);
            GetPointerType(pointerId, out var type);//ToDo we can cache this and invalidate in WM_POINTERDEVICECHANGE
            switch (type)
            {
                case PointerInputType.PT_PEN:
                    device = _penDevice;
                    GetPointerPenInfo(pointerId, out var penInfo);
                    info = penInfo.pointerInfo;
                    break;
                case PointerInputType.PT_TOUCH:
                    device = _touchDevice;
                    GetPointerTouchInfo(pointerId, out var touchInfo);
                    info = touchInfo.pointerInfo;
                    break;
                default:
                    device = _mouseDevice;
                    GetPointerInfo(pointerId, out info);
                    break;
            }
        }

        public static uint GetWheelDelta(IntPtr wParam) => HIWORD(wParam);
        public static uint ToPointerId(IntPtr wParam) => LOWORD(wParam);
        public static uint LOWORD(IntPtr param) => (uint)param & 0xffff;
        public static uint HIWORD(IntPtr param) => (uint)param >> 16;


        public readonly bool BelowWin8 = Win32Platform.WindowsVersion < PlatformConstants.Windows8;

        private void UpdateInputMethod(IntPtr hkl)
        {
            // note: for non-ime language, also create it so that emoji panel tracks cursor
            var langid = LGID(hkl);
            if (langid == _langid && Imm32InputMethod.Current.HWND == Hwnd)
            {
                return;
            } 
            _langid = langid;

            Imm32InputMethod.Current.SetLanguageAndWindow(this, Hwnd, hkl);
            
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
            return new Point((short)(ToInt32(lParam) & 0xffff), (short)(ToInt32(lParam) >> 16)) / RenderScaling;
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
            return GetInputModifiers(keys);
        }

        private static RawInputModifiers GetInputModifiers(ModifierKeys keys)
        {
            var modifiers = WindowsKeyboardDevice.Instance.Modifiers;

            if (keys.HasAllFlags(ModifierKeys.MK_LBUTTON))
            {
                modifiers |= RawInputModifiers.LeftMouseButton;
            }

            if (keys.HasAllFlags(ModifierKeys.MK_RBUTTON))
            {
                modifiers |= RawInputModifiers.RightMouseButton;
            }

            if (keys.HasAllFlags(ModifierKeys.MK_MBUTTON))
            {
                modifiers |= RawInputModifiers.MiddleMouseButton;
            }

            if (keys.HasAllFlags(ModifierKeys.MK_XBUTTON1))
            {
                modifiers |= RawInputModifiers.XButton1MouseButton;
            }

            if (keys.HasAllFlags(ModifierKeys.MK_XBUTTON2))
            {
                modifiers |= RawInputModifiers.XButton2MouseButton;
            }

            return modifiers;
        }
    }
}
