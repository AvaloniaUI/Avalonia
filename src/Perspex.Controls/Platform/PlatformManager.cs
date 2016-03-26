using System;
using System.Reactive.Disposables;
using Perspex.Input;
using Perspex.Input.Raw;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Controls.Platform
{
    public static partial class PlatformManager
    {

        static IPlatformSettings GetSettings()
            => PerspexLocator.Current.GetService<IPlatformSettings>();

        static bool s_designerMode;
        private static double _designerScalingFactor = 1;

        public static IRenderTarget CreateRenderTarget(ITopLevelImpl window)
        {
            return
                new RenderTargetDecorator(
                    PerspexLocator.Current.GetService<IPlatformRenderInterface>().CreateRenderer(window.Handle), window);
        }

        public static IDisposable DesignerMode()
        {
            s_designerMode = true;
            return Disposable.Create(() => s_designerMode = false);
        }

        public static void SetDesignerScalingFactor(double factor)
        {
            _designerScalingFactor = factor;
        }

        class RenderTargetDecorator : IRenderTarget
        {
            private readonly IRenderTarget _target;
            private readonly ITopLevelImpl _window;

            public RenderTargetDecorator(IRenderTarget target, ITopLevelImpl window)
            {
                _target = target;
                _window = window;
            }

            public void Dispose() => _target.Dispose();

            public DrawingContext CreateDrawingContext()
            {
                var cs = _window.ClientSize;
                var ctx = _target.CreateDrawingContext();
                var factor = _window.Scaling;
                if (factor != 1)
                {
                    ctx.PushPostTransform(Matrix.CreateScale(factor, factor));
                    ctx.PushTransformContainer();
                }
                return ctx;
            }
        }

        public static IWindowImpl CreateWindow()
        {
            var platform = PerspexLocator.Current.GetService<IWindowingPlatform>();
            
            if (platform == null)
            {
                throw new Exception("Could not CreateWindow(): IWindowingPlatform is not registered.");
            }

            return s_designerMode ? platform.CreateEmbeddableWindow() : platform.CreateWindow();
        }

        public static IPopupImpl CreatePopup()
        {
            return PerspexLocator.Current.GetService<IWindowingPlatform>().CreatePopup();
        }
    }
}
