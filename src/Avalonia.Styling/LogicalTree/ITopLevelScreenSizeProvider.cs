using System;

namespace Avalonia.LogicalTree;

public interface ITopLevelScreenSizeProvider
{
    IScreenSizeProvider? GetScreenSizeProvider();

    event EventHandler? ScreenSizeProviderChanged;
}
