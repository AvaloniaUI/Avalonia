using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Platform;
using static Avalonia.Controls.Platform.IWin32OptionsTopLevelImpl;

namespace Avalonia.Controls.Platform
{
    [Unstable]
    public class Win32SpecificOptions
    {
        /// <summary>
        /// Sets a callback to set the window's style.
        /// </summary>
        /// <param name="topLevel">The window implementation</param>
        /// <param name="callback">The callback</param>
        public static void SetWindowStylesCallback(TopLevel topLevel, CustomWindowStylesCallback? callback)
        {
            if (topLevel.PlatformImpl is IWin32OptionsTopLevelImpl toplevelImpl)
            {
                toplevelImpl.WindowStylesCallback = callback;
            }
        }

        /// <summary>
        /// Sets a custom callback for the window's WndProc
        /// </summary>
        /// <param name="topLevel">The window</param>
        /// <param name="callback"></param>
        public static void SetWndProcHookCallback(TopLevel topLevel, CustomWndProcHookCallback? callback)
        {
            if (topLevel.PlatformImpl is IWin32OptionsTopLevelImpl toplevelImpl)
            {
                toplevelImpl.WndProcHookCallback = callback;
            }
        }
    }
}
