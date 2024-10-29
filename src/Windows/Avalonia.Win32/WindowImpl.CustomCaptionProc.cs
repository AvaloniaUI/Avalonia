using System;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.VisualTree;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    internal partial class WindowImpl
    {
        // Hit test the frame for resizing and moving.
        private HitTestValues HitTestNCA(IntPtr hWnd, IntPtr wParam, IntPtr lParam)
        {
            // Get the point coordinates for the hit test (screen space).
            var ptMouse = PointFromLParam(lParam);

            // Get the window rectangle.
            GetWindowRect(hWnd, out var rcWindow);

            // Get the frame rectangle, adjusted for the style without a caption.
            var rcFrame = new RECT();
            AdjustWindowRectEx(ref rcFrame, (uint)(WindowStyles.WS_OVERLAPPEDWINDOW & ~WindowStyles.WS_CAPTION), false, 0);

            var borderThickness = new RECT();
            
            AdjustWindowRectEx(ref borderThickness, (uint)GetStyle(), false, 0);
            borderThickness.left *= -1;
            borderThickness.top *= -1;

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

            ReadOnlySpan<HitTestValues> hitZones = stackalloc HitTestValues[]
            {
                HitTestValues.HTTOPLEFT, onResizeBorder ? HitTestValues.HTTOP : HitTestValues.HTCAPTION,
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
                        if (WindowState == WindowState.FullScreen)
                        {
                            return (IntPtr)HitTestValues.HTCLIENT;
                        }
                        var hittestResult = HitTestNCA(hWnd, wParam, lParam);

                        lRet = (IntPtr)hittestResult;

                        if (hittestResult == HitTestValues.HTCAPTION)
                        {
                            var captionHittestResult = HitTestCaption(lParam);
                            if (captionHittestResult != HitTestValues.HTNOWHERE)
                            {
                                lRet = (IntPtr)captionHittestResult;
                            } 
                        }

                        if (hittestResult != HitTestValues.HTNOWHERE)
                        {
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
                    if (lRet == IntPtr.Zero
                        && WindowState != WindowState.FullScreen
                        && HitTestNCA(hWnd, wParam, lParam) is HitTestValues.HTCAPTION
                        && HitTestCaption(lParam) is HitTestValues.HTCLOSE or HitTestValues.HTMINBUTTON or HitTestValues.HTMAXBUTTON)
                    {
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
                    break;
                case WindowsMessage.WM_NCPOINTERUPDATE when _wmPointerEnabled:
                case WindowsMessage.WM_NCPOINTERDOWN when _wmPointerEnabled:
                case WindowsMessage.WM_NCPOINTERUP when _wmPointerEnabled:
                    if (lRet == IntPtr.Zero
                        && WindowState != WindowState.FullScreen
                        && HitTestNCA(hWnd, wParam, lParam) is HitTestValues.HTCAPTION
                        && HitTestCaption(lParam) is HitTestValues.HTCLOSE or HitTestValues.HTMINBUTTON or HitTestValues.HTMAXBUTTON)
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

        private HitTestValues HitTestCaption(IntPtr lParam)
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
                    var nearestButton = visual.FindAncestorOfType<Button>(includeSelf: true);
                    var isCaptionButton = nearestButton?.FindAncestorOfType<CaptionButtons>() is not null;
                    if (nearestButton is null || !isCaptionButton)
                        return HitTestValues.HTCLIENT;
    
                    return nearestButton.Name switch
                    {
                        CaptionButtons.PART_CloseButton => HitTestValues.HTCLOSE,
                        CaptionButtons.PART_MinimizeButton => HitTestValues.HTMINBUTTON,
                        CaptionButtons.PART_RestoreButton => HitTestValues.HTMAXBUTTON,
                        _ => HitTestValues.HTCLIENT, // CaptionButtons.PART_FullScreenButton...
                    };
                }
            }
            return HitTestValues.HTNOWHERE;
        }
    }
}
