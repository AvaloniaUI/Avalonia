using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Metadata;
using Avalonia.Platform;
using static Avalonia.Controls.Platform.IWin32OptionsTopLevelImpl;

namespace Avalonia.Controls
{
    /// <summary>
    /// Set of Win32 specific properties and events that allow deeper customization of the application per platform.
    /// </summary>
    public class Win32Properties
    {
        public static readonly AttachedProperty<Win32WindowCornerHints> WindowCornerHintProperty =
            AvaloniaProperty.RegisterAttached<Win32Properties, Window, Win32WindowCornerHints>("WindowCornerHint");

        public static void SetWindowCornerHint(Window obj, Win32WindowCornerHints value) => obj.SetValue(WindowCornerHintProperty, value);
        public static Win32WindowCornerHints GetWindowCornerHint(Window obj) => obj.GetValue(WindowCornerHintProperty);

        public delegate (uint style, uint exStyle) CustomWindowStylesCallback(uint style, uint exStyle);
        public delegate IntPtr CustomWndProcHookCallback(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled);

        /// <summary>
        /// Adds a callback to set the window's style.
        /// </summary>
        /// <param name="topLevel">The window implementation</param>
        /// <param name="callback">The callback</param>
        public static void AddWindowStylesCallback(TopLevel topLevel, CustomWindowStylesCallback? callback)
        {
            if (topLevel.PlatformImpl is IWin32OptionsTopLevelImpl toplevelImpl)
            {
                toplevelImpl.WindowStylesCallback += callback;
            }
        }

        /// <summary>
        /// Removes a callback to set the window's style.
        /// </summary>
        /// <param name="topLevel">The window implementation</param>
        /// <param name="callback">The callback</param>
        public static void RemoveWindowStylesCallback(TopLevel topLevel, CustomWindowStylesCallback? callback)
        {
            if (topLevel.PlatformImpl is IWin32OptionsTopLevelImpl toplevelImpl)
            {
                toplevelImpl.WindowStylesCallback -= callback;
            }
        }

        /// <summary>
        /// Adds a custom callback for the window's WndProc
        /// </summary>
        /// <param name="topLevel">The window</param>
        /// <param name="callback">The callback</param>
        public static void AddWndProcHookCallback(TopLevel topLevel, CustomWndProcHookCallback? callback)
        {
            if (topLevel.PlatformImpl is IWin32OptionsTopLevelImpl toplevelImpl)
            {
                toplevelImpl.WndProcHookCallback += callback;
            }
        }

        /// <summary>
        /// Removes a custom callback for the window's WndProc
        /// </summary>
        /// <param name="topLevel">The window</param>
        /// <param name="callback">The callback</param>
        public static void RemoveWndProcHookCallback(TopLevel topLevel, CustomWndProcHookCallback? callback)
        {
            if (topLevel.PlatformImpl is IWin32OptionsTopLevelImpl toplevelImpl)
            {
                toplevelImpl.WndProcHookCallback -= callback;
            }
        }

        static Win32Properties()
        {
            WindowCornerHintProperty.Changed.AddClassHandler<Window>((window, e) =>
            {
                if (window.PlatformImpl is IWin32OptionsTopLevelImpl toplevelImpl)
                {
                    toplevelImpl.SetWindowCornerHints(e.GetNewValue<Win32WindowCornerHints>());
                }
            });
        }
    }
}
