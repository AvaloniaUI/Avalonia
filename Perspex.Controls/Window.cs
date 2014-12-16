// -----------------------------------------------------------------------
// <copyright file="Window.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
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

    public class Window : ContentControl, ILayoutRoot, IRenderRoot, ICloseable, IFocusScope
    {
        public static readonly PerspexProperty<Size> ClientSizeProperty =
            PerspexProperty.Register<Window, Size>("ClientSize");

        public static readonly PerspexProperty<string> TitleProperty = 
            PerspexProperty.Register<Window, string>("Title", "Window");

        private IWindowImpl impl;

        private Dispatcher dispatcher;

        private IRenderManager renderManager;

        private IRenderer renderer;

        private IInputManager inputManager;

        static Window()
        {
            BackgroundProperty.OverrideDefaultValue(typeof(Window), Brushes.White);
            Window.AffectsMeasure(Window.ClientSizeProperty);
        }

        public Window()
        {
            IPlatformRenderInterface renderInterface = Locator.Current.GetService<IPlatformRenderInterface>();

            this.impl = Locator.Current.GetService<IWindowImpl>();
            this.inputManager = Locator.Current.GetService<IInputManager>();
            this.LayoutManager = Locator.Current.GetService<ILayoutManager>();
            this.renderManager = Locator.Current.GetService<IRenderManager>();

            if (renderInterface == null)
            {
                throw new InvalidOperationException(
                    "Could not create an interface to the rendering subsystem: maybe no rendering subsystem was initialized?");
            }

            if (this.impl == null)
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

            this.impl.SetOwner(this);
            this.impl.Activated = this.HandleActivated;
            this.impl.Closed = this.HandleClosed;
            this.impl.Input = this.HandleInput;
            this.impl.Paint = this.HandlePaint;
            this.impl.Resized = this.HandleResized;

            Size clientSize = this.ClientSize = this.impl.ClientSize;
            this.dispatcher = Dispatcher.UIThread;
            this.renderer = renderInterface.CreateRenderer(this.impl.Handle, clientSize.Width, clientSize.Height);

            this.LayoutManager.Root = this;
            this.LayoutManager.LayoutNeeded.Subscribe(_ => this.HandleLayoutNeeded());
            this.renderManager.RenderNeeded.Subscribe(_ => this.HandleRenderNeeded());

            this.GetObservable(TitleProperty).Subscribe(s => this.impl.SetTitle(s));

            IStyler styler = Locator.Current.GetService<IStyler>();
            styler.ApplyStyles(this);
        }

        public event EventHandler Activated;

        public event EventHandler Closed;

        public Size ClientSize
        {
            get { return this.GetValue(ClientSizeProperty); }
            set { this.SetValue(ClientSizeProperty, value); }
        }

        public string Title
        {
            get { return this.GetValue(TitleProperty); }
            set { this.SetValue(TitleProperty, value); }
        }

        public ILayoutManager LayoutManager
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

        public void Show()
        {
            this.impl.Show();
            this.LayoutPass();
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
            this.dispatcher.InvokeAsync(this.LayoutPass, DispatcherPriority.Render);
        }

        private void HandleRenderNeeded()
        {
            this.dispatcher.InvokeAsync(
                () => this.impl.Invalidate(new Rect(this.ClientSize)), 
                DispatcherPriority.Render);
        }

        private void HandlePaint(Rect rect, IPlatformHandle handle)
        {
            this.renderer.Render(this, handle);
            this.renderManager.RenderFinished();
        }

        private void HandleResized(Size size)
        {
            this.ClientSize = size;
            this.renderer.Resize((int)size.Width, (int)size.Height);
            this.LayoutManager.ExecuteLayoutPass();
            this.impl.Invalidate(new Rect(this.ClientSize));
        }

        private void LayoutPass()
        {
            this.LayoutManager.ExecuteLayoutPass();
            this.impl.Invalidate(new Rect(this.ClientSize));
        }
    }
}
