using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
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

            if (_extendTitleBarHint >= 0)
            {
                border_thickness.top = (int)(_extendedMargins.Top * Scaling);
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

        protected virtual IntPtr CustomCaptionProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool callDwp)
        {
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
                        if(WindowState == WindowState.FullScreen)
                        {
                            return (IntPtr)HitTestValues.HTCLIENT;
                        }
                        var hittestResult = HitTestNCA(hWnd, wParam, lParam);

                        lRet = (IntPtr)hittestResult;

                        uint timestamp = unchecked((uint)GetMessageTime());

                        if (hittestResult == HitTestValues.HTCAPTION)
                        {
                            var position = PointToClient(PointFromLParam(lParam));

                            var visual = (_owner as Window).Renderer.HitTestFirst(position, _owner as Window, x =>
                            {
                                if (x is IInputElement ie && !ie.IsHitTestVisible)
                                {
                                    return false;
                                }

                                return true;
                            });

                            if (visual != null)
                            {
                                hittestResult = HitTestValues.HTCLIENT;
                                lRet = (IntPtr)hittestResult;
                            }
                        }

                        if (hittestResult != HitTestValues.HTNOWHERE)
                        {
                            callDwp = false;
                        }
                    }
                    break;
            }

            return lRet;
        }
    }
}
