// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using JetBrains.Annotations;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for top-level widgets.
    /// </summary>
    /// <remarks>
    /// This class acts as a base for top level widget.
    /// It handles scheduling layout, styling and rendering as well as
    /// tracking the widget's <see cref="ClientSize"/>.
    /// </remarks>
    public abstract class TopLevel : ContentControl,
        IInputRoot,
        ILayoutRoot,
        IRenderRoot,
        ICloseable,
        IStyleRoot,
        IWeakSubscriber<ResourcesChangedEventArgs>
    {
        /// <summary>
        /// Defines the <see cref="ClientSize"/> property.
        /// </summary>
        public static readonly DirectProperty<TopLevel, Size> ClientSizeProperty =
            AvaloniaProperty.RegisterDirect<TopLevel, Size>(nameof(ClientSize), o => o.ClientSize);

        /// <summary>
        /// Defines the <see cref="IInputRoot.PointerOverElement"/> property.
        /// </summary>
        public static readonly StyledProperty<IInputElement> PointerOverElementProperty =
            AvaloniaProperty.Register<TopLevel, IInputElement>(nameof(IInputRoot.PointerOverElement));

        private readonly IInputManager _inputManager;
        private readonly IAccessKeyHandler _accessKeyHandler;
        private readonly IKeyboardNavigationHandler _keyboardNavigationHandler;
        private readonly IApplicationLifecycle _applicationLifecycle;
        private readonly IPlatformRenderInterface _renderInterface;
        private Size _clientSize;
        private ILayoutManager _layoutManager;
        
        public IFocusManager FocusManager { get; private set; }

        /// <summary>
        /// Initializes static members of the <see cref="TopLevel"/> class.
        /// </summary>
        static TopLevel()
        {
            AffectsMeasure<TopLevel>(ClientSizeProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TopLevel"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific window implementation.</param>
        public TopLevel(ITopLevelImpl impl)
            : this(impl, AvaloniaLocator.Current)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TopLevel"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific window implementation.</param>
        /// <param name="dependencyResolver">
        /// The dependency resolver to use. If null the default dependency resolver will be used.
        /// </param>
        public TopLevel(ITopLevelImpl impl, IAvaloniaDependencyResolver dependencyResolver)
        {
            if (impl == null)
            {
                throw new InvalidOperationException(
                    "Could not create window implementation: maybe no windowing subsystem was initialized?");
            }
            FocusManager = new FocusManager(this);
            PlatformImpl = impl;

            dependencyResolver = dependencyResolver ?? AvaloniaLocator.Current;
            var styler = TryGetService<IStyler>(dependencyResolver);

            _accessKeyHandler = TryGetService<IAccessKeyHandler>(dependencyResolver);
            _inputManager = TryGetService<IInputManager>(dependencyResolver);
            _keyboardNavigationHandler = TryGetService<IKeyboardNavigationHandler>(dependencyResolver);
            _applicationLifecycle = TryGetService<IApplicationLifecycle>(dependencyResolver);
            _renderInterface = TryGetService<IPlatformRenderInterface>(dependencyResolver);

            Renderer = impl.CreateRenderer(this);

            if (Renderer != null)
            {
                Renderer.SceneInvalidated += SceneInvalidated;
            }

            impl.SetInputRoot(this);

            impl.Closed = HandleClosed;
            impl.Input = HandleInput;
            impl.Paint = HandlePaint;
            impl.Resized = HandleResized;
            impl.ScalingChanged = HandleScalingChanged;

            _keyboardNavigationHandler?.SetOwner(this);
            _accessKeyHandler?.SetOwner(this);
            styler?.ApplyStyles(this);

            ClientSize = impl.ClientSize;
            
            this.GetObservable(PointerOverElementProperty)
                .Select(
                    x => (x as InputElement)?.GetObservable(CursorProperty) ?? Observable.Empty<Cursor>())
                .Switch().Subscribe(cursor => PlatformImpl?.SetCursor(cursor?.PlatformCursor));

            if (_applicationLifecycle != null)
            {
                _applicationLifecycle.Exit += OnApplicationExiting;
            }

            if (((IStyleHost)this).StylingParent is IResourceProvider applicationResources)
            {
                WeakSubscriptionManager.Subscribe(
                    applicationResources,
                    nameof(IResourceProvider.ResourcesChanged),
                    this);
            }
        }

        /// <summary>
        /// Fired when the window is opened.
        /// </summary>
        public event EventHandler Opened;

        /// <summary>
        /// Fired when the window is closed.
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// Gets or sets the client size of the window.
        /// </summary>
        public Size ClientSize
        {
            get { return _clientSize; }
            protected set { SetAndRaise(ClientSizeProperty, ref _clientSize, value); }
        }

        public ILayoutManager LayoutManager
        {
            get
            {
                if (_layoutManager == null)
                    _layoutManager = CreateLayoutManager();
                return _layoutManager;
            }
        }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        [CanBeNull]
        public ITopLevelImpl PlatformImpl { get; private set; }
        
        /// <summary>
        /// Gets the renderer for the window.
        /// </summary>
        public IRenderer Renderer { get; private set; }

        /// <summary>
        /// Gets the access key handler for the window.
        /// </summary>
        IAccessKeyHandler IInputRoot.AccessKeyHandler => _accessKeyHandler;

        /// <summary>
        /// Gets or sets the keyboard navigation handler for the window.
        /// </summary>
        IKeyboardNavigationHandler IInputRoot.KeyboardNavigationHandler => _keyboardNavigationHandler;

        /// <summary>
        /// Gets or sets the input element that the pointer is currently over.
        /// </summary>
        IInputElement IInputRoot.PointerOverElement
        {
            get { return GetValue(PointerOverElementProperty); }
            set { SetValue(PointerOverElementProperty, value); }
        }

        /// <inheritdoc/>
        IMouseDevice IInputRoot.MouseDevice => PlatformImpl?.MouseDevice;

        void IWeakSubscriber<ResourcesChangedEventArgs>.OnEvent(object sender, ResourcesChangedEventArgs e)
        {
            ((ILogical)this).NotifyResourcesChanged(e);
        }

        /// <summary>
        /// Gets or sets a value indicating whether access keys are shown in the window.
        /// </summary>
        bool IInputRoot.ShowAccessKeys
        {
            get { return GetValue(AccessText.ShowAccessKeyProperty); }
            set { SetValue(AccessText.ShowAccessKeyProperty, value); }
        }

        /// <inheritdoc/>
        Size ILayoutRoot.MaxClientSize => Size.Infinity;

        /// <inheritdoc/>
        double ILayoutRoot.LayoutScaling => PlatformImpl?.Scaling ?? 1;

        /// <inheritdoc/>
        double IRenderRoot.RenderScaling => PlatformImpl?.Scaling ?? 1;

        IStyleHost IStyleHost.StylingParent
        {
            get { return AvaloniaLocator.Current.GetService<IGlobalStyles>(); }
        }

        IRenderTarget IRenderRoot.CreateRenderTarget() => CreateRenderTarget();

        /// <inheritdoc/>
        protected virtual IRenderTarget CreateRenderTarget()
        {
            if(PlatformImpl == null)
                throw new InvalidOperationException("Can't create render target, PlatformImpl is null (might be already disposed)");
            return _renderInterface.CreateRenderTarget(PlatformImpl.Surfaces);
        }

        /// <inheritdoc/>
        void IRenderRoot.Invalidate(Rect rect)
        {
            PlatformImpl?.Invalidate(rect);
        }
        
        /// <inheritdoc/>
        Point IRenderRoot.PointToClient(PixelPoint p)
        {
            return PlatformImpl?.PointToClient(p) ?? default;
        }

        /// <inheritdoc/>
        PixelPoint IRenderRoot.PointToScreen(Point p)
        {
            return PlatformImpl?.PointToScreen(p) ?? default;
        }
        
        /// <summary>
        /// Creates the layout manager for this <see cref="TopLevel" />.
        /// </summary>
        protected virtual ILayoutManager CreateLayoutManager() => new LayoutManager();

        /// <summary>
        /// Handles a paint notification from <see cref="ITopLevelImpl.Resized"/>.
        /// </summary>
        /// <param name="rect">The dirty area.</param>
        protected virtual void HandlePaint(Rect rect)
        {
            Renderer?.Paint(rect);
        }

        /// <summary>
        /// Handles a closed notification from <see cref="ITopLevelImpl.Closed"/>.
        /// </summary>
        protected virtual void HandleClosed()
        {
            PlatformImpl = null;

            Closed?.Invoke(this, EventArgs.Empty);
            Renderer?.Dispose();
            Renderer = null;
            _applicationLifecycle.Exit -= OnApplicationExiting;
        }

        /// <summary>
        /// Handles a resize notification from <see cref="ITopLevelImpl.Resized"/>.
        /// </summary>
        /// <param name="clientSize">The new client size.</param>
        protected virtual void HandleResized(Size clientSize)
        {
            ClientSize = clientSize;
            Width = clientSize.Width;
            Height = clientSize.Height;
            LayoutManager.ExecuteLayoutPass();
            Renderer?.Resized(clientSize);
        }

        /// <summary>
        /// Handles a window scaling change notification from 
        /// <see cref="ITopLevelImpl.ScalingChanged"/>.
        /// </summary>
        /// <param name="scaling">The window scaling.</param>
        protected virtual void HandleScalingChanged(double scaling)
        {
            foreach (ILayoutable control in this.GetSelfAndVisualDescendants())
            {
                control.InvalidateMeasure();
            }
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            throw new InvalidOperationException(
                $"Control '{GetType().Name}' is a top level control and cannot be added as a child.");
        }

        /// <summary>
        /// Raises the <see cref="Opened"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnOpened(EventArgs e) => Opened?.Invoke(this, e);

        /// <summary>
        /// Tries to get a service from an <see cref="IAvaloniaDependencyResolver"/>, logging a
        /// warning if not found.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="resolver">The resolver.</param>
        /// <returns>The service.</returns>
        private T TryGetService<T>(IAvaloniaDependencyResolver resolver) where T : class
        {
            var result = resolver.GetService<T>();

            if (result == null)
            {
                Logger.Warning(
                    LogArea.Control,
                    this,
                    "Could not create {Service} : maybe Application.RegisterServices() wasn't called?",
                    typeof(T));
            }

            return result;
        }

        private void OnApplicationExiting(object sender, EventArgs args)
        {
            HandleApplicationExiting();
        }

        /// <summary>
        /// Handles the application exiting, either from the last window closing, or a call to <see cref="IApplicationLifecycle.Exit"/>.
        /// </summary>
        protected virtual void HandleApplicationExiting()
        {
        }

        /// <summary>
        /// Handles input from <see cref="ITopLevelImpl.Input"/>.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void HandleInput(RawInputEventArgs e)
        {
            _inputManager.ProcessInput(e, FocusManager.FocusedElement);
        }

        private void SceneInvalidated(object sender, SceneInvalidatedEventArgs e)
        {
            (this as IInputRoot).MouseDevice.SceneInvalidated(this, e.DirtyRect);
        }
    }
}
