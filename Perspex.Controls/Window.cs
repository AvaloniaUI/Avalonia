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

    public class Window : ContentControl, ILayoutRoot, IRenderRoot, ICloseable
    {
        public static readonly PerspexProperty<Size> ClientSizeProperty =
            PerspexProperty.Register<Window, Size>("ClientSize");

        public static readonly PerspexProperty<string> TitleProperty = 
            PerspexProperty.Register<Window, string>("Title", "Window");

        private IWindowImpl impl;

        private Dispatcher dispatcher;

        private IRenderer renderer;

        private IInputManager inputManager;

        public event EventHandler Activated;

        public event EventHandler Closed;

        static Window()
        {
            BackgroundProperty.OverrideDefaultValue(typeof(Window), Brushes.White);
        }

        public Window()
        {
            IPlatformRenderInterface renderInterface = Locator.Current.GetService<IPlatformRenderInterface>();

            this.impl = Locator.Current.GetService<IWindowImpl>();
            this.inputManager = Locator.Current.GetService<IInputManager>();

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

            this.impl.SetOwner(this);
            this.impl.Activated += this.HandleActivated;
            this.impl.Closed += this.HandleClosed;
            this.impl.Input += this.HandleInput;

            Size clientSize = this.ClientSize = this.impl.ClientSize;
            this.dispatcher = Dispatcher.UIThread;
            this.renderer = renderInterface.CreateRenderer(this.impl.Handle, clientSize.Width, clientSize.Height);

            this.LayoutManager = new LayoutManager(this);
            this.LayoutManager.LayoutNeeded.Subscribe(_ => this.HandleLayoutNeeded());

            this.RenderManager = new RenderManager();
            this.RenderManager.RenderNeeded.Subscribe(_ => this.HandleRenderNeeded());

            this.GetObservable(TitleProperty).Subscribe(s => this.impl.SetTitle(s));

            IStyler styler = Locator.Current.GetService<IStyler>();
            styler.ApplyStyles(this);
        }

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

        public IRenderManager RenderManager
        {
            get;
            private set;
        }

        public void Show()
        {
            this.impl.Show();
            this.LayoutPass();
        }

        private void HandleActivated(object sender, EventArgs e)
        {
            if (this.Activated != null)
            {
                this.Activated(this, EventArgs.Empty);
            }
        }

        private void HandleClosed(object sender, EventArgs e)
        {
            if (this.Closed != null)
            {
                this.Closed(this, EventArgs.Empty);
            }
        }

        private void HandleInput(object sender, RawInputEventArgs e)
        {
            this.inputManager.Process(e);
        }

        private void HandleLayoutNeeded()
        {
            this.dispatcher.InvokeAsync(this.LayoutPass, DispatcherPriority.Render);
        }

        private void HandleRenderNeeded()
        {
            this.dispatcher.InvokeAsync(this.RenderVisualTree, DispatcherPriority.Render);
        }

        private void LayoutPass()
        {
            this.LayoutManager.ExecuteLayoutPass();
            this.renderer.Render(this);
            this.RenderManager.RenderFinished();
        }

        private void RenderVisualTree()
        {
            if (!this.LayoutManager.LayoutQueued)
            {
                this.renderer.Render(this);
                this.RenderManager.RenderFinished();
            }
        }
    }
}
