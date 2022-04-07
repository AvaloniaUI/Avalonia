using System;

namespace Avalonia.LogicalTree;

public interface IScreenSizeProvider
{
    double GetScreenWidth();
        
    double GetScreenHeight();

    event EventHandler? ScreenSizeChanged;
}
