using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform
{
    [PrivateApi]
    public interface IWin32OptionsTopLevelImpl : ITopLevelImpl
    {
        public delegate (uint style, uint exStyle) CustomWindowStylesCallback(uint style, uint exStyle);
        public delegate IntPtr CustomWndProcHookCallback(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled);

        /// <summary>
        /// Gets or sets a callback to set the window styles. 
        /// </summary>
        public CustomWindowStylesCallback? WindowStylesCallback { get; set; }

        /// <summary>
        /// Gets or sets a custom callback for the window's WndProc
        /// </summary>
        public CustomWndProcHookCallback? WndProcHookCallback { get; set; }
    }
}
