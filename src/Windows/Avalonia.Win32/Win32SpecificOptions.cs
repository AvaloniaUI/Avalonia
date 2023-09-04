using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using static Avalonia.Win32.IWin32OptionsTopLevelImpl;

namespace Avalonia.Win32
{
    public class Win32SpecificOptions
    {
        /// <summary>
        /// Sets a callback to set the window's style.
        /// </summary>
        /// <param name="topLevelImpl">The top level's <see cref="ITopLevelImpl"/> implementation</param>
        /// <param name="callback">The callback</param>
        public static void SetWindowStylesCallback(ITopLevelImpl topLevelImpl, CustomWindowStylesCallback? callback)
        {
            if(topLevelImpl is IWin32OptionsTopLevelImpl toplevelImpl)
            {
                toplevelImpl.WindowStylesCallback = callback;
            }
        }

        /// <summary>
        /// Sets a custom callback for the window's WndProc
        /// </summary>
        /// <param name="topLevelImpl"></param>
        /// <param name="callback"></param>
        public static void SetWndProcHookCallback(ITopLevelImpl topLevelImpl, CustomWndProcHookCallback? callback)
        {
            if (topLevelImpl is IWin32OptionsTopLevelImpl toplevelImpl)
            {
                toplevelImpl.WndProcHookCallback = callback;
            }
        }
    }
}
