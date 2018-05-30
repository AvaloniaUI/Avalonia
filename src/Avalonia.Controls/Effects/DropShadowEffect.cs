using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Visuals.Effects;

namespace Avalonia.Controls.Effects
{
    class DropShadowEffect: AvaloniaObject, IEffect
    {
        private static IEffectImpl _impl;

        static DropShadowEffect()
        {
            _impl = CreateDropShadowEffect();
        }

        protected static IEffectImpl CreateDropShadowEffect()
        {
            var factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            return factory.CreateDropShadowEffect();
        }

        public IEffectImpl PlatformImpl()
        {
            return _impl;
        }
    }
}
