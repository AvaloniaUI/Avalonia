using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Determines how a <see cref="Window"/> will size itself to fit its content.
    /// </summary>
    [Flags]
    public enum SizeToContent
    {
        /// <summary>
        /// The window will not automatically size itself to fit its content.
        /// </summary>
        Manual = 0,

        /// <summary>
        /// The window will size itself horizontally to fit its content.
        /// </summary>
        Width = 1,

        /// <summary>
        /// The window will size itself vertically to fit its content.
        /// </summary>
        Height = 2,

        /// <summary>
        /// The window will size itself horizontally and vertically to fit its content.
        /// </summary>
        WidthAndHeight = 3,
    }

    /// <summary>
    /// Determines system decorations (title bar, border, etc) for a <see cref="Window"/>
    /// </summary>
    public enum SystemDecorations
    {
        /// <summary>
        /// No decorations
        /// </summary>
        None = 0,

        /// <summary>
        /// Window border without titlebar
        /// </summary>
        BorderOnly = 1,

        /// <summary>
        /// Fully decorated (default)
        /// </summary>
        Full = 2
    }

    /// <summary>
    /// A top-level window.
    /// </summary>
    public class Window : WindowBase, IStyleable, IFocusScope, ILayoutRoot
    {
        private readonly List<(Window child, bool isDialog)> _children = new List<(Window, bool)>();
        private bool _isExtendedIntoWindowDecorations;
        private Thickness _windowDecorationMargin;
        private Thickness _offScreenMargin;

        /// <summary>
        /// Defines the <see cref="SizeToContent"/> property.
        /// </summary>
        public static readonly StyledProperty<SizeToContent> SizeToContentProperty =
            AvaloniaProperty.Register<Window, SizeToContent>(nameof(SizeToContent));

        /// <summary>
        /// Defines the <see cref="ExtendClientAreaToDecorationsHint"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ExtendClientAreaToDecorationsHintProperty =
            AvaloniaProperty.Register<Window, bool>(nameof(ExtendClientAreaToDecorationsHint), false);

        public static readonly StyledProperty<ExtendClientAreaChromeHints> ExtendClientAreaChromeHintsProperty =
            AvaloniaProperty.Register<Window, ExtendClientAreaChromeHints>(nameof(ExtendClientAreaChromeHints), ExtendClientAreaChromeHints.Default);

        public static readonly StyledProperty<double> ExtendClientAreaTitleBarHeightHintProperty =
            AvaloniaProperty.Register<Window, double>(nameof(ExtendClientAreaTitleBarHeightHint), -1);

        /// <summary>
        /// Defines the <see cref="IsExtendedIntoWindowDecorations"/> property.
        /// </summary>
        public static readonly DirectProperty<Window, bool> IsExtendedIntoWindowDecorationsProperty =
            AvaloniaProperty.RegisterDirect<Window, bool>(nameof(IsExtendedIntoWindowDecorations),
                o => o.IsExtendedIntoWindowDecorations,
                unsetValue: false);

        /// <summary>
        /// Defines the <see cref="WindowDecorationMargin"/> property.
        /// </summary>
        public static readonly DirectProperty<Window, Thickness> WindowDecorationMarginProperty =
            AvaloniaProperty.RegisterDirect<Window, Thickness>(nameof(WindowDecorationMargin),
                o => o.WindowDecorationMargin);

        public static readonly DirectProperty<Window, Thickness> OffScreenMarginProperty =
            AvaloniaProperty.RegisterDirect<Window, Thickness>(nameof(OffScreenMargin),
                o => o.OffScreenMargin);

        /// <summary>
        /// Defines the <see cref="SystemDecorations"/> property.
        /// </summary>
        public static readonly StyledProperty<SystemDecorations> SystemDecorationsProperty =
            AvaloniaProperty.Register<Window, SystemDecorations>(nameof(SystemDecorations), SystemDecorations.Full);

        /// <summary>
        /// Defines the <see cref="ShowActivated"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowActivatedProperty =
            AvaloniaProperty.Register<Window, bool>(nameof(ShowActivated), true);

        /// <summary>
        /// Enables or disables the taskbar icon
        /// </summary>
        public static readonly StyledProperty<bool> ShowInTaskbarProperty =
            AvaloniaProperty.Register<Window, bool>(nameof(ShowInTaskbar), true);

        /// <summary>
        /// Represents the current window state (normal, minimized, maximized)
        /// </summary>
        public static readonly StyledProperty<WindowState> WindowStateProperty =
            AvaloniaProperty.Register<Window, WindowState>(nameof(WindowState));

        /// <summary>
        /// Defines the <see cref="Title"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> TitleProperty =
            AvaloniaProperty.Register<Window, string?>(nameof(Title), "Window");

        /// <summary>
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly StyledProperty<WindowIcon?> IconProperty =
            AvaloniaProperty.Register<Window, WindowIcon?>(nameof(Icon));

        /// <summary>
        /// Defines the <see cref="WindowStartupLocation"/> property.
        /// </summary>
        public static readonly DirectProperty<Window, WindowStartupLocation> WindowStartupLocationProperty =
            AvaloniaProperty.RegisterDirect<Window, WindowStartupLocation>(
                nameof(WindowStartupLocation),
                o => o.WindowStartupLocation,
                (o, v) => o.WindowStartupLocation = v);

        public static readonly StyledProperty<bool> CanResizeProperty =
            AvaloniaProperty.Register<Window, bool>(nameof(CanResize), true);

        /// <summary>
        /// Routed event that can be used for global tracking of window destruction
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> WindowClosedEvent =
            RoutedEvent.Register<Window, RoutedEventArgs>("WindowClosed", RoutingStrategies.Direct);

        /// <summary>
        /// Routed event that can be used for global tracking of opening windows
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> WindowOpenedEvent =
            RoutedEvent.Register<Window, RoutedEventArgs>("WindowOpened", RoutingStrategies.Direct);
        private object? _dialogResult;
        private readonly Size _maxPlatformClientSize;
        private WindowStartupLocation _windowStartupLocation;
        private bool _shown;
        private bool _showingAsDialog;

        /// <summary>
        /// Initializes static members of the <see cref="Window"/> class.
        /// </summary>
        static Window()
        {
            BackgroundProperty.OverrideDefaultValue(typeof(Window), Brushes.White);
            TitleProperty.Changed.AddClassHandler<Window>((s, e) => s.PlatformImpl?.SetTitle((string?)e.NewValue));
            ShowInTaskbarProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.ShowTaskbarIcon((bool)e.NewValue!));

            IconProperty.Changed.AddClassHandler<Window>((s, e) => s.PlatformImpl?.SetIcon(((WindowIcon?)e.NewValue)?.PlatformImpl));

            CanResizeProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.CanResize((bool)e.NewValue!));

            WindowStateProperty.Changed.AddClassHandler<Window>(
                (w, e) => { if (w.PlatformImpl != null) w.PlatformImpl.WindowState = (WindowState)e.NewValue!; });

            ExtendClientAreaToDecorationsHintProperty.Changed.AddClassHandler<Window>(
                (w, e) => { if (w.PlatformImpl != null) w.PlatformImpl.SetExtendClientAreaToDecorationsHint((bool)e.NewValue!); });

            ExtendClientAreaChromeHintsProperty.Changed.AddClassHandler<Window>(
                (w, e) =>
                {
                    if (w.PlatformImpl != null)
                    {
                        w.PlatformImpl.SetExtendClientAreaChromeHints((ExtendClientAreaChromeHints)e.NewValue!);
                    }
                });

            ExtendClientAreaTitleBarHeightHintProperty.Changed.AddClassHandler<Window>(
                (w, e) => { if (w.PlatformImpl != null) w.PlatformImpl.SetExtendClientAreaTitleBarHeightHint((double)e.NewValue!); });

            MinWidthProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size((double)e.NewValue!, w.MinHeight), new Size(w.MaxWidth, w.MaxHeight)));
            MinHeightProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size(w.MinWidth, (double)e.NewValue!), new Size(w.MaxWidth, w.MaxHeight)));
            MaxWidthProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size(w.MinWidth, w.MinHeight), new Size((double)e.NewValue!, w.MaxHeight)));
            MaxHeightProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size(w.MinWidth, w.MinHeight), new Size(w.MaxWidth, (double)e.NewValue!)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        public Window()
            : this(PlatformManager.CreateWindow())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        /// <param name="impl">The window implementation.</param>
        public Window(IWindowImpl impl)
            : base(impl)
        {
            impl.Closing = HandleClosing;
            impl.GotInputWhenDisabled = OnGotInputWhenDisabled;
            impl.WindowStateChanged = HandleWindowStateChanged;
            _maxPlatformClientSize = PlatformImpl?.MaxAutoSizeHint ?? default(Size);
            impl.ExtendClientAreaToDecorationsChanged = ExtendClientAreaToDecorationsChanged;
            this.GetObservable(ClientSizeProperty).Skip(1).Subscribe(x => PlatformImpl?.Resize(x, PlatformResizeReason.Application));

            PlatformImpl?.ShowTaskbarIcon(ShowInTaskbar);
        }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        public new IWindowImpl? PlatformImpl => (IWindowImpl?)base.PlatformImpl;

        /// <summary>
        /// Gets a collection of child windows owned by this window.
        /// </summary>
        public IReadOnlyList<Window> OwnedWindows => _children.Select(x => x.child).ToArray();

        /// <summary>
        /// Gets or sets a value indicating how the window will size itself to fit its content.
        /// </summary>
        /// <remarks>
        /// If <see cref="SizeToContent"/> has a value other than <see cref="SizeToContent.Manual"/>,
        /// <see cref="SizeToContent"/> is automatically set to <see cref="SizeToContent.Manual"/>
        /// if a user resizes the window by using the resize grip or dragging the border.
        /// 
        /// NOTE: Because of a limitation of X11, <see cref="SizeToContent"/> will be reset on X11 to
        /// <see cref="SizeToContent.Manual"/> on any resize - including the resize that happens when
        /// the window is first shown. This is because X11 resize notifications are asynchronous and
        /// there is no way to know whether a resize came from the user or the layout system. To avoid
        /// this, consider setting <see cref="CanResize"/> to false, which will disable user resizing
        /// of the window.
        /// </remarks>
        public SizeToContent SizeToContent
        {
            get { return GetValue(SizeToContentProperty); }
            set { SetValue(SizeToContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the title of the window.
        /// </summary>
        public string? Title
        {
            get { return GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// Gets or sets if the ClientArea is Extended into the Window Decorations (chrome or border).
        /// </summary>
        public bool ExtendClientAreaToDecorationsHint
        {
            get { return GetValue(ExtendClientAreaToDecorationsHintProperty); }
            set { SetValue(ExtendClientAreaToDecorationsHintProperty, value); }
        }

        /// <summary>
        /// Gets or Sets the <see cref="Avalonia.Platform.ExtendClientAreaChromeHints"/> that control
        /// how the chrome looks when the client area is extended.
        /// </summary>
        public ExtendClientAreaChromeHints ExtendClientAreaChromeHints
        {
            get => GetValue(ExtendClientAreaChromeHintsProperty);
            set => SetValue(ExtendClientAreaChromeHintsProperty, value);
        }

        /// <summary>
        /// Gets or Sets the TitlebarHeightHint for when the client area is extended.
        /// A value of -1 will cause the titlebar to be auto sized to the OS default.
        /// Any other positive value will cause the titlebar to assume that height.
        /// </summary>
        public double ExtendClientAreaTitleBarHeightHint
        {
            get => GetValue(ExtendClientAreaTitleBarHeightHintProperty);
            set => SetValue(ExtendClientAreaTitleBarHeightHintProperty, value);
        }        

        /// <summary>
        /// Gets if the ClientArea is Extended into the Window Decorations.
        /// </summary>
        public bool IsExtendedIntoWindowDecorations
        {
            get => _isExtendedIntoWindowDecorations;
            private set => SetAndRaise(IsExtendedIntoWindowDecorationsProperty, ref _isExtendedIntoWindowDecorations, value);
        }        

        /// <summary>
        /// Gets the WindowDecorationMargin.
        /// This tells you the thickness around the window that is used by borders and the titlebar.
        /// </summary>
        public Thickness WindowDecorationMargin
        {
            get => _windowDecorationMargin;
            private set => SetAndRaise(WindowDecorationMarginProperty, ref _windowDecorationMargin, value);
        }        

        /// <summary>
        /// Gets the window margin that is hidden off the screen area.
        /// This is generally only the case on Windows when in Maximized where the window border
        /// is hidden off the screen. This Margin may be used to ensure user content doesnt overlap this space.
        /// </summary>
        public Thickness OffScreenMargin
        {
            get => _offScreenMargin;
            private set => SetAndRaise(OffScreenMarginProperty, ref _offScreenMargin, value);
        }

        /// <summary>
        /// Sets the system decorations (title bar, border, etc)
        /// </summary>
        public SystemDecorations SystemDecorations
        {
            get { return GetValue(SystemDecorationsProperty); }
            set { SetValue(SystemDecorationsProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether a window is activated when first shown. 
        /// </summary>
        public bool ShowActivated
        {
            get { return GetValue(ShowActivatedProperty); }
            set { SetValue(ShowActivatedProperty, value); }
        }

        /// <summary>
        /// Enables or disables the taskbar icon
        /// </summary>
        /// 
        public bool ShowInTaskbar
        {
            get { return GetValue(ShowInTaskbarProperty); }
            set { SetValue(ShowInTaskbarProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimized/maximized state of the window.
        /// </summary>
        public WindowState WindowState
        {
            get { return GetValue(WindowStateProperty); }
            set { SetValue(WindowStateProperty, value); }
        }

        /// <summary>
        /// Enables or disables resizing of the window.
        /// </summary>
        public bool CanResize
        {
            get { return GetValue(CanResizeProperty); }
            set { SetValue(CanResizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the icon of the window.
        /// </summary>
        public WindowIcon? Icon
        {
            get { return GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        /// <summary>
        /// Gets or sets the startup location of the window.
        /// </summary>
        public WindowStartupLocation WindowStartupLocation
        {
            get { return _windowStartupLocation; }
            set { SetAndRaise(WindowStartupLocationProperty, ref _windowStartupLocation, value); }
        }

        /// <summary>
        /// Gets or sets the window position in screen coordinates.
        /// </summary>
        public PixelPoint Position
        {
            get { return PlatformImpl?.Position ?? PixelPoint.Origin; }
            set
            {
                PlatformImpl?.Move(value);
            }
        }

        /// <summary>
        /// Starts moving a window with left button being held. Should be called from left mouse button press event handler
        /// </summary>
        public void BeginMoveDrag(PointerPressedEventArgs e) => PlatformImpl?.BeginMoveDrag(e);

        /// <summary>
        /// Starts resizing a window. This function is used if an application has window resizing controls. 
        /// Should be called from left mouse button press event handler
        /// </summary>
        public void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e) => PlatformImpl?.BeginResizeDrag(edge, e);

        /// <inheritdoc/>
        Type IStyleable.StyleKey => typeof(Window);

        /// <summary>
        /// Fired before a window is closed.
        /// </summary>
        public event EventHandler<WindowClosingEventArgs>? Closing;

        /// <summary>
        /// Closes the window.
        /// </summary>
        public void Close()
        {
            CloseCore(WindowCloseReason.WindowClosing, true);
        }

        /// <summary>
        /// Closes a dialog window with the specified result.
        /// </summary>
        /// <param name="dialogResult">The dialog result.</param>
        /// <remarks>
        /// When the window is shown with the <see cref="ShowDialog{TResult}(Window)"/>
        /// or <see cref="ShowDialog{TResult}(Window)"/> method, the
        /// resulting task will produce the <see cref="_dialogResult"/> value when the window
        /// is closed.
        /// </remarks>
        public void Close(object dialogResult)
        {
            _dialogResult = dialogResult;
            CloseCore(WindowCloseReason.WindowClosing, true);
        }

        internal void CloseCore(WindowCloseReason reason, bool isProgrammatic)
        {
            bool close = true;

            try
            {
                if (ShouldCancelClose(new WindowClosingEventArgs(reason, isProgrammatic)))
                {
                    close = false;
                }
            }
            finally
            {
                if (close)
                {
                    CloseInternal();
                }
            }
        }

        /// <summary>
        /// Handles a closing notification from <see cref="IWindowImpl.Closing"/>.
        /// <returns>true if closing is cancelled. Otherwise false.</returns>
        /// </summary>
        /// <param name="reason">The reason the window is closing.</param>
        private protected virtual bool HandleClosing(WindowCloseReason reason)
        {
            if (!ShouldCancelClose(new WindowClosingEventArgs(reason, false)))
            {
                CloseInternal();
                return false;
            }
            
            return true;
        }

        private void CloseInternal()
        {
            foreach (var (child, _) in _children.ToArray())
            {
                child.CloseInternal();
            }

            if (Owner is Window owner)
            {
                owner.RemoveChild(this);
            }

            Owner = null;

            PlatformImpl?.Dispose();

            _showingAsDialog = false;
        }

        private bool ShouldCancelClose(WindowClosingEventArgs args)
        {
            bool canClose = true;

            if (_children.Count > 0)
            {
                var childArgs = args.CloseReason == WindowCloseReason.WindowClosing ?
                    new WindowClosingEventArgs(WindowCloseReason.OwnerWindowClosing, args.IsProgrammatic) :
                    args;

                foreach (var (child, _) in _children.ToArray())
                {
                    if (child.ShouldCancelClose(childArgs))
                    {
                        canClose = false;
                    }
                }
            }

            if (canClose)
            {
                OnClosing(args);

                return args.Cancel;
            }

            return true;
        }

        protected virtual void HandleWindowStateChanged(WindowState state)
        {
            WindowState = state;

            if (state == WindowState.Minimized)
            {
                Renderer.Stop();
            }
            else
            {
                Renderer.Start();
            }
        }

        protected virtual void ExtendClientAreaToDecorationsChanged(bool isExtended)
        {
            IsExtendedIntoWindowDecorations = isExtended;
            WindowDecorationMargin = PlatformImpl?.ExtendedMargins ?? default;
            OffScreenMargin = PlatformImpl?.OffScreenMargin ?? default;
        }

        /// <summary>
        /// Hides the window but does not close it.
        /// </summary>
        public override void Hide()
        {
            using (FreezeVisibilityChangeHandling())
            {
                if (!_shown)
                {
                    return;
                }

                Renderer?.Stop();

                if (Owner is Window owner)
                {
                    owner.RemoveChild(this);
                }

                if (_children.Count > 0)
                {
                    foreach (var child in _children.ToArray())
                    {
                        child.child.Hide();
                    }
                }

                Owner = null;
                PlatformImpl?.Hide();
                IsVisible = false;
                _shown = false;
            }
        }

        /// <summary>
        /// Shows the window.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The window has already been closed.
        /// </exception>
        public override void Show()
        {
            ShowCore(null);
        }

        protected override void IsVisibleChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (!IgnoreVisibilityChanges)
            {
                var isVisible = e.GetNewValue<bool>();

                if (_shown != isVisible)
                {
                    if(!_shown)
                    {
                        Show();
                    }
                    else
                    {
                        if (_showingAsDialog)
                        {
                            Close(false);
                        }
                        else
                        {
                            Hide();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Shows the window as a child of <paramref name="owner"/>.
        /// </summary>
        /// <param name="owner">Window that will be the owner of the shown window.</param>
        /// <exception cref="InvalidOperationException">
        /// The window has already been closed.
        /// </exception>
        public void Show(Window owner)
        {
            if (owner is null)
            {
                throw new ArgumentNullException(nameof(owner), "Showing a child window requires valid parent.");
            }

            ShowCore(owner);
        }

        private void EnsureStateBeforeShow()
        {
            if (PlatformImpl == null)
            {
                throw new InvalidOperationException("Cannot re-show a closed window.");
            }
        }
        
        private void EnsureParentStateBeforeShow(Window owner)
        {
            if (owner.PlatformImpl == null)
            {
                throw new InvalidOperationException("Cannot show a window with a closed owner.");
            }

            if (owner == this)
            {
                throw new InvalidOperationException("A Window cannot be its own owner.");
            }

            if (!owner.IsVisible)
            {
                throw new InvalidOperationException("Cannot show window with non-visible owner.");
            }
        }

        private void ShowCore(Window? owner)
        {
            using (FreezeVisibilityChangeHandling())
            {
                EnsureStateBeforeShow();

                if (owner != null)
                {
                    EnsureParentStateBeforeShow(owner);
                }

                if (_shown)
                {
                    return;
                }

                RaiseEvent(new RoutedEventArgs(WindowOpenedEvent));

                EnsureInitialized();
                ApplyStyling();
                _shown = true;
                IsVisible = true;

                var initialSize = new Size(
                    double.IsNaN(Width) ? Math.Max(MinWidth, ClientSize.Width) : Width,
                    double.IsNaN(Height) ? Math.Max(MinHeight, ClientSize.Height) : Height);

                if (initialSize != ClientSize)
                {
                    PlatformImpl?.Resize(initialSize, PlatformResizeReason.Layout);
                }

                LayoutManager.ExecuteInitialLayoutPass();

                if (PlatformImpl != null && owner?.PlatformImpl is not null)
                {
                    PlatformImpl.SetParent(owner.PlatformImpl);
                }

                Owner = owner;
                owner?.AddChild(this, false);

                SetWindowStartupLocation(owner?.PlatformImpl);

                PlatformImpl?.Show(ShowActivated, false);
                Renderer?.Start();
                OnOpened(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <param name="owner">The dialog's owner window.</param>
        /// <exception cref="InvalidOperationException">
        /// The window has already been closed.
        /// </exception>
        /// <returns>
        /// A task that can be used to track the lifetime of the dialog.
        /// </returns>
        public Task ShowDialog(Window owner)
        {
            return ShowDialog<object>(owner);
        }

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result produced by the dialog.
        /// </typeparam>
        /// <param name="owner">The dialog's owner window.</param>
        /// <returns>.
        /// A task that can be used to retrieve the result of the dialog when it closes.
        /// </returns>
        public Task<TResult> ShowDialog<TResult>(Window owner)
        {
            using (FreezeVisibilityChangeHandling())
            {
                EnsureStateBeforeShow();

                if (owner == null)
                {
                    throw new ArgumentNullException(nameof(owner));
                }

                EnsureParentStateBeforeShow(owner);

                if (_shown)
                {
                    throw new InvalidOperationException("The window is already being shown.");
                }

                RaiseEvent(new RoutedEventArgs(WindowOpenedEvent));

                EnsureInitialized();
                ApplyStyling();
                _shown = true;
                _showingAsDialog = true;
                IsVisible = true;

                var initialSize = new Size(
                    double.IsNaN(Width) ? ClientSize.Width : Width,
                    double.IsNaN(Height) ? ClientSize.Height : Height);

                if (initialSize != ClientSize)
                {
                    PlatformImpl?.Resize(initialSize, PlatformResizeReason.Layout);
                }

                LayoutManager.ExecuteInitialLayoutPass();

                var result = new TaskCompletionSource<TResult>();

                PlatformImpl?.SetParent(owner.PlatformImpl!);
                Owner = owner;
                owner.AddChild(this, true);

                SetWindowStartupLocation(owner.PlatformImpl);

                PlatformImpl?.Show(ShowActivated, true);

                Renderer?.Start();

                Observable.FromEventPattern<EventHandler, EventArgs>(
                        x => Closed += x,
                        x => Closed -= x)
                    .Take(1)
                    .Subscribe(_ =>
                    {
                        owner.Activate();
                        result.SetResult((TResult)(_dialogResult ?? default(TResult)!));
                    });

                OnOpened(EventArgs.Empty);
                return result.Task;
            }
        }

        private void UpdateEnabled()
        {
            bool isEnabled = true;

            foreach (var (_, isDialog)  in _children)
            {
                if (isDialog)
                {
                    isEnabled = false;
                    break;
                }
            }

            PlatformImpl?.SetEnabled(isEnabled);
        }

        private void AddChild(Window window, bool isDialog)
        {
            _children.Add((window, isDialog));
            UpdateEnabled();
        }

        private void RemoveChild(Window window)
        {
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                var (child, _) = _children[i];

                if (ReferenceEquals(child, window))
                {
                    _children.RemoveAt(i);
                }
            }

            UpdateEnabled();
        }

        private void OnGotInputWhenDisabled()
        {
            Window? firstDialogChild = null;

            foreach (var (child, isDialog)  in _children)
            {
                if (isDialog)
                {
                    firstDialogChild = child;
                    break;
                }
            }

            if (firstDialogChild != null)
            {
                firstDialogChild.OnGotInputWhenDisabled();
            }
            else
            {
                Activate();
            }
        }

        private void SetWindowStartupLocation(IWindowBaseImpl? owner = null)
        {
            var startupLocation = WindowStartupLocation;

            if (startupLocation == WindowStartupLocation.CenterOwner &&
                (owner is null || 
                 (Owner is Window ownerWindow && ownerWindow.WindowState == WindowState.Minimized))
                )
            {
                // If startup location is CenterOwner, but owner is null or minimized then fall back
                // to CenterScreen. This behavior is consistent with WPF.
                startupLocation = WindowStartupLocation.CenterScreen;
            }

            var scaling = owner?.DesktopScaling ?? PlatformImpl?.DesktopScaling ?? 1;

            // Use frame size, falling back to client size if the platform can't give it to us.
            var rect = FrameSize.HasValue ?
                new PixelRect(PixelSize.FromSize(FrameSize.Value, scaling)) :
                new PixelRect(PixelSize.FromSize(ClientSize, scaling));

            if (startupLocation == WindowStartupLocation.CenterScreen)
            {
                Screen? screen = null;

                if (owner is not null)
                {
                    screen = Screens.ScreenFromWindow(owner) 
                             ?? Screens.ScreenFromPoint(owner.Position);
                }

                screen ??= Screens.ScreenFromPoint(Position);

                if (screen is not null)
                {
                    Position = screen.WorkingArea.CenterRect(rect).Position;
                }
            }
            else if (startupLocation == WindowStartupLocation.CenterOwner)
            {
                var ownerSize = owner!.FrameSize ?? owner.ClientSize;
                var ownerRect = new PixelRect(
                    owner.Position,
                    PixelSize.FromSize(ownerSize, scaling));
                Position = ownerRect.CenterRect(rect).Position;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var sizeToContent = SizeToContent;
            var clientSize = ClientSize;
            var constraint = clientSize;
            var maxAutoSize = PlatformImpl?.MaxAutoSizeHint ?? Size.Infinity;

            if (MaxWidth > 0 && MaxWidth < maxAutoSize.Width)
            {
                maxAutoSize = maxAutoSize.WithWidth(MaxWidth);
            }
            if (MaxHeight > 0 && MaxHeight < maxAutoSize.Height)
            {
                maxAutoSize = maxAutoSize.WithHeight(MaxHeight);
            }

            if (sizeToContent.HasAllFlags(SizeToContent.Width))
            {
                constraint = constraint.WithWidth(maxAutoSize.Width);
            }

            if (sizeToContent.HasAllFlags(SizeToContent.Height))
            {
                constraint = constraint.WithHeight(maxAutoSize.Height);
            }

            var result = base.MeasureOverride(constraint);

            if (!sizeToContent.HasAllFlags(SizeToContent.Width))
            {
                if (!double.IsInfinity(availableSize.Width))
                {
                    result = result.WithWidth(availableSize.Width);
                }
                else
                {
                    result = result.WithWidth(clientSize.Width);
                }
            }

            if (!sizeToContent.HasAllFlags(SizeToContent.Height))
            {
                if (!double.IsInfinity(availableSize.Height))
                {
                    result = result.WithHeight(availableSize.Height);
                }
                else
                {
                    result = result.WithHeight(clientSize.Height);
                }
            }

            return result;
        }

        protected sealed override Size ArrangeSetBounds(Size size)
        {
            PlatformImpl?.Resize(size, PlatformResizeReason.Layout);
            return ClientSize;
        }

        protected sealed override void HandleClosed()
        {
            RaiseEvent(new RoutedEventArgs(WindowClosedEvent));

            base.HandleClosed();

            if (Owner is Window owner)
            {
                owner.RemoveChild(this);
            }

            Owner = null;
        }

        /// <inheritdoc/>
        protected sealed override void HandleResized(Size clientSize, PlatformResizeReason reason)
        {
            if (ClientSize != clientSize || double.IsNaN(Width) || double.IsNaN(Height))
            {
                var sizeToContent = SizeToContent;

                // If auto-sizing is enabled, and the resize came from a user resize (or the reason was
                // unspecified) then turn off auto-resizing for any window dimension that is not equal
                // to the requested size.
                if (sizeToContent != SizeToContent.Manual &&
                    CanResize &&
                    reason == PlatformResizeReason.Unspecified ||
                    reason == PlatformResizeReason.User)
                {
                    if (clientSize.Width != ClientSize.Width)
                        sizeToContent &= ~SizeToContent.Width;
                    if (clientSize.Height != ClientSize.Height)
                        sizeToContent &= ~SizeToContent.Height;
                    SizeToContent = sizeToContent;
                }

                Width = clientSize.Width;
                Height = clientSize.Height;
            }

            base.HandleResized(clientSize, reason);
        }

        /// <summary>
        /// Raises the <see cref="Closing"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// A type that derives from <see cref="Window"/>  may override <see cref="OnClosing"/>. The
        /// overridden method must call <see cref="OnClosing"/> on the base class if the
        /// <see cref="Closing"/> event needs to be raised.
        /// </remarks>
        protected virtual void OnClosing(WindowClosingEventArgs e) => Closing?.Invoke(this, e);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == SystemDecorationsProperty)
            {
                var (_, typedNewValue) = change.GetOldAndNewValue<SystemDecorations>();

                PlatformImpl?.SetSystemDecorations(typedNewValue);
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new WindowAutomationPeer(this);
        }
    }
}
