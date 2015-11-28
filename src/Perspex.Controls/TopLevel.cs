// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Perspex.Controls.Platform;
using Perspex.Controls.Primitives;
using Perspex.Input;
using Perspex.Input.Raw;
using Perspex.Layout;
using Perspex.Platform;
using Perspex.Rendering;
using Perspex.Styling;
using Perspex.Threading;

namespace Perspex.Controls
{
    /// <summary>
    /// Base class for top-level windows.
    /// </summary>
    /// <remarks>
    /// This class acts as a base for top level windows such as <see cref="Window"/> and
    /// <see cref="PopupRoot"/>. It handles scheduling layout, styling and rendering as well as
    /// tracking the window <see cref="ClientSize"/> and <see cref="IsActive"/> state.
    /// </remarks>
    public abstract class TopLevel : ContentControl, IInputRoot, ILayoutRoot, IRenderRoot, ICloseable
    {
        /// <summary>
        /// Defines the <see cref="ClientSize"/> property.
        /// </summary>
        public static readonly PerspexProperty<Size> ClientSizeProperty =
            PerspexProperty.RegisterDirect<TopLevel, Size>(nameof(ClientSize), o => o.ClientSize);

        /// <summary>
        /// Defines the <see cref="IsActive"/> property.
        /// </summary>
        public static readonly PerspexProperty<bool> IsActiveProperty =
            PerspexProperty.RegisterDirect<TopLevel, bool>(nameof(IsActive), o => o.IsActive);

        /// <summary>
        /// Defines the <see cref="IInputRoot.PointerOverElement"/> property.
        /// </summary>
        public static readonly PerspexProperty<IInputElement> PointerOverElementProperty =
            PerspexProperty.Register<TopLevel, IInputElement>(nameof(IInputRoot.PointerOverElement));

        private readonly IRenderQueueManager _renderQueueManager;
        private readonly IInputManager _inputManager;
        private readonly IAccessKeyHandler _accessKeyHandler;
        private readonly IKeyboardNavigationHandler _keyboardNavigationHandler;
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
            : this(impl, PerspexLocator.Current)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TopLevel"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific window implementation.</param>
        /// <param name="dependencyResolver">
        /// The dependency resolver to use. If null the default dependency resolver will be used.
        /// </param>
        public TopLevel(ITopLevelImpl impl, IPerspexDependencyResolver dependencyResolver)
        {
            if (impl == null)
            {
                throw new InvalidOperationException(
                    "Could not create window implementation: maybe no windowing subsystem was initialized?");
            }

            PlatformImpl = impl;

            dependencyResolver = dependencyResolver ?? PerspexLocator.Current;
            var renderInterface = TryGetService<IPlatformRenderInterface>(dependencyResolver);
            var styler = TryGetService<IStyler>(dependencyResolver);
            _accessKeyHandler = TryGetService<IAccessKeyHandler>(dependencyResolver);
            _inputManager = TryGetService<IInputManager>(dependencyResolver);
            _keyboardNavigationHandler = TryGetService<IKeyboardNavigationHandler>(dependencyResolver);
            LayoutManager = TryGetService<ILayoutManager>(dependencyResolver);
            _renderQueueManager = TryGetService<IRenderQueueManager>(dependencyResolver);
            (TryGetService<ITopLevelRenderer>(dependencyResolver) ?? new DefaultTopLevelRenderer()).Attach(this);

            PlatformImpl.SetInputRoot(this);
            PlatformImpl.Activated = HandleActivated;
            PlatformImpl.Deactivated = HandleDeactivated;
            PlatformImpl.Closed = HandleClosed;
            PlatformImpl.Input = HandleInput;
            PlatformImpl.Resized = HandleResized;

            var clientSize = ClientSize = PlatformImpl.ClientSize;

            if (LayoutManager != null)
            {
                LayoutManager.Root = this;
                LayoutManager.LayoutNeeded.Subscribe(_ => HandleLayoutNeeded());
                LayoutManager.LayoutCompleted.Subscribe(_ => HandleLayoutCompleted());
            }

            _keyboardNavigationHandler?.SetOwner(this);
            _accessKeyHandler?.SetOwner(this);
            styler?.ApplyStyles(this);

            GetObservable(ClientSizeProperty).Skip(1).Subscribe(x => PlatformImpl.ClientSize = x);
            GetObservable(PointerOverElementProperty)
                .Select(
                    x => (x as InputElement)?.GetObservable(CursorProperty) ?? Observable.Empty<Cursor>())
                .Switch().Subscribe(cursor => PlatformImpl.SetCursor(cursor?.PlatformCursor));
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
        /// Gets the layout manager for the window.
        /// </summary>
        public ILayoutManager LayoutManager
        {
            get;
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

        /// <summary>
        /// Whether an auto-size operation is in progress.
        /// </summary>
        protected bool AutoSizing
        {
            get;
            private set;
        }

        /// <summary>
        /// Translates a point from window coordinates into screen coordinates.
        /// </summary>
        /// <param name="p">The point.</param>
        /// <returns>The point in screen coordinates.</returns>
        Point IRenderRoot.TranslatePointToScreen(Point p)
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
            LayoutManager.ExecuteLayoutPass();
            PlatformImpl.Invalidate(new Rect(clientSize));
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            throw new InvalidOperationException(
                $"Control '{GetType().Name}' is a top level control and cannot be added as a child.");
        }

        /// <summary>
        /// Tries to get a service from an <see cref="IPerspexDependencyResolver"/>, throwing an
        /// exception if not found.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="resolver">The resolver.</param>
        /// <returns>The service.</returns>
        private static T TryGetService<T>(IPerspexDependencyResolver resolver) where T : class
        {
            var result = resolver.GetService<T>();

            System.Diagnostics.Debug.WriteLineIf(
                result == null,
                $"Could not create {typeof(T).Name} : maybe Application.RegisterServices() wasn't called?");

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
            _inputManager.Process(e);
        }

        /// <summary>
        /// Handles a layout request from <see cref="LayoutManager.LayoutNeeded"/>.
        /// </summary>
        private void HandleLayoutNeeded()
        {
            Dispatcher.UIThread.InvokeAsync(LayoutManager.ExecuteLayoutPass, DispatcherPriority.Render);
        }

        /// <summary>
        /// Handles a layout completion request from <see cref="LayoutManager.LayoutCompleted"/>.
        /// </summary>
        private void HandleLayoutCompleted()
        {
            _renderQueueManager?.InvalidateRender(this);
        }
    }
}
