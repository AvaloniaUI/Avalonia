using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Win32.Automation;
using Avalonia.Win32.Input;
using Avalonia.Win32.Interop;
using Avalonia.Win32.Interop.Automation;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    internal partial class WindowImpl
    {
        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation",
            Justification = "Using Win32 naming for consistency.")]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We do .NET COM interop availability checks")]
        [UnconditionalSuppressMessage("Trimming", "IL2050", Justification = "We do .NET COM interop availability checks")]
        protected virtual unsafe IntPtr AppWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            const double wheelDelta = 120.0;
            const long uiaRootObjectId = -25;
            uint timestamp = unchecked((uint)GetMessageTime());
            RawInputEventArgs? e = null;
            var shouldTakeFocus = false;
            var message = (WindowsMessage)msg;
            switch (message)
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
                        if (ToInt32(wParam) == 1 && (_windowProperties.Decorations == SystemDecorations.None || _isClientAreaExtended))
                        {
                            return IntPtr.Zero;
                        }

                        break;
                    }

                case WindowsMessage.WM_CLOSE:
                    {
                        bool? preventClosing = Closing?.Invoke(WindowCloseReason.WindowClosing);
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
                        // The first and foremost thing to do - notify the TopLevel
                        Closed?.Invoke();

                        if (UiaCoreTypesApi.IsNetComInteropAvailable)
                        {
                            UiaCoreProviderApi.UiaReturnRawElementProvider(_hwnd, IntPtr.Zero, IntPtr.Zero, null);
                        }

                        // We need to release IMM context and state to avoid leaks.
                        if (Imm32InputMethod.Current.Hwnd == _hwnd)
                        {
                            Imm32InputMethod.Current.ClearLanguageAndWindow();
                        }

                        // Cleanup render targets
                        (_glSurface as IDisposable)?.Dispose();

                        if (_dropTarget != null)
                        {
                            OleContext.Current?.UnregisterDragDrop(Handle);
                            _dropTarget.Dispose();
                            _dropTarget = null;
                        }

                        _framebuffer.Dispose();
                        _inputPane?.Dispose();

                        //Window doesn't exist anymore
                        _hwnd = IntPtr.Zero;
                        //Remove root reference to this class, so unmanaged delegate can be collected
                        s_instances.Remove(this);

                        _mouseDevice.Dispose();
                        _touchDevice.Dispose();
                        //Free other resources
                        Dispose();

                        // Schedule cleanup of anything that requires window to be destroyed
                        Dispatcher.UIThread.Post(AfterCloseCleanup);
                        return IntPtr.Zero;
                    }

                case WindowsMessage.WM_DPICHANGED:
                    {
                        _dpi = (uint)wParam >> 16;
                        var newDisplayRect = Marshal.PtrToStructure<RECT>(lParam);
                        _scaling = _dpi / 96.0;
                        RefreshIcon();
                        ScalingChanged?.Invoke(_scaling);

                        using (SetResizeReason(WindowResizeReason.DpiChange))
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

                case WindowsMessage.WM_GETICON:
                    if (_iconImpl == null)
                    {
                        break;
                    }

                    var requestIcon = (Icons)wParam;
                    var requestDpi = (uint) lParam;

                    if (requestDpi == 0)
                    {
                        requestDpi = _dpi;
                    }
                                        
                    return LoadIcon(requestIcon, requestDpi)?.Handle ?? default;

                case WindowsMessage.WM_KEYDOWN:
                case WindowsMessage.WM_SYSKEYDOWN:
                    {
                        e = TryCreateRawKeyEventArgs(RawKeyEventType.KeyDown, timestamp, wParam, lParam);
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
                        e = TryCreateRawKeyEventArgs(RawKeyEventType.KeyUp, timestamp, wParam, lParam);
                        break;
                    }
                case WindowsMessage.WM_CHAR:
                    {
                        if (Imm32InputMethod.Current.IsComposing)
                        {
                            break;
                        }

                        // Ignore control chars and chars that were handled in WM_KEYDOWN.
                        if (ToInt32(wParam) >= 32 && !_ignoreWmChar)
                        {
                            var text = new string((char)ToInt32(wParam), 1);

                            e = new RawTextInputEventArgs(WindowsKeyboardDevice.Instance, timestamp, Owner, text);
                        }

                        break;
                    }

                case WindowsMessage.WM_LBUTTONDOWN:
                case WindowsMessage.WM_RBUTTONDOWN:
                case WindowsMessage.WM_MBUTTONDOWN:
                case WindowsMessage.WM_XBUTTONDOWN:
                    {
                        if (IsMouseInPointerEnabled)
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
                            Owner,
#pragma warning disable CS8509
                            message switch
#pragma warning restore CS8509
                            {
                                WindowsMessage.WM_LBUTTONDOWN => RawPointerEventType.LeftButtonDown,
                                WindowsMessage.WM_RBUTTONDOWN => RawPointerEventType.RightButtonDown,
                                WindowsMessage.WM_MBUTTONDOWN => RawPointerEventType.MiddleButtonDown,
                                WindowsMessage.WM_XBUTTONDOWN =>
                                HighWord(ToInt32(wParam)) == 1 ?
                                    RawPointerEventType.XButton1Down :
                                    RawPointerEventType.XButton2Down,
                            },
                            DipFromLParam(lParam), GetMouseModifiers(wParam));
                        break;
                    }

                case WindowsMessage.WM_LBUTTONUP:
                case WindowsMessage.WM_RBUTTONUP:
                case WindowsMessage.WM_MBUTTONUP:
                case WindowsMessage.WM_XBUTTONUP:
                    {
                        if (IsMouseInPointerEnabled)
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
                            Owner,
#pragma warning disable CS8509
                            message switch
#pragma warning restore CS8509
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
                    if (!IsMouseInPointerEnabled)
                    {
                        _mouseDevice.Capture(null);
                    }

                    break;

                case WindowsMessage.WM_MOUSEMOVE:
                    {
                        if (IsMouseInPointerEnabled)
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

                        var point = DipFromLParam(lParam);

                        // Prepare points for the IntermediatePoints call.
                        var p = new POINT()
                        {
                            X = (int)(point.X * RenderScaling),
                            Y = (int)(point.Y * RenderScaling)
                        };
                        ClientToScreen(_hwnd, ref p);
                        var currPoint = new MOUSEMOVEPOINT()
                        {
                            x = p.X & 0xFFFF,
                            y = p.Y & 0xFFFF,
                            time = (int)timestamp
                        };
                        var prevPoint = _lastWmMousePoint;
                        _lastWmMousePoint = currPoint;

                        e = new RawPointerEventArgs(
                            _mouseDevice,
                            timestamp,
                            Owner,
                            RawPointerEventType.Move,
                            point,
                            GetMouseModifiers(wParam))
                        {
                            IntermediatePoints = new Lazy<IReadOnlyList<RawPointerPoint>?>(() => CreateIntermediatePoints(currPoint, prevPoint))
                        };

                        break;
                    }

                case WindowsMessage.WM_MOUSEWHEEL:
                    {
                        if (IsMouseInPointerEnabled)
                        {
                            break;
                        }
                        e = new RawMouseWheelEventArgs(
                            _mouseDevice,
                            timestamp,
                            Owner,
                            PointToClient(PointFromLParam(lParam)),
                            new Vector(0, (ToInt32(wParam) >> 16) / wheelDelta),
                            GetMouseModifiers(wParam));
                        break;
                    }

                case WindowsMessage.WM_MOUSEHWHEEL:
                    {
                        if (IsMouseInPointerEnabled)
                        {
                            break;
                        }
                        e = new RawMouseWheelEventArgs(
                            _mouseDevice,
                            timestamp,
                            Owner,
                            PointToClient(PointFromLParam(lParam)),
                            new Vector(-(ToInt32(wParam) >> 16) / wheelDelta, 0),
                            GetMouseModifiers(wParam));
                        break;
                    }

                case WindowsMessage.WM_MOUSELEAVE:
                    {
                        if (IsMouseInPointerEnabled)
                        {
                            break;
                        }
                        _trackingMouse = false;
                        e = new RawPointerEventArgs(
                            _mouseDevice,
                            timestamp,
                            Owner,
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
                        if (IsMouseInPointerEnabled)
                        {
                            break;
                        }
                        e = new RawPointerEventArgs(
                            _mouseDevice,
                            timestamp,
                            Owner,
#pragma warning disable CS8509
                            message switch
#pragma warning restore CS8509
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
                        if (_wmPointerEnabled || Input is not { } input)
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
                                input.Invoke(new RawTouchEventArgs(_touchDevice, touchInput.Time,
                                    Owner,
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
                case WindowsMessage.WM_NCPOINTERDOWN:
                case WindowsMessage.WM_NCPOINTERUP:
                case WindowsMessage.WM_POINTERDOWN:
                case WindowsMessage.WM_POINTERUP:
                case WindowsMessage.WM_POINTERUPDATE:
                    {
                        if (!_wmPointerEnabled)
                        {
                            break;
                        }
                        GetDevicePointerInfo(wParam, out var device, out var info, out var point, out var modifiers, ref timestamp);
                        var eventType = GetEventType(message, info);

                        var args = CreatePointerArgs(device, timestamp, eventType, point, modifiers, info.pointerId);
                        args.IntermediatePoints = CreateLazyIntermediatePoints(info);
                        e = args;
                        break;
                    }
                case WindowsMessage.WM_POINTERDEVICEOUTOFRANGE:
                case WindowsMessage.WM_POINTERLEAVE:
                case WindowsMessage.WM_POINTERCAPTURECHANGED:
                    {
                        if (!_wmPointerEnabled)
                        {
                            break;
                        }
                        GetDevicePointerInfo(wParam, out var device, out var info, out var point, out var modifiers, ref timestamp);
                        var eventType = device is TouchDevice ? RawPointerEventType.TouchCancel : RawPointerEventType.LeaveWindow;
                        e = CreatePointerArgs(device, timestamp, eventType, point, modifiers, info.pointerId);
                        break;
                    }
                case WindowsMessage.WM_POINTERWHEEL:
                case WindowsMessage.WM_POINTERHWHEEL:
                    {
                        if (!_wmPointerEnabled)
                        {
                            break;
                        }
                        GetDevicePointerInfo(wParam, out var device, out var info, out var point, out var modifiers, ref timestamp);

                        var val = (ToInt32(wParam) >> 16) / wheelDelta;
                        var delta = message == WindowsMessage.WM_POINTERWHEEL ? new Vector(0, val) : new Vector(val, 0);
                        e = new RawMouseWheelEventArgs(device, timestamp, Owner, point.Position, delta, modifiers)
                        {
                            RawPointerId = info.pointerId
                        };
                        break;
                    }
                case WindowsMessage.WM_POINTERDEVICEINRANGE:
                    {
                        if (!_wmPointerEnabled)
                        {
                            break;
                        }

                        // Do not generate events, but release mouse capture on any other device input.
                        GetDevicePointerInfo(wParam, out var device, out _, out _, out _, ref timestamp);
                        if (device != _mouseDevice)
                        {
                            _mouseDevice.Capture(null);
                            return IntPtr.Zero;
                        }
                        break;
                    }
                case WindowsMessage.WM_POINTERACTIVATE:
                    {
                        //occurs when a pointer activates an inactive window.
                        //we should handle this and return PA_ACTIVATE or PA_NOACTIVATE
                        //https://docs.microsoft.com/en-us/previous-versions/windows/desktop/inputmsg/wm-pointeractivate
                        break;
                    }
                case WindowsMessage.WM_POINTERDEVICECHANGE:
                    {
                        //notifies about changes in the settings of a monitor that has a digitizer attached to it.
                        //https://docs.microsoft.com/en-us/previous-versions/windows/desktop/inputmsg/wm-pointerdevicechange
                        break;
                    }
                case WindowsMessage.WM_NCPOINTERUPDATE:
                    {
                        //NC stands for non-client area - window header and window border
                        //As I found above in an old message handling - we dont need to handle NC pointer move/updates.
                        //All we need is pointer down and up. So this is skipped for now.
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
                        //This message is sent in a dialog scenarios. Contains mouse position.
                        //Old message, but listed in the wm_pointer reference
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
                        using (NonPumpingSyncContext.Use(NonPumpingWaitHelperImpl.Instance))
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
                    _resizeReason = WindowResizeReason.User;
                    break;

                case WindowsMessage.WM_SIZE:
                    {
                        var size = (SizeCommand)wParam;

                        if (Resized != null &&
                            (size == SizeCommand.Restored ||
                             size == SizeCommand.Maximized))
                        {
                            var clientSize = new Size(ToInt32(lParam) & 0xffff, ToInt32(lParam) >> 16);
                            Resized(clientSize / RenderScaling, _resizeReason);
                        }

                        var windowState = size switch
                        {
                            SizeCommand.Maximized => WindowState.Maximized,
                            SizeCommand.Minimized => WindowState.Minimized,
                            _ when _isFullScreenActive => WindowState.FullScreen,
                            _ => WindowState.Normal,
                        };

                        if (windowState != _lastWindowState)
                        {
                            _lastWindowState = windowState;

                            var newWindowProperties = _windowProperties;

                            newWindowProperties.WindowState = windowState;

                            UpdateWindowProperties(newWindowProperties);

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
                    _resizeReason = WindowResizeReason.Unspecified;
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
                        unchecked
                        {
                            DefWindowProc(Hwnd, msg, wParam, lParam & ~(nint)ISC_SHOWUICOMPOSITIONWINDOW);
                        }

                        UpdateInputMethod(GetKeyboardLayout(0));

                        return IntPtr.Zero;
                    }
                case WindowsMessage.WM_IME_COMPOSITION:
                    {
                        Imm32InputMethod.Current.HandleComposition(wParam, lParam, timestamp);

                        break;
                    }
                case WindowsMessage.WM_IME_SELECT:
                    break;
                case WindowsMessage.WM_IME_CHAR:
                case WindowsMessage.WM_IME_COMPOSITIONFULL:
                case WindowsMessage.WM_IME_CONTROL:
                case WindowsMessage.WM_IME_KEYDOWN:
                case WindowsMessage.WM_IME_KEYUP:
                case WindowsMessage.WM_IME_NOTIFY:
                    break;
                case WindowsMessage.WM_IME_STARTCOMPOSITION:
                    {
                        Imm32InputMethod.Current.HandleCompositionStart();

                        return IntPtr.Zero;
                    }
                case WindowsMessage.WM_IME_ENDCOMPOSITION:
                    {
                        Imm32InputMethod.Current.HandleCompositionEnd(timestamp);

                        return IntPtr.Zero;
                    }
                case WindowsMessage.WM_GETOBJECT:
                    if ((long)lParam == uiaRootObjectId && UiaCoreTypesApi.IsNetComInteropAvailable && _owner is Control control)
                    {
                        var peer = ControlAutomationPeer.CreatePeerForElement(control);
                        var node = AutomationNode.GetOrCreate(peer);
                        return UiaCoreProviderApi.UiaReturnRawElementProvider(_hwnd, wParam, lParam, node);
                    }
                    break;
            }

#if USE_MANAGED_DRAG
            if (_managedDrag.PreprocessInputEvent(ref e))
                return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
#endif

            if (shouldTakeFocus)
            {
                SetFocus(_hwnd);
            }

            if (e != null && Input != null)
            {
                Input(e);

                if (message == WindowsMessage.WM_KEYDOWN)
                {
                    if(e is RawKeyEventArgs args && args.Key == Key.ImeProcessed)
                    {
                        _ignoreWmChar = true;
                    }
                    else
                    {
                        // Handling a WM_KEYDOWN message should cause the subsequent WM_CHAR message to
                        // be ignored. This should be safe to do as WM_CHAR should only be produced in
                        // response to the call to TranslateMessage/DispatchMessage after a WM_KEYDOWN
                        // is handled.
                        _ignoreWmChar = e.Handled;
                    }
                 }

                if (s_intermediatePointsPooledList.Count > 0)
                {
                    s_intermediatePointsPooledList.Dispose();
                }

                if (e.Handled)
                {
                    return IntPtr.Zero;
                }
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private Lazy<IReadOnlyList<RawPointerPoint>?>? CreateLazyIntermediatePoints(POINTER_INFO info)
        {
            var historyCount = Math.Min((int)info.historyCount, MaxPointerHistorySize);
            if (historyCount > 1)
            {
                return new Lazy<IReadOnlyList<RawPointerPoint>?>(() =>
                {
                    s_intermediatePointsPooledList.Clear();
                    s_intermediatePointsPooledList.Capacity = historyCount;

                    // Pointers in history are ordered from newest to oldest, so we need to reverse iteration.
                    // Also we skip the newest pointer, because original event arguments already contains it.

                    if (info.pointerType == PointerInputType.PT_TOUCH)
                    {
                        s_historyTouchInfos ??= new POINTER_TOUCH_INFO[MaxPointerHistorySize];
                        if (GetPointerTouchInfoHistory(info.pointerId, ref historyCount, s_historyTouchInfos))
                        {
                            for (int i = historyCount - 1; i >= 1; i--)
                            {
                                var historyTouchInfo = s_historyTouchInfos[i];
                                s_intermediatePointsPooledList.Add(CreateRawPointerPoint(historyTouchInfo));
                            }
                        }
                    }
                    else if (info.pointerType == PointerInputType.PT_PEN)
                    {
                        s_historyPenInfos ??= new POINTER_PEN_INFO[MaxPointerHistorySize];
                        if (GetPointerPenInfoHistory(info.pointerId, ref historyCount, s_historyPenInfos))
                        {
                            for (int i = historyCount - 1; i >= 1; i--)
                            {
                                var historyPenInfo = s_historyPenInfos[i];
                                s_intermediatePointsPooledList.Add(CreateRawPointerPoint(historyPenInfo));
                            }
                        }
                    }
                    else
                    {
                        s_historyInfos ??= new POINTER_INFO[MaxPointerHistorySize];
                        // Currently Windows does not return history info for mouse input, but we handle it just for case.
                        if (GetPointerInfoHistory(info.pointerId, ref historyCount, s_historyInfos))
                        {
                            for (int i = historyCount - 1; i >= 1; i--)
                            {
                                var historyInfo = s_historyInfos[i];
                                s_intermediatePointsPooledList.Add(CreateRawPointerPoint(historyInfo));
                            }
                        }
                    }
                    return s_intermediatePointsPooledList;
                });
            }

            return null;
        }

        private unsafe IReadOnlyList<RawPointerPoint> CreateIntermediatePoints(MOUSEMOVEPOINT movePoint, MOUSEMOVEPOINT prevMovePoint)
        {
            // To understand some of this code, please check MS docs:
            // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getmousemovepointsex#remarks

            s_mouseHistoryInfos ??= new MOUSEMOVEPOINT[64];

            fixed (MOUSEMOVEPOINT* movePoints = s_mouseHistoryInfos)
            {
                var movePointCopy = movePoint;
                movePointCopy.time = 0; // empty "time" as otherwise WinAPI will always fail
                int pointsCount = GetMouseMovePointsEx(
                    (uint)(Marshal.SizeOf(movePointCopy)),
                    &movePointCopy, movePoints, s_mouseHistoryInfos.Length,
                    1);

                // GetMouseMovePointsEx can return -1 if point wasn't found or there is so beeg delay that original points were erased from the buffer.
                if (pointsCount <= 1)
                {
                    return Array.Empty<RawPointerPoint>();
                }

                s_intermediatePointsPooledList.Clear();
                s_intermediatePointsPooledList.Capacity = pointsCount;
                for (int i = pointsCount - 1; i >= 1; i--)
                {
                    var historyInfo = s_mouseHistoryInfos[i];
                    // Skip points newer than current point.
                    if (historyInfo.time > movePoint.time ||
                        (historyInfo.time == movePoint.time &&
                         historyInfo.x == movePoint.x &&
                         historyInfo.y == movePoint.y))
                    {
                        continue;
                    }
                    // Skip poins older from previous WM_MOUSEMOVE point.
                    if (historyInfo.time < prevMovePoint.time ||
                        (historyInfo.time == prevMovePoint.time &&
                            historyInfo.x == prevMovePoint.x &&
                            historyInfo.y == prevMovePoint.y))
                    {
                        continue;
                    }

                    // To support multiple screens.
                    if (historyInfo.x > 32767)
                        historyInfo.x -= 65536;

                    if (historyInfo.y > 32767)
                        historyInfo.y -= 65536;

                    var point = PointToClient(new PixelPoint(historyInfo.x, historyInfo.y));
                    s_intermediatePointsPooledList.Add(new RawPointerPoint
                    {
                        Position = point
                    });
                }
                return s_intermediatePointsPooledList;
            }
        }

        private RawPointerEventArgs CreatePointerArgs(IInputDevice device, ulong timestamp, RawPointerEventType eventType, RawPointerPoint point, RawInputModifiers modifiers, uint rawPointerId)
        {
            return device is TouchDevice
                ? new RawTouchEventArgs(device, timestamp, Owner, eventType, point, modifiers, rawPointerId)
                : new RawPointerEventArgs(device, timestamp, Owner, eventType, point, modifiers)
                {
                    RawPointerId = rawPointerId
                };
        }

        private void GetDevicePointerInfo(IntPtr wParam,
            out IPointerDevice device, out POINTER_INFO info, out RawPointerPoint point,
            out RawInputModifiers modifiers, ref uint timestamp)
        {
            var pointerId = (uint)(ToInt32(wParam) & 0xFFFF);
            GetPointerType(pointerId, out var type);

            modifiers = default;

            switch (type)
            {
                case PointerInputType.PT_PEN:
                    device = _penDevice;
                    GetPointerPenInfo(pointerId, out var penInfo);
                    info = penInfo.pointerInfo;
                    point = CreateRawPointerPoint(penInfo);
                    if (penInfo.penFlags.HasFlag(PenFlags.PEN_FLAGS_BARREL))
                    {
                        modifiers |= RawInputModifiers.PenBarrelButton;
                    }
                    if (penInfo.penFlags.HasFlag(PenFlags.PEN_FLAGS_ERASER))
                    {
                        modifiers |= RawInputModifiers.PenEraser;
                    }
                    if (penInfo.penFlags.HasFlag(PenFlags.PEN_FLAGS_INVERTED))
                    {
                        modifiers |= RawInputModifiers.PenInverted;
                    }
                    break;
                case PointerInputType.PT_TOUCH:
                    device = _touchDevice;
                    GetPointerTouchInfo(pointerId, out var touchInfo);
                    info = touchInfo.pointerInfo;
                    point = CreateRawPointerPoint(touchInfo);
                    break;
                default:
                    device = _mouseDevice;
                    GetPointerInfo(pointerId, out info);
                    point = CreateRawPointerPoint(info);
                    break;
            }

            if (info.dwTime != 0)
            {
                timestamp = info.dwTime;
            }

            modifiers |= GetInputModifiers(info.pointerFlags);
        }

        private RawPointerPoint CreateRawPointerPoint(POINTER_INFO pointerInfo)
        {
            var point = PointToClient(new PixelPoint(pointerInfo.ptPixelLocationX, pointerInfo.ptPixelLocationY));
            return new RawPointerPoint
            {
                Position = point
            };
        }
        private RawPointerPoint CreateRawPointerPoint(POINTER_TOUCH_INFO info)
        {
            var pointerInfo = info.pointerInfo;
            var point = PointToClient(new PixelPoint(pointerInfo.ptPixelLocationX, pointerInfo.ptPixelLocationY));
            return new RawPointerPoint
            {
                Position = point,
                // POINTER_PEN_INFO.pressure is normalized to a range between 0 and 1024, with 512 as a default.
                // But in our API we use range from 0.0 to 1.0.
                Pressure = info.pressure / 1024f
            };
        }
        private RawPointerPoint CreateRawPointerPoint(POINTER_PEN_INFO info)
        {
            var pointerInfo = info.pointerInfo;
            var point = PointToClient(new PixelPoint(pointerInfo.ptPixelLocationX, pointerInfo.ptPixelLocationY));
            return new RawPointerPoint
            {
                Position = point,
                // POINTER_PEN_INFO.pressure is normalized to a range between 0 and 1024, with 512 as a default.
                // But in our API we use range from 0.0 to 1.0.
                Pressure = info.pressure / 1024f,
                Twist = info.rotation,
                XTilt = info.tiltX,
                YTilt = info.tiltY
            };
        }

        private static RawPointerEventType GetEventType(WindowsMessage message, POINTER_INFO info)
        {
            var isTouch = info.pointerType == PointerInputType.PT_TOUCH;
            if (info.pointerFlags.HasFlag(PointerFlags.POINTER_FLAG_CANCELED))
            {
                return isTouch ? RawPointerEventType.TouchCancel : RawPointerEventType.LeaveWindow;
            }

            var eventType = ToEventType(info.ButtonChangeType, isTouch);
            if (eventType == RawPointerEventType.LeftButtonDown &&
                message == WindowsMessage.WM_NCPOINTERDOWN)
            {
                eventType = RawPointerEventType.NonClientLeftButtonDown;
            }

            return eventType;
        }

        private static RawPointerEventType ToEventType(PointerButtonChangeType type, bool isTouch)
        {
            return type switch
            {
                PointerButtonChangeType.POINTER_CHANGE_FIRSTBUTTON_DOWN when isTouch => RawPointerEventType.TouchBegin,
                PointerButtonChangeType.POINTER_CHANGE_FIRSTBUTTON_DOWN when !isTouch => RawPointerEventType.LeftButtonDown,
                PointerButtonChangeType.POINTER_CHANGE_SECONDBUTTON_DOWN => RawPointerEventType.RightButtonDown,
                PointerButtonChangeType.POINTER_CHANGE_THIRDBUTTON_DOWN => RawPointerEventType.MiddleButtonDown,
                PointerButtonChangeType.POINTER_CHANGE_FOURTHBUTTON_DOWN => RawPointerEventType.XButton1Down,
                PointerButtonChangeType.POINTER_CHANGE_FIFTHBUTTON_DOWN => RawPointerEventType.XButton2Down,

                PointerButtonChangeType.POINTER_CHANGE_FIRSTBUTTON_UP when isTouch => RawPointerEventType.TouchEnd,
                PointerButtonChangeType.POINTER_CHANGE_FIRSTBUTTON_UP when !isTouch => RawPointerEventType.LeftButtonUp,
                PointerButtonChangeType.POINTER_CHANGE_SECONDBUTTON_UP => RawPointerEventType.RightButtonUp,
                PointerButtonChangeType.POINTER_CHANGE_THIRDBUTTON_UP => RawPointerEventType.MiddleButtonUp,
                PointerButtonChangeType.POINTER_CHANGE_FOURTHBUTTON_UP => RawPointerEventType.XButton1Up,
                PointerButtonChangeType.POINTER_CHANGE_FIFTHBUTTON_UP => RawPointerEventType.XButton2Up,
                _ when isTouch => RawPointerEventType.TouchUpdate,
                _ => RawPointerEventType.Move
            };
        }

        private void UpdateInputMethod(IntPtr hkl)
        {
            // note: for non-ime language, also create it so that emoji panel tracks cursor
            var langid = LGID(hkl);

            if (langid == _langid && Imm32InputMethod.Current.Hwnd == Hwnd)
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

        private static PixelPoint PointFromLParam(IntPtr lParam)
        {
            return new PixelPoint((short)(ToInt32(lParam) & 0xffff), (short)(ToInt32(lParam) >> 16));
        }

        private bool ShouldIgnoreTouchEmulatedMessage()
        {
            // Note: GetMessageExtraInfo doesn't work with WM_POINTER events.

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

        private static RawInputModifiers GetInputModifiers(PointerFlags flags)
        {
            var modifiers = WindowsKeyboardDevice.Instance.Modifiers;

            if (flags.HasAllFlags(PointerFlags.POINTER_FLAG_FIRSTBUTTON))
            {
                modifiers |= RawInputModifiers.LeftMouseButton;
            }

            if (flags.HasAllFlags(PointerFlags.POINTER_FLAG_SECONDBUTTON))
            {
                modifiers |= RawInputModifiers.RightMouseButton;
            }

            if (flags.HasAllFlags(PointerFlags.POINTER_FLAG_THIRDBUTTON))
            {
                modifiers |= RawInputModifiers.MiddleMouseButton;
            }

            if (flags.HasAllFlags(PointerFlags.POINTER_FLAG_FOURTHBUTTON))
            {
                modifiers |= RawInputModifiers.XButton1MouseButton;
            }

            if (flags.HasAllFlags(PointerFlags.POINTER_FLAG_FIFTHBUTTON))
            {
                modifiers |= RawInputModifiers.XButton2MouseButton;
            }

            return modifiers;
        }

        private RawKeyEventArgs? TryCreateRawKeyEventArgs(RawKeyEventType eventType, ulong timestamp, IntPtr wParam, IntPtr lParam)
        {
            var virtualKey = ToInt32(wParam);
            var keyData = ToInt32(lParam);
            var key = KeyInterop.KeyFromVirtualKey(virtualKey, keyData);
            var physicalKey = KeyInterop.PhysicalKeyFromVirtualKey(virtualKey, keyData);

            if (key == Key.None && physicalKey == PhysicalKey.None)
                return null;

            var keySymbol = KeyInterop.GetKeySymbol(virtualKey, keyData);

            return new RawKeyEventArgs(
                WindowsKeyboardDevice.Instance,
                timestamp,
                Owner,
                eventType,
                key,
                WindowsKeyboardDevice.Instance.Modifiers,
                physicalKey,
                keySymbol);
        }
    }
}
