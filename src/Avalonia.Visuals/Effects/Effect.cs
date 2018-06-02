using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Visuals.Effects
{
    public abstract class Effect: AvaloniaObject
    {
        public event EventHandler Changed;

        protected bool _isDirty;

        public abstract IEffectImpl PlatformImpl { get; }

        protected void RaiseChanged()
        {
            _isDirty = true;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
