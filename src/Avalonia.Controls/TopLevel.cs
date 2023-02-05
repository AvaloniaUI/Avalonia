using System;
using System.ComponentModel;
using Avalonia.Reactive;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.Utilities;
using Avalonia.Input.Platform;
using System.Linq;

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
    [TemplatePart("PART_TransparencyFallback", typeof(Border))]
    public abstract class TopLevel : ContentControl,
        IInputRoot,
        ILayoutRoot,
        IRenderRoot,
        ICloseable,
        IStyleHost,
        ILogicalRoot,
        ITextInputMethodRoot
    {
        /// <summary>
        /// Defines the <see cref="ClientSize"/> property.
        /// </summary>
        public static readonly DirectProperty<TopLevel, Size> ClientSizeProperty =
            AvaloniaProperty.RegisterDirect<TopLevel, Size>(nameof(ClientSize), o => o.ClientSize);

        /// <summary>
        /// Defines the <see cref="FrameSize"/> property.
        /// </summary>
        public static readonly DirectProperty<TopLevel, Size?> FrameSizeProperty =
            AvaloniaProperty.RegisterDirect<TopLevel, Size?>(nameof(FrameSize), o => o.FrameSize);

        /// <summary>
        /// Defines the <see cref="IInputRoot.PointerOverElement"/> property.
        /// </summary>
        public static readonly StyledProperty<IInputElement?> PointerOverElementProperty =
            AvaloniaProperty.Register<TopLevel, IInputElement?>(nameof(IInputRoot.PointerOverElement));

        /// <summary>
        /// Defines the <see cref="TransparencyLevelHint"/> property.
        /// </summary>
        public static readonly StyledProperty<WindowTransparencyLevel> TransparencyLevelHintProperty =
            AvaloniaProperty.Register<TopLevel, WindowTransparencyLevel>(nameof(TransparencyLevelHint), WindowTransparencyLevel.None);

        /// <summary>
        /// Defines the <see cref="ActualTransparencyLevel"/> property.
        /// </summary>
        public static readonly DirectProperty<TopLevel, WindowTransparencyLevel> ActualTransparencyLevelProperty =
            AvaloniaProperty.RegisterDirect<TopLevel, WindowTransparencyLevel>(nameof(ActualTransparencyLevel), 
                o => o.ActualTransparencyLevel, 
                unsetValue: WindowTransparencyLevel.None);        

        /// <summary>
        /// Defines the <see cref="TransparencyBackgroundFallbackProperty"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> TransparencyBackgroundFallbackProperty =
            AvaloniaProperty.Register<TopLevel, IBrush>(nameof(TransparencyBackgroundFallback), Brushes.White);

        /// <summary>
        /// Defines the <see cref="BackRequested"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> BackRequestedEvent = 
            RoutedEvent.Register<TopLevel, RoutedEventArgs>(nameof(BackRequested), RoutingStrategies.Bubble);

        private static readonly WeakEvent<IResourceHost, ResourcesChangedEventArgs>
            ResourcesChangedWeakEvent = WeakEvent.Register<IResourceHost, ResourcesChangedEventArgs>(
                (s, h) => s.ResourcesChanged += h,
                (s, h) => s.ResourcesChanged -= h
            );

        private readonly IInputManager? _inputManager;
        private readonly IAccessKeyHandler? _accessKeyHandler;
        private readonly IKeyboardNavigationHandler? _keyboardNavigationHandler;
        private readonly IPlatformRenderInterface? _renderInterface;
        private readonly IGlobalStyles? _globalStyles;
        private readonly IGlobalThemeVariantProvider? _applicationThemeHost;
        private readonly PointerOverPreProcessor? _pointerOverPreProcessor;
        private readonly IDisposable? _pointerOverPreProcessorSubscription;
        private readonly IDisposable? _backGestureSubscription;
        private Size _clientSize;
        private Size? _frameSize;
        private WindowTransparencyLevel _actualTransparencyLevel;
        private ILayoutManager? _layoutManager;
        private Border? _transparencyFallbackBorder;
        private TargetWeakEventSubscriber<TopLevel, ResourcesChangedEventArgs>? _resourcesChangesSubscriber;
        private IStorageProvider? _storageProvider;
        private LayoutDiagnosticBridge? _layoutDiagnosticBridge;
        
        /// <summary>
        /// Initializes static members of the <see cref="TopLevel"/> class.
        /// </summary>
        static TopLevel()
        {
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<TopLevel>(KeyboardNavigationMode.Cycle);
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
        public TopLevel(ITopLevelImpl impl, IAvaloniaDependencyResolver? dependencyResolver)
        {
            if (impl == null)
            {
                throw new InvalidOperationException(
                    "Could not create window implementation: maybe no windowing subsystem was initialized?");
            }

            PlatformImpl = impl;

            _actualTransparencyLevel = PlatformImpl.TransparencyLevel;            

            dependencyResolver = dependencyResolver ?? AvaloniaLocator.Current;

            _accessKeyHandler = TryGetService<IAccessKeyHandler>(dependencyResolver);
            _inputManager = TryGetService<IInputManager>(dependencyResolver);
            _keyboardNavigationHandler = TryGetService<IKeyboardNavigationHandler>(dependencyResolver);
            _renderInterface = TryGetService<IPlatformRenderInterface>(dependencyResolver);
            _globalStyles = TryGetService<IGlobalStyles>(dependencyResolver);
            _applicationThemeHost = TryGetService<IGlobalThemeVariantProvider>(dependencyResolver);

            Renderer = impl.CreateRenderer(this);

            if (Renderer != null)
            {
                Renderer.SceneInvalidated += SceneInvalidated;
            }
            else
            {
                // Prevent nullable error.
                Renderer = null!;
            }

            impl.SetInputRoot(this);

            impl.Closed = HandleClosed;
            impl.Input = HandleInput;
            impl.Paint = HandlePaint;
            impl.Resized = HandleResized;
            impl.ScalingChanged = HandleScalingChanged;
            impl.TransparencyLevelChanged = HandleTransparencyLevelChanged;

            _keyboardNavigationHandler?.SetOwner(this);
            _accessKeyHandler?.SetOwner(this);

            if (_globalStyles is object)
            {
                _globalStyles.GlobalStylesAdded += ((IStyleHost)this).StylesAdded;
                _globalStyles.GlobalStylesRemoved += ((IStyleHost)this).StylesRemoved;
            }
            if (_applicationThemeHost is { })
            {
                SetValue(ActualThemeVariantProperty, _applicationThemeHost.ActualThemeVariant, BindingPriority.Template);
                _applicationThemeHost.ActualThemeVariantChanged += GlobalActualThemeVariantChanged;
            }

            ClientSize = impl.ClientSize;
            FrameSize = impl.FrameSize;

            this.GetObservable(PointerOverElementProperty)
                .Select(
                    x => (x as InputElement)?.GetObservable(CursorProperty) ?? Observable.Empty<Cursor>())
                .Switch().Subscribe(cursor => PlatformImpl?.SetCursor(cursor?.PlatformImpl));

            if (((IStyleHost)this).StylingParent is IResourceHost applicationResources)
            {
                _resourcesChangesSubscriber = new TargetWeakEventSubscriber<TopLevel, ResourcesChangedEventArgs>(
                    this, static (target, _, _, e) =>
                    {
                        ((ILogical)target).NotifyResourcesChanged(e);
                    });

                ResourcesChangedWeakEvent.Subscribe(applicationResources, _resourcesChangesSubscriber);
            }

            impl.LostFocus += PlatformImpl_LostFocus;

            _pointerOverPreProcessor = new PointerOverPreProcessor(this);
            _pointerOverPreProcessorSubscription = _inputManager?.PreProcess.Subscribe(_pointerOverPreProcessor);

            if(impl.TryGetFeature<ISystemNavigationManagerImpl>() is {} systemNavigationManager)
            {
                systemNavigationManager.BackRequested += (s, e) =>
                {
                    e.RoutedEvent = BackRequestedEvent;
                    RaiseEvent(e);
                };
            }

            _backGestureSubscription = _inputManager?.PreProcess.Subscribe(e =>
            {
                bool backRequested = false;

                if (e is RawKeyEventArgs rawKeyEventArgs && rawKeyEventArgs.Type == RawKeyEventType.KeyDown)
                {
                    var keymap = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>()?.Back;

                    if (keymap != null)
                    {
                        var keyEvent = new KeyEventArgs()
                        {
                            KeyModifiers = (KeyModifiers)rawKeyEventArgs.Modifiers,
                            Key = rawKeyEventArgs.Key
                        };

                        backRequested = keymap.Any( key => key.Matches(keyEvent));
                    }
                }
                else if(e is RawPointerEventArgs pointerEventArgs)
                {
                    backRequested = pointerEventArgs.Type == RawPointerEventType.XButton1Down;
                }

                if (backRequested)
                {
                    var backRequestedEventArgs = new RoutedEventArgs(BackRequestedEvent);
                    RaiseEvent(backRequestedEventArgs);

                    e.Handled = backRequestedEventArgs.Handled;
                }
            });
        }

        /// <summary>
        /// Fired when the window is opened.
        /// </summary>
        public event EventHandler? Opened;

        /// <summary>
        /// Fired when the window is closed.
        /// </summary>
        public event EventHandler? Closed;

        /// <summary>
        /// Gets or sets the client size of the window.
        /// </summary>
        public Size ClientSize
        {
            get { return _clientSize; }
            protected set { SetAndRaise(ClientSizeProperty, ref _clientSize, value); }
        }

        /// <summary>
        /// Gets or sets the total size of the window.
        /// </summary>
        public Size? FrameSize
        {
            get { return _frameSize; }
            protected set { SetAndRaise(FrameSizeProperty, ref _frameSize, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="WindowTransparencyLevel"/> that the TopLevel should use when possible.
        /// </summary>
        public WindowTransparencyLevel TransparencyLevelHint
        {
            get { return GetValue(TransparencyLevelHintProperty); }
            set { SetValue(TransparencyLevelHintProperty, value); }
        }

        /// <summary>
        /// Gets the achieved <see cref="WindowTransparencyLevel"/> that the platform was able to provide.
        /// </summary>
        public WindowTransparencyLevel ActualTransparencyLevel
        {
            get => _actualTransparencyLevel;
            private set => SetAndRaise(ActualTransparencyLevelProperty, ref _actualTransparencyLevel, value);
        }        

        /// <summary>
        /// Gets or sets the <see cref="IBrush"/> that transparency will blend with when transparency is not supported.
        /// By default this is a solid white brush.
        /// </summary>
        public IBrush TransparencyBackgroundFallback
        {
            get => GetValue(TransparencyBackgroundFallbackProperty);
            set => SetValue(TransparencyBackgroundFallbackProperty, value);
        }

        /// <inheritdoc cref="ThemeVariantScope.RequestedThemeVariant"/>
        public ThemeVariant? RequestedThemeVariant
        {
            get => GetValue(RequestedThemeVariantProperty);
            set => SetValue(RequestedThemeVariantProperty, value);
        }

        /// <summary>
        /// Occurs when physical Back Button is pressed or a back navigation has been requested.
        /// </summary>
        public event EventHandler<RoutedEventArgs> BackRequested
        {
            add { AddHandler(BackRequestedEvent, value); }
            remove { RemoveHandler(BackRequestedEvent, value); }
        }

        public ILayoutManager LayoutManager
        {
            get
            {
                if (_layoutManager is null)
                {
                    _layoutManager = CreateLayoutManager();

                    if (_layoutManager is LayoutManager typedLayoutManager && Renderer is not null)
                    {
                        _layoutDiagnosticBridge = new LayoutDiagnosticBridge(Renderer.Diagnostics, typedLayoutManager);
                        _layoutDiagnosticBridge.SetupBridge();
                    }
                }

                return _layoutManager;
            }
        }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        public ITopLevelImpl? PlatformImpl { get; private set; }
        
        /// <summary>
        /// Gets the renderer for the window.
        /// </summary>
        public IRenderer Renderer { get; private set; }

        internal PixelPoint? LastPointerPosition => _pointerOverPreProcessor?.LastPosition;
        
        /// <summary>
        /// Gets the access key handler for the window.
        /// </summary>
        IAccessKeyHandler IInputRoot.AccessKeyHandler => _accessKeyHandler!;

        /// <summary>
        /// Gets or sets the keyboard navigation handler for the window.
        /// </summary>
        IKeyboardNavigationHandler IInputRoot.KeyboardNavigationHandler => _keyboardNavigationHandler!;

        /// <inheritdoc/>
        IInputElement? IInputRoot.PointerOverElement
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
        double ILayoutRoot.LayoutScaling => PlatformImpl?.RenderScaling ?? 1;

        /// <inheritdoc/>
        double IRenderRoot.RenderScaling => PlatformImpl?.RenderScaling ?? 1;

        IStyleHost IStyleHost.StylingParent => _globalStyles!;
        
        public IStorageProvider StorageProvider => _storageProvider
            ??= AvaloniaLocator.Current.GetService<IStorageProviderFactory>()?.CreateProvider(this)
            ?? PlatformImpl?.TryGetFeature<IStorageProvider>()
            ?? throw new InvalidOperationException("StorageProvider platform implementation is not available.");
        
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
        /// Gets the <see cref="TopLevel" /> for which the given <see cref="Visual"/> is hosted in.
        /// </summary>
        /// <param name="visual">The visual to query its TopLevel</param>
        /// <returns>The TopLevel</returns>
        public static TopLevel? GetTopLevel(Visual? visual)
        {
            return visual == null ? null : visual.VisualRoot as TopLevel;
        }
        
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TransparencyLevelHintProperty)
            {
                if (PlatformImpl != null)
                {
                    PlatformImpl.SetTransparencyLevelHint(change.GetNewValue<WindowTransparencyLevel>());
                    HandleTransparencyLevelChanged(PlatformImpl.TransparencyLevel);
                }
            }
            else if (change.Property == ActualThemeVariantProperty)
            {
                PlatformImpl?.SetFrameThemeVariant((PlatformThemeVariant?)change.GetNewValue<ThemeVariant>() ?? PlatformThemeVariant.Light);
            }
        }
        
        /// <summary>
        /// Creates the layout manager for this <see cref="TopLevel" />.
        /// </summary>
        protected virtual ILayoutManager CreateLayoutManager() => new LayoutManager(this);

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
            if (_globalStyles is object)
            {
                _globalStyles.GlobalStylesAdded -= ((IStyleHost)this).StylesAdded;
                _globalStyles.GlobalStylesRemoved -= ((IStyleHost)this).StylesRemoved;
            }
            if (_applicationThemeHost is { })
            {
                _applicationThemeHost.ActualThemeVariantChanged -= GlobalActualThemeVariantChanged;
            }

            Renderer?.Dispose();
            Renderer = null!;

            _layoutDiagnosticBridge?.Dispose();
            _layoutDiagnosticBridge = null;

            _pointerOverPreProcessor?.OnCompleted();
            _pointerOverPreProcessorSubscription?.Dispose();
            _backGestureSubscription?.Dispose();

            PlatformImpl = null;

            var logicalArgs = new LogicalTreeAttachmentEventArgs(this, this, null);
            ((ILogical)this).NotifyDetachedFromLogicalTree(logicalArgs);

            var visualArgs = new VisualTreeAttachmentEventArgs(this, this);
            OnDetachedFromVisualTreeCore(visualArgs);
            
            OnClosed(EventArgs.Empty);

            LayoutManager?.Dispose();
        }

        /// <summary>
        /// Handles a resize notification from <see cref="ITopLevelImpl.Resized"/>.
        /// </summary>
        /// <param name="clientSize">The new client size.</param>
        /// <param name="reason">The reason for the resize.</param>
        protected virtual void HandleResized(Size clientSize, PlatformResizeReason reason)
        {
            ClientSize = clientSize;
            FrameSize = PlatformImpl!.FrameSize;
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
            LayoutHelper.InvalidateSelfAndChildrenMeasure(this);
        }

        private static bool TransparencyLevelsMatch (WindowTransparencyLevel requested, WindowTransparencyLevel received)
        {
            if(requested == received)
            {
                return true;
            }
            else if(requested >= WindowTransparencyLevel.Blur && received >= WindowTransparencyLevel.Blur)
            {
                return true;
            }

            return false;
        }

        protected virtual void HandleTransparencyLevelChanged(WindowTransparencyLevel transparencyLevel)
        {
            if(_transparencyFallbackBorder != null)
            {
                if(transparencyLevel == WindowTransparencyLevel.None || 
                    TransparencyLevelHint == WindowTransparencyLevel.None || 
                    !TransparencyLevelsMatch(TransparencyLevelHint, transparencyLevel))
                {
                    _transparencyFallbackBorder.Background = TransparencyBackgroundFallback;
                }
                else
                {
                    _transparencyFallbackBorder.Background = null;
                }
            }

            ActualTransparencyLevel = transparencyLevel;
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            throw new InvalidOperationException(
                $"Control '{GetType().Name}' is a top level control and cannot be added as a child.");
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (PlatformImpl is null)
                return;

            _transparencyFallbackBorder = e.NameScope.Find<Border>("PART_TransparencyFallback");

            HandleTransparencyLevelChanged(PlatformImpl.TransparencyLevel);
        }

        /// <summary>
        /// Raises the <see cref="Opened"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnOpened(EventArgs e)
        {
            FrameSize = PlatformImpl?.FrameSize;
            Opened?.Invoke(this, e);  
        } 

        /// <summary>
        /// Raises the <see cref="Closed"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected virtual void OnClosed(EventArgs e) => Closed?.Invoke(this, e);

        /// <summary>
        /// Tries to get a service from an <see cref="IAvaloniaDependencyResolver"/>, logging a
        /// warning if not found.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="resolver">The resolver.</param>
        /// <returns>The service.</returns>
        private T? TryGetService<T>(IAvaloniaDependencyResolver resolver) where T : class
        {
            var result = resolver.GetService<T>();

            if (result == null)
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                    this,
                    "Could not create {Service} : maybe Application.RegisterServices() wasn't called?",
                    typeof(T));
            }

            return result;
        }

        /// <summary>
        /// Handles input from <see cref="ITopLevelImpl.Input"/>.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void HandleInput(RawInputEventArgs e)
        {
            if (PlatformImpl != null)
            {
                if (e is RawPointerEventArgs pointerArgs)
                {
                    pointerArgs.InputHitTestResult = this.InputHitTest(pointerArgs.Position);
                }

                _inputManager?.ProcessInput(e);
            }
            else
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                    this,
                    "PlatformImpl is null, couldn't handle input.");
            }
        }

        private void GlobalActualThemeVariantChanged(object? sender, EventArgs e)
        {
            SetValue(ActualThemeVariantProperty, ((IGlobalThemeVariantProvider)sender!).ActualThemeVariant, BindingPriority.Template);
        }

        private void SceneInvalidated(object? sender, SceneInvalidatedEventArgs e)
        {
            _pointerOverPreProcessor?.SceneInvalidated(e.DirtyRect);
        }

        void PlatformImpl_LostFocus()
        {
            var focused = (Visual?)FocusManager.Instance?.Current;
            if (focused == null)
                return;
            while (focused.VisualParent != null)
                focused = focused.VisualParent;

            if (focused == this)
                KeyboardDevice.Instance?.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);
        }

        protected override bool BypassFlowDirectionPolicies => true;

        public override void InvalidateMirrorTransform()
        {
            // Do nothing becuase TopLevel should't apply MirrorTransform on himself.
        }

        ITextInputMethodImpl? ITextInputMethodRoot.InputMethod => PlatformImpl?.TryGetFeature<ITextInputMethodImpl>();

        /// <summary>
        /// Provides layout pass timing from the layout manager to the renderer, for diagnostics purposes.
        /// </summary>
        private sealed class LayoutDiagnosticBridge : IDisposable
        {
            private readonly RendererDiagnostics _diagnostics;
            private readonly LayoutManager _layoutManager;
            private bool _isHandling;

            public LayoutDiagnosticBridge(RendererDiagnostics diagnostics, LayoutManager layoutManager)
            {
                _diagnostics = diagnostics;
                _layoutManager = layoutManager;

                diagnostics.PropertyChanged += OnDiagnosticsPropertyChanged;
            }

            public void SetupBridge()
            {
                var needsHandling = (_diagnostics.DebugOverlays & RendererDebugOverlays.LayoutTimeGraph) != 0;
                if (needsHandling != _isHandling)
                {
                    _isHandling = needsHandling;
                    _layoutManager.LayoutPassTimed = needsHandling
                        ? timing => _diagnostics.LastLayoutPassTiming = timing
                        : null;
                }
            }

            private void OnDiagnosticsPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(RendererDiagnostics.DebugOverlays))
                {
                    SetupBridge();
                }
            }

            public void Dispose()
            {
                _diagnostics.PropertyChanged -= OnDiagnosticsPropertyChanged;
                _layoutManager.LayoutPassTimed = null;
            }
        }
    }
}
