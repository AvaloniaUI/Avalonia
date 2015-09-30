using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Collections;
using Perspex.Input;
using Perspex.Input.Raw;
using Perspex.Media;
using Perspex.MobilePlatform.Fakes;
using Perspex.Platform;

namespace Perspex.MobilePlatform
{
    class SceneComposer : IVisual
    {
        private readonly IWindowImpl _window;

        private readonly List<MobileWindow> _windows = new List<MobileWindow>();
        private readonly List<MobilePopup> _popups = new List<MobilePopup>();
        private readonly PerspexList<IVisual> _visibleVisuals = new PerspexList<IVisual>();
        private MobileWindow _activeWindow;
        private IRenderer _render;
        private InvalidationHelper _renderHelper = new InvalidationHelper();

        public SceneComposer(IWindowImpl window)
        {
            _window = window;
            _render = Platform.NativeRenderInterface.CreateRenderer(window.Handle, window.ClientSize.Width,
                window.ClientSize.Height);
            _window.SetInputRoot(new FakeInputRoot());
            _window.Paint = (rect, handle) =>
            {
                _render.Resize((int) _window.ClientSize.Width, (int) _window.ClientSize.Height);
                _render.Render(this, handle);
            };
            _window.Resized = HandleResize;

            _window.Input = DispatchInput;
            _renderHelper.Invalidated += () => _window.Invalidate(new Rect(_window.ClientSize));

        }

        private void HandleResize(Size obj)
        {
            foreach (var w in _windows)
                w.SetSize(_window.ClientSize);
        }

        MobileTopLevel GetEventTargetAndTransformEvent(RawInputEventArgs ev)
        {
            if (ev is RawKeyEventArgs)
            {
                return (MobileTopLevel)_popups.FirstOrDefault() ?? _activeWindow;
            }
            var mouseEv = ev as RawMouseEventArgs;
            if (mouseEv != null)
            {
                MobileTopLevel target = _activeWindow;
                foreach (var popup in Enumerable.Reverse(_popups))
                {
                    if (popup.Visual.Bounds.Contains(mouseEv.Position))
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

        public bool IsVisible { get; set; } = true;
        public bool ClipToBounds { get; set; }
        public Rect Bounds => new Rect(_window.ClientSize);
        
        public bool IsAttachedToVisualTree => true;
        public bool IsEffectivelyVisible => true;

        public double Opacity { get; set; } = 1;
        public Transform RenderTransform { get; set; }
        public RelativePoint TransformOrigin { get; set; }
        public IPerspexReadOnlyList<IVisual> VisualChildren => _visibleVisuals;
        public IVisual VisualParent { get; } = null;
        public int ZIndex { get; set; }

        public void Render(IDrawingContext context)
        {

        }

        public Matrix TransformToVisual(IVisual visual)
        {
            return Matrix.Identity;
        }

        public void AddWindow(MobileWindow window)
        {
            _windows.Add(window);
            window.SetSize(_window.ClientSize);
            RecalcTree();
        }

        private void RecalcTree()
        {
            _visibleVisuals.Clear();
            var topMost = _windows.LastOrDefault();
            if (topMost != null)
            {
                _visibleVisuals.Add(topMost.Visual);
                _activeWindow = topMost;
            }
            var zIndex = 1;
            foreach (var popup in _popups)
            {
                popup.Visual.ZIndex = zIndex;
                zIndex++;
                _visibleVisuals.Add(popup.Visual);
            }
            foreach(var w in _windows)
                if (_activeWindow != w)
                    w.Deactivated?.Invoke();
            _activeWindow?.Activated?.Invoke();
            _window.Invalidate(new Rect(_window.ClientSize));
        }

        public void RemoveWindow(MobileWindow window)
        {
            _windows.Remove(window);
            RecalcTree();
        }

        public void RenderRequestedBy(MobileTopLevel topLevel)
        {
            _renderHelper.Invalidate();
        }

        public void AddPopup(MobilePopup mobilePopup)
        {
            _popups.Add(mobilePopup);
            RecalcTree();
        }

        public void RemovePopup(MobilePopup mobilePopup)
        {
            _popups.Remove(mobilePopup);
            RecalcTree();
        }
    }
}
