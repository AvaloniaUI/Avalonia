using System;
using Avalonia.Controls.Embedding;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;

namespace Avalonia.Web
{
    internal class WebEmbeddableControlRoot : EmbeddableControlRoot
    {
        class SplashScreenCloseCustomDrawingOperation : ICustomDrawOperation
        {
            private bool _hasRendered;
            private Action _onFirstRender;

            public SplashScreenCloseCustomDrawingOperation(Action onFirstRender)
            {
                _onFirstRender = onFirstRender;
            }

            public Rect Bounds => Rect.Empty;

            public bool HasRendered => _hasRendered;

            public void Dispose()
            {
                
            }

            public bool Equals(ICustomDrawOperation? other)
            {
                return false;
            }

            public bool HitTest(Point p)
            {
                return false;
            }

            public void Render(IDrawingContextImpl context)
            {
                _hasRendered = true;
                _onFirstRender();
            }
        }

        public WebEmbeddableControlRoot(ITopLevelImpl impl, Action onFirstRender) : base(impl)
        {
            _splashCloseOp = new SplashScreenCloseCustomDrawingOperation(() =>
            {
                _splashCloseOp = null;
                onFirstRender();
            });
        }

        private SplashScreenCloseCustomDrawingOperation? _splashCloseOp;

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (_splashCloseOp != null)
            {
                context.Custom(_splashCloseOp);
            }
        }
    }
}
