using Avalonia.Controls.ApplicationLifetimes;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;

namespace Avalonia.Tizen;

public sealed class TizenPlatformOptions
{
    public bool UseDeferredRendering { get; set; } = false;
    public bool UseGpu { get; set; } = true;
}
