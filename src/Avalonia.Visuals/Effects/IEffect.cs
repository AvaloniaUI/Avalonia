using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Visuals.Effects
{
    public interface IEffect: IAvaloniaObject
    {
        IEffectImpl PlatformImpl { get; }
    }
}
