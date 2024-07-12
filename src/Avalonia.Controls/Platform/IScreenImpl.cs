using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Controls.Utils;
using Avalonia.Metadata;
using Avalonia.Threading;
#pragma warning disable CS1591 // Private API doesn't require XML documentation. 

namespace Avalonia.Platform
{
    [Unstable]
    public interface IScreenImpl
    {
        int ScreenCount { get; }
        IReadOnlyList<Screen> AllScreens { get; }
        Action? Changed { get; set; }
        Screen? ScreenFromWindow(IWindowBaseImpl window);
        Screen? ScreenFromTopLevel(ITopLevelImpl topLevel);
        Screen? ScreenFromPoint(PixelPoint point);
        Screen? ScreenFromRect(PixelRect rect);
        Task<bool> RequestScreenDetails();
    }
}
