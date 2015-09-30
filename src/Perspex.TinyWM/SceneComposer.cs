using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Collections;
using Perspex.Input;
using Perspex.Input.Raw;
using Perspex.Media;
using Perspex.TinyWM.Fakes;
using Perspex.Platform;
using Perspex.Rendering;

namespace Perspex.TinyWM
{
    class SceneComposer
    {
        private readonly IWindowImpl _window;

        private readonly List<WindowImpl> _windows = new List<WindowImpl>();
        private readonly List<PopupImpl> _popups = new List<PopupImpl>();
        private WindowImpl _activeWindow;
        private IRenderTarget _render;
        private InvalidationHelper _renderHelper = new InvalidationHelper();

        public SceneComposer(IWindowImpl window)
        {
            _window = window;
            _render = WindowManager.NativeRenderInterface.CreateRenderer(window.Handle, window.ClientSize.Width,
                window.ClientSize.Height);
            _window.SetInputRoot(new FakeInputRoot());
            _window.Paint = (rect) =>
            {
                _render.Resize((int)_window.ClientSize.Width, (int)_window.ClientSize.Height);
                using (var ctx = _render.CreateDrawingContext())
                    Render(ctx);
            };
            _window.Resized = HandleResize;
            _window.Closed += () =>
            {
                foreach (var tl in _windows.Cast<TopLevelImpl>().Concat(_popups))
                    tl?.Closed();
            };
            _window.Input = DispatchInput;
            _renderHelper.Invalidated += () => _window.Invalidate(new Rect(_window.ClientSize));

        }

        private void HandleResize(Size obj)
        {
            foreach (var w in _windows)
                w.SetSize(_window.ClientSize);
        }

        void Render(IDrawingContext ctx)
        {
            if (_activeWindow == null)
                return;
            ctx.Render(_activeWindow.TopLevel);
            foreach (var popup in _popups)
                if (popup.TopLevel != null)
                    using (ctx.PushTransform(Matrix.CreateTranslation(popup.X, popup.Y)))
                        ctx.Render(popup.TopLevel);
        }


        TopLevelImpl GetEventTargetAndTransformEvent(RawInputEventArgs ev)
        {
            if (ev is RawKeyEventArgs)
            {
                return (TopLevelImpl)_popups.FirstOrDefault() ?? _activeWindow;
            }
            var mouseEv = ev as RawMouseEventArgs;
            if (mouseEv != null)
            {
                TopLevelImpl target = _activeWindow;
                foreach (var popup in Enumerable.Reverse(_popups))
                {
                    if (popup.Bounds.Contains(mouseEv.Position))
                    {
                        mouseEv.Position = new Point(mouseEv.Position.X - popup.Y, mouseEv.Position.Y - popup.Y);
                        target = popup;
                        break;
                    }
                }
                mouseEv.Root = target?.InputRoot;
                return target;
            }
            return _activeWindow;
        }

        private void DispatchInput(RawInputEventArgs ev)
        {
            if(_activeWindow == null)
                return;
            GetEventTargetAndTransformEvent(ev)?.Input?.Invoke(ev);
        }

        public void AddWindow(WindowImpl window)
        {
            _windows.Add(window);
            window.SetSize(_window.ClientSize);
            HandleActivation();
        }

        private void HandleActivation()
        {
            _activeWindow = _windows.LastOrDefault();
            foreach(var w in _windows)
                if (_activeWindow != w)
                    w.Deactivated?.Invoke();
            _activeWindow?.Activated?.Invoke();
            _window.Invalidate(new Rect(_window.ClientSize));
        }

        public void RemoveWindow(WindowImpl window)
        {
            _windows.Remove(window);
            HandleActivation();
        }

        public void RenderRequestedBy(TopLevelImpl topLevel)
        {
            _renderHelper.Invalidate();
        }

        public void AddPopup(PopupImpl mobilePopup)
        {
            _popups.Add(mobilePopup);
            HandleActivation();
        }

        public void RemovePopup(PopupImpl mobilePopup)
        {
            _popups.Remove(mobilePopup);
            HandleActivation();
        }
    }
}
