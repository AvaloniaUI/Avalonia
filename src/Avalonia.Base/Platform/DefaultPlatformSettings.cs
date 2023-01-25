using System;
using Avalonia.Input;
using Avalonia.Media;

namespace Avalonia.Platform
{
    /// <summary>
    /// A default implementation of <see cref="IPlatformSettings"/> for platforms.
    /// </summary>
    public class DefaultPlatformSettings : IPlatformSettings
    {
        public virtual Size GetTapSize(PointerType type)
        {
            return type switch
            {
                PointerType.Touch => new(10, 10),
                _ => new(4, 4),
            };
        }
        public virtual Size GetDoubleTapSize(PointerType type)
        {
            return type switch
            {
                PointerType.Touch => new(16, 16),
                _ => new(4, 4),
            };
        }
        public virtual TimeSpan GetDoubleTapTime(PointerType type) => TimeSpan.FromMilliseconds(500);

        public virtual TimeSpan HoldWaitDuration => TimeSpan.FromMilliseconds(300);
        
        public virtual PlatformColorValues GetColorValues()
        {
            return new PlatformColorValues
            {
                ThemeVariant = PlatformThemeVariant.Light
            };
        }

        public event EventHandler<PlatformColorValues>? ColorValuesChanged;

        protected void OnColorValuesChanged(PlatformColorValues colorValues)
        {
            ColorValuesChanged?.Invoke(this, colorValues);
        }
    }
}
