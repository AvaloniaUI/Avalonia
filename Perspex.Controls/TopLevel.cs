// -----------------------------------------------------------------------
// <copyright file="TopLevel.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using Perspex.Input;
    using Perspex.Input.Raw;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.Platform;
    using Perspex.Rendering;
    using Perspex.Styling;
    using Perspex.Threading;
    using Splat;

    public abstract class TopLevel : ContentControl, ILayoutRoot, IRenderRoot, ICloseable, IFocusScope
    {
        public static readonly PerspexProperty<Size> ClientSizeProperty =
            PerspexProperty.Register<TopLevel, Size>("ClientSize");

        private Dispatcher dispatcher;

        private IRenderManager renderManager;

        private IRenderer renderer;

        private IInputManager inputManager;

        private bool autoSizing;

        static TopLevel()
        {
            TopLevel.AffectsMeasure(TopLevel.ClientSizeProperty);
        }

        public TopLevel(ITopLevelImpl impl)
        {
            IPlatformRenderInterface renderInterface = Locator.Current.GetService<IPlatformRenderInterface>();

            this.PlatformImpl = impl;
            this.inputManager = Locator.Current.GetService<IInputManager>();
            this.LayoutManager = Locator.Current.GetService<ILayoutManager>();
            this.renderManager = Locator.Current.GetService<IRenderManager>();

            if (renderInterface == null)
            {
                throw new InvalidOperationException(
                    "Could not create an interface to the rendering subsystem: maybe no rendering subsystem was initialized?");
            }

            if (this.PlatformImpl == null)
            {
                throw new InvalidOperationException(
                    "Could not create window implementation: maybe no windowing subsystem was initialized?");
            }

            if (this.inputManager == null)
            {
                throw new InvalidOperationException(
                    "Could not create input manager: maybe Application.RegisterServices() wasn't called?");
            }

            if (this.LayoutManager == null)
            {
                throw new InvalidOperationException(
                    "Could not create layout manager: maybe Application.RegisterServices() wasn't called?");
            }

            if (this.renderManager == null)
            {
                throw new InvalidOperationException(
                    "Could not create render manager: maybe Application.RegisterServices() wasn't called?");
            }

            this.PlatformImpl.SetOwner(this);
            this.PlatformImpl.Activated = this.HandleActivated;
            this.PlatformImpl.Closed = this.HandleClosed;
            this.PlatformImpl.Input = this.HandleInput;
            this.PlatformImpl.Paint = this.HandlePaint;
            this.PlatformImpl.Resized = this.HandleResized;
            
            Size clientSize = this.ClientSize = this.PlatformImpl.ClientSize;

            this.dispatcher = Dispatcher.UIThread;
            this.renderer = renderInterface.CreateRenderer(this.PlatformImpl.Handle, clientSize.Width, clientSize.Height);

            this.LayoutManager.Root = this;
            this.LayoutManager.LayoutNeeded.Subscribe(_ => this.HandleLayoutNeeded());
            this.renderManager.RenderNeeded.Subscribe(_ => this.HandleRenderNeeded());

            IStyler styler = Locator.Current.GetService<IStyler>();
            styler.ApplyStyles(this);

            this.GetObservable(ClientSizeProperty).Skip(1).Subscribe(x => this.PlatformImpl.ClientSize = x);
        }

        public event EventHandler Activated;

        public event EventHandler Closed;

        public Size ClientSize
        {
            get { return this.GetValue(ClientSizeProperty); }
            set { this.SetValue(ClientSizeProperty, value); }
        }

        public ILayoutManager LayoutManager
        {
            get;
            private set;
        }

        public ITopLevelImpl PlatformImpl
        {
            get;
            private set;
        }

        IRenderer IRenderRoot.Renderer
        {
            get { return this.renderer; }
        }

        IRenderManager IRenderRoot.RenderManager
        {
            get { return this.renderManager; }
        }

        Point IRenderRoot.TranslatePointToScreen(Point p)
        {
            return this.PlatformImpl.PointToScreen(p);
        }

        protected IDisposable BeginAutoSizing()
        {
            this.autoSizing = true;
            return Disposable.Create(() => this.autoSizing = false);
        }

        protected void ExecuteLayoutPass()
        {
            this.LayoutManager.ExecuteLayoutPass();

            using (this.BeginAutoSizing())
            {
                this.ClientSize = new Size(
                    double.IsNaN(this.Width) ? this.DesiredSize.Value.Width : this.ClientSize.Width,
                    double.IsNaN(this.Height) ? this.DesiredSize.Value.Height : this.ClientSize.Height);
            }

            this.PlatformImpl.Invalidate(new Rect(this.ClientSize));
        }

        private void HandleActivated()
        {
            if (this.Activated != null)
            {
                this.Activated(this, EventArgs.Empty);
            }

            FocusManager.Instance.SetFocusScope(this);
        }

        private void HandleClosed()
        {
            if (this.Closed != null)
            {
                this.Closed(this, EventArgs.Empty);
            }
        }

        private void HandleInput(RawInputEventArgs e)
        {
            this.inputManager.Process(e);
        }

        private void HandleLayoutNeeded()
        {
            this.dispatcher.InvokeAsync(this.ExecuteLayoutPass, DispatcherPriority.Render);
        }

        private void HandleRenderNeeded()
        {
            this.dispatcher.InvokeAsync(
                () => this.PlatformImpl.Invalidate(new Rect(this.ClientSize)), 
                DispatcherPriority.Render);
        }

        private void HandlePaint(Rect rect, IPlatformHandle handle)
        {
            this.renderer.Render(this, handle);
            this.renderManager.RenderFinished();
        }

        private void HandleResized(Size clientSize)
        {
            if (!this.autoSizing)
            {
                this.Width = clientSize.Width;
                this.Height = clientSize.Height;
            }

            this.ClientSize = clientSize;
            this.renderer.Resize((int)clientSize.Width, (int)clientSize.Height);
            this.LayoutManager.ExecuteLayoutPass();
            this.PlatformImpl.Invalidate(new Rect(clientSize));
        }
    }
}
