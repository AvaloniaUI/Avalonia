// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for top-level windows.
    /// </summary>
    /// <remarks>
    /// This class acts as a base for top level windows such as <see cref="Window"/> and
    /// <see cref="PopupRoot"/>. It handles scheduling layout, styling and rendering as well as
    /// tracking the window <see cref="ClientSize"/> and <see cref="IsActive"/> state.
    /// </remarks>
    public abstract class TopLevel : ContentControl, IInputRoot, ILayoutRoot, IRenderRoot, ICloseable, IStyleRoot
    {
        /// <summary>
        /// Defines the <see cref="ClientSize"/> property.
        /// </summary>
        public static readonly DirectProperty<TopLevel, Size> ClientSizeProperty =
            AvaloniaProperty.RegisterDirect<TopLevel, Size>(nameof(ClientSize), o => o.ClientSize);

        /// <summary>
        /// Defines the <see cref="IsActive"/> property.
        /// </summary>
        public static readonly DirectProperty<TopLevel, bool> IsActiveProperty =
            AvaloniaProperty.RegisterDirect<TopLevel, bool>(nameof(IsActive), o => o.IsActive);

        /// <summary>
        /// Defines the <see cref="IInputRoot.PointerOverElement"/> property.
        /// </summary>
        public static readonly StyledProperty<IInputElement> PointerOverElementProperty =
            AvaloniaProperty.Register<TopLevel, IInputElement>(nameof(IInputRoot.PointerOverElement));

        private readonly IRenderQueueManager _renderQueueManager;
        private readonly IInputManager _inputManager;
        private readonly IAccessKeyHandler _accessKeyHandler;
        private readonly IKeyboardNavigationHandler _keyboardNavigationHandler;
        private readonly IApplicationLifecycle _applicationLifecycle;
        private Size _clientSize;
        private bool _isActive;

        /// <summary>
        /// Initializes static members of the <see cref="TopLevel"/> class.
        /// </summary>
        static TopLevel()
        {
            AffectsMeasure(ClientSizeProperty);
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

            PlatformImpl = impl;

            dependencyResolver = dependencyResolver ?? AvaloniaLocator.Current;
            var renderInterface = TryGetService<IPlatformRenderInterface>(dependencyResolver);
            var styler = TryGetService<IStyler>(dependencyResolver);
            _accessKeyHandler = TryGetService<IAccessKeyHandler>(dependencyResolver);
            _inputManager = TryGetService<IInputManager>(dependencyResolver);
            _keyboardNavigationHandler = TryGetService<IKeyboardNavigationHandler>(dependencyResolver);
            _renderQueueManager = TryGetService<IRenderQueueManager>(dependencyResolver);
            _applicationLifecycle = TryGetService<IApplicationLifecycle>(dependencyResolver);

            (dependencyResolver.GetService<ITopLevelRenderer>() ?? new DefaultTopLevelRenderer()).Attach(this);

            PlatformImpl.SetInputRoot(this);
            PlatformImpl.Activated = HandleActivated;
            PlatformImpl.Deactivated = HandleDeactivated;
            PlatformImpl.Closed = HandleClosed;
            PlatformImpl.Input = HandleInput;
            PlatformImpl.Resized = HandleResized;
            PlatformImpl.ScalingChanged = HandleScalingChanged;
            PlatformImpl.PositionChanged = HandlePositionChanged;

            _keyboardNavigationHandler?.SetOwner(this);
            _accessKeyHandler?.SetOwner(this);
            styler?.ApplyStyles(this);

            ClientSize = PlatformImpl.ClientSize;
            this.GetObservable(ClientSizeProperty).Skip(1).Subscribe(x => PlatformImpl.ClientSize = x);
            this.GetObservable(PointerOverElementProperty)
                .Select(
                    x => (x as InputElement)?.GetObservable(CursorProperty) ?? Observable.Empty<Cursor>())
                .Switch().Subscribe(cursor => PlatformImpl.SetCursor(cursor?.PlatformCursor));

            if (_applicationLifecycle != null)
            {
                _applicationLifecycle.OnExit += OnApplicationExiting;
            }
        }

        /// <summary>
        /// Fired when the window is activated.
        /// </summary>
        public event EventHandler Activated;

        /// <summary>
        /// Fired when the window is closed.
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// Fired when the window is deactivated.
        /// </summary>
        public event EventHandler Deactivated;

        /// <summary>
        /// Fired when the window position is changed.
        /// </summary>
        public event EventHandler<PositionChangedEventArgs> PositionChanged;

        /// <summary>
        /// Gets or sets the client size of the window.
        /// </summary>
        public Size ClientSize
        {
            get { return _clientSize; }
            private set { SetAndRaise(ClientSizeProperty, ref _clientSize, value); }
        }

        /// <summary>
        /// Gets a value that indicates whether the window is active.
        /// </summary>
        public bool IsActive
        {
            get { return _isActive; }
            private set { SetAndRaise(IsActiveProperty, ref _isActive, value); }
        }

        /// <summary>
        /// Gets or sets the window position in screen coordinates.
        /// </summary>
        public Point Position
        {
            get { return PlatformImpl.Position; }
            set { PlatformImpl.Position = value; }
        }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        public ITopLevelImpl PlatformImpl
        {
            get;
        }
        
        /// <summary>
        /// Gets the window render manager.
        /// </summary>
        IRenderQueueManager IRenderRoot.RenderQueueManager => _renderQueueManager;

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
        double ILayoutRoot.LayoutScaling => PlatformImpl.Scaling;

        IStyleHost IStyleHost.StylingParent
        {
            get { return AvaloniaLocator.Current.GetService<IGlobalStyles>(); }
        }

        /// <summary>
        /// Whether an auto-size operation is in progress.
        /// </summary>
        protected bool AutoSizing
        {
            get;
            private set;
        }

        /// <inheritdoc/>
        Point IRenderRoot.PointToClient(Point p)
        {
            return PlatformImpl.PointToClient(p);
        }

        /// <inheritdoc/>
        Point IRenderRoot.PointToScreen(Point p)
        {
            return PlatformImpl.PointToScreen(p);
        }

        /// <summary>
        /// Activates the window.
        /// </summary>
        public void Activate()
        {
            PlatformImpl.Activate();
        }

        /// <summary>
        /// Begins an auto-resize operation.
        /// </summary>
        /// <returns>A disposable used to finish the operation.</returns>
        /// <remarks>
        /// When an auto-resize operation is in progress any resize events received will not be
        /// cause the new size to be written to the <see cref="Layoutable.Width"/> and
        /// <see cref="Layoutable.Height"/> properties.
        /// </remarks>
        protected IDisposable BeginAutoSizing()
        {
            AutoSizing = true;
            return Disposable.Create(() => AutoSizing = false);
        }

        /// <summary>
        /// Carries out the arrange pass of the window.
        /// </summary>
        /// <param name="finalSize">The final window size.</param>
        /// <returns>The <paramref name="finalSize"/> parameter unchanged.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            using (BeginAutoSizing())
            {
                PlatformImpl.ClientSize = finalSize;
            }

            return base.ArrangeOverride(PlatformImpl.ClientSize);
        }

        /// <summary>
        /// Handles a resize notification from <see cref="ITopLevelImpl.Resized"/>.
        /// </summary>
        /// <param name="clientSize">The new client size.</param>
        protected virtual void HandleResized(Size clientSize)
        {
            if (!AutoSizing)
            {
                Width = clientSize.Width;
                Height = clientSize.Height;
            }

            ClientSize = clientSize;
            LayoutManager.Instance.ExecuteLayoutPass();
            PlatformImpl.Invalidate(new Rect(clientSize));
        }

        /// <summary>
        /// Handles a window scaling change notification from 
        /// <see cref="ITopLevelImpl.ScalingChanged"/>.
        /// </summary>
        /// <param name="scaling">The window scaling.</param>
        protected virtual void HandleScalingChanged(double scaling)
        {
            foreach (ILayoutable control in this.GetSelfAndVisualDescendents())
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
        /// Tries to get a service from an <see cref="IAvaloniaDependencyResolver"/>, throwing an
        /// exception if not found.
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

        /// <summary>
        /// Handles an activated notification from <see cref="ITopLevelImpl.Activated"/>.
        /// </summary>
        private void HandleActivated()
        {
            Activated?.Invoke(this, EventArgs.Empty);

            var scope = this as IFocusScope;

            if (scope != null)
            {
                FocusManager.Instance.SetFocusScope(scope);
            }

            IsActive = true;
        }

        /// <summary>
        /// Handles a closed notification from <see cref="ITopLevelImpl.Closed"/>.
        /// </summary>
        private void HandleClosed()
        {
            Closed?.Invoke(this, EventArgs.Empty);
            _applicationLifecycle.OnExit -= OnApplicationExiting;
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
        /// Handles a deactivated notification from <see cref="ITopLevelImpl.Deactivated"/>.
        /// </summary>
        private void HandleDeactivated()
        {
            IsActive = false;

            Deactivated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Handles input from <see cref="ITopLevelImpl.Input"/>.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void HandleInput(RawInputEventArgs e)
        {
            _inputManager.ProcessInput(e);
        }

        /// <summary>
        /// Handles a window position change notification from 
        /// <see cref="ITopLevelImpl.PositionChanged"/>.
        /// </summary>
        /// <param name="pos">The window position.</param>
        private void HandlePositionChanged(Point pos)
        {
            PositionChanged?.Invoke(this, new PositionChangedEventArgs(pos));
        }

        /// <summary>
        /// Starts moving a window with left button being held. Should be called from left mouse button press event handler
        /// </summary>
        public void BeginMoveDrag() => PlatformImpl.BeginMoveDrag();

        /// <summary>
        /// Starts resizing a window. This function is used if an application has window resizing controls. 
        /// Should be called from left mouse button press event handler
        /// </summary>
        public void BeginResizeDrag(WindowEdge edge) => PlatformImpl.BeginResizeDrag(edge);
    }
}
