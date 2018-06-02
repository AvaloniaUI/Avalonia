using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;

namespace Avalonia.Visuals.Effects
{
    public interface IDropShadowEffectImpl
    {
        double OffsetX { get; set; }

        double OffsetY { get; set; }

        double Blur { get; set; }

        Color Color { get; set; }
    }
}
