using System;
using Avalonia.Automation;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Reactive;
using Avalonia.Styling;

namespace Avalonia.Controls.Chrome;

/// <summary>
/// Manages client-side window decorations (app-drawn window frame).
/// This is a logical element that holds the decorations template and properties.
/// TopLevelHost extracts overlay/underlay/popover visuals from the template content
/// and inserts them into its own visual tree.
/// </summary>
[PseudoClasses(":normal", ":maximized", ":fullscreen", ":has-shadow", ":has-border", ":has-titlebar")]
public class WindowDrawnDecorations : StyledElement
{
    // Template part names for caption buttons
    internal const string PART_CloseButton = "PART_CloseButton";
    internal const string PART_MinimizeButton = "PART_MinimizeButton";
    internal const string PART_MaximizeButton = "PART_MaximizeButton";
    internal const string PART_FullScreenButton = "PART_FullScreenButton";
    // Popover caption buttons (separate names to avoid name scope conflicts)
    internal const string PART_PopoverCloseButton = "PART_PopoverCloseButton";
    internal const string PART_PopoverFullScreenButton = "PART_PopoverFullScreenButton";
    // Titlebar panel
    internal const string PART_TitleBar = "PART_TitleBar";

    /// <summary>
    /// Defines the <see cref="Template"/> property.
    /// </summary>
    public static readonly StyledProperty<IWindowDrawnDecorationsTemplate?> TemplateProperty =
        AvaloniaProperty.Register<WindowDrawnDecorations, IWindowDrawnDecorationsTemplate?>(nameof(Template));

    /// <summary>
    /// Defines the <see cref="DefaultTitleBarHeight"/> property.
    /// </summary>
    public static readonly StyledProperty<double> DefaultTitleBarHeightProperty =
        AvaloniaProperty.Register<WindowDrawnDecorations, double>(nameof(DefaultTitleBarHeight));

    /// <summary>
    /// Defines the <see cref="DefaultFrameThickness"/> property.
    /// </summary>
    public static readonly StyledProperty<Thickness> DefaultFrameThicknessProperty =
        AvaloniaProperty.Register<WindowDrawnDecorations, Thickness>(nameof(DefaultFrameThickness));

    /// <summary>
    /// Defines the <see cref="DefaultShadowThickness"/> property.
    /// </summary>
    public static readonly StyledProperty<Thickness> DefaultShadowThicknessProperty =
        AvaloniaProperty.Register<WindowDrawnDecorations, Thickness>(nameof(DefaultShadowThickness));

    /// <summary>
    /// Defines the <see cref="TitleBarHeight"/> property.
    /// </summary>
    public static readonly DirectProperty<WindowDrawnDecorations, double> TitleBarHeightProperty =
        AvaloniaProperty.RegisterDirect<WindowDrawnDecorations, double>(
            nameof(TitleBarHeight),
            o => o.TitleBarHeight);

    /// <summary>
    /// Defines the <see cref="FrameThickness"/> property.
    /// </summary>
    public static readonly DirectProperty<WindowDrawnDecorations, Thickness> FrameThicknessProperty =
        AvaloniaProperty.RegisterDirect<WindowDrawnDecorations, Thickness>(
            nameof(FrameThickness),
            o => o.FrameThickness);

    /// <summary>
    /// Defines the <see cref="ShadowThickness"/> property.
    /// </summary>
    public static readonly DirectProperty<WindowDrawnDecorations, Thickness> ShadowThicknessProperty =
        AvaloniaProperty.RegisterDirect<WindowDrawnDecorations, Thickness>(
            nameof(ShadowThickness),
            o => o.ShadowThickness);

    /// <summary>
    /// Defines the <see cref="HasShadow"/> property.
    /// </summary>
    public static readonly DirectProperty<WindowDrawnDecorations, bool> HasShadowProperty =
        AvaloniaProperty.RegisterDirect<WindowDrawnDecorations, bool>(
            nameof(HasShadow),
            o => o.HasShadow);

    /// <summary>
    /// Defines the <see cref="HasBorder"/> property.
    /// </summary>
    public static readonly DirectProperty<WindowDrawnDecorations, bool> HasBorderProperty =
        AvaloniaProperty.RegisterDirect<WindowDrawnDecorations, bool>(
            nameof(HasBorder),
            o => o.HasBorder);

    /// <summary>
    /// Defines the <see cref="HasTitleBar"/> property.
    /// </summary>
    public static readonly DirectProperty<WindowDrawnDecorations, bool> HasTitleBarProperty =
        AvaloniaProperty.RegisterDirect<WindowDrawnDecorations, bool>(
            nameof(HasTitleBar),
            o => o.HasTitleBar);

    /// <summary>
    /// Defines the <see cref="Title"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<WindowDrawnDecorations, string?>(nameof(Title));

    /// <summary>
    /// Defines the <see cref="EnabledParts"/> property.
    /// </summary>
    internal static readonly StyledProperty<DrawnWindowDecorationParts> EnabledPartsProperty =
        AvaloniaProperty.Register<WindowDrawnDecorations, DrawnWindowDecorationParts>(nameof(EnabledParts),
            defaultValue: DrawnWindowDecorationParts.None);

    private IWindowDrawnDecorationsTemplate? _appliedTemplate;
    private INameScope? _templateNameScope;
    private Button? _closeButton;
    private Button? _minimizeButton;
    private Button? _maximizeButton;
    private Button? _fullScreenButton;
    private Button? _popoverCloseButton;
    private Button? _popoverFullScreenButton;
    private IDisposable? _windowSubscriptions;
    private Window? _hostWindow;
    private double _titleBarHeightOverride = -1;

    /// <summary>
    /// Raised when any property affecting the effective geometry changes
    /// (effective titlebar height, frame thickness, or shadow thickness).
    /// </summary>
    internal event Action? EffectiveGeometryChanged;

    /// <summary>
    /// Gets or sets the decorations template.
    /// </summary>
    public IWindowDrawnDecorationsTemplate? Template
    {
        get => GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the theme-set default titlebar height.
    /// </summary>
    public double DefaultTitleBarHeight
    {
        get => GetValue(DefaultTitleBarHeightProperty);
        set => SetValue(DefaultTitleBarHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the theme-set default frame thickness.
    /// </summary>
    public Thickness DefaultFrameThickness
    {
        get => GetValue(DefaultFrameThicknessProperty);
        set => SetValue(DefaultFrameThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets the theme-set default shadow thickness.
    /// </summary>
    public Thickness DefaultShadowThickness
    {
        get => GetValue(DefaultShadowThicknessProperty);
        set => SetValue(DefaultShadowThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets the titlebar height override.
    /// When NaN, falls back to <see cref="DefaultTitleBarHeight"/>.
    /// </summary>
    internal double TitleBarHeightOverride
    {
        get => _titleBarHeightOverride;
        set { _titleBarHeightOverride = value; UpdateEffectiveGeometry(); }
    }

    /// <summary>
    /// Gets or sets the frame thickness override.
    /// When set, takes precedence over <see cref="DefaultFrameThickness"/>.
    /// </summary>
    internal Thickness? FrameThicknessOverride
    {
        get;
        set
        {
            field = value;
            UpdateEffectiveGeometry();
        }
    }

    /// <summary>
    /// Gets or sets the shadow thickness override.
    /// When set, takes precedence over <see cref="DefaultShadowThickness"/>.
    /// </summary>
    internal Thickness? ShadowThicknessOverride
    {
        get;
        set
        {
            field = value;
            UpdateEffectiveGeometry();
        }
    }

    /// <summary>
    /// Gets or sets the window title displayed in the decorations.
    /// </summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets which decoration parts are enabled.
    /// Set by Window based on platform capabilities and user preferences.
    /// </summary>
    internal DrawnWindowDecorationParts EnabledParts
    {
        get => GetValue(EnabledPartsProperty);
        set => SetValue(EnabledPartsProperty, value);
    }

    /// <summary>
    /// Gets the built template content.
    /// </summary>
    public WindowDrawnDecorationsContent? Content { get; private set; }

    /// <summary>
    /// Gets the effective titlebar height, resolving NaN override to the default.
    /// Returns 0 if titlebar part is disabled.
    /// </summary>
    public double TitleBarHeight
    {
        get;
        private set => SetAndRaise(TitleBarHeightProperty, ref field, value);
    }

    /// <summary>
    /// Gets the effective frame thickness.
    /// Uses FrameThicknessOverride if explicitly set, otherwise DefaultFrameThickness.
    /// Returns zero if border part is disabled.
    /// </summary>
    public Thickness FrameThickness
    {
        get;
        private set => SetAndRaise(FrameThicknessProperty, ref field, value);
    }

    /// <summary>
    /// Gets the effective shadow thickness.
    /// Uses ShadowThicknessOverride if explicitly set, otherwise DefaultShadowThickness.
    /// Returns zero if shadow part is disabled.
    /// </summary>
    public Thickness ShadowThickness
    {
        get;
        private set => SetAndRaise(ShadowThicknessProperty, ref field, value);
    }

    /// <summary>
    /// Gets a value indicating whether the shadow decoration part is enabled.
    /// </summary>
    public bool HasShadow
    {
        get;
        private set => SetAndRaise(HasShadowProperty, ref field, value);
    }

    /// <summary>
    /// Gets a value indicating whether the border decoration part is enabled.
    /// </summary>
    public bool HasBorder
    {
        get;
        private set => SetAndRaise(HasBorderProperty, ref field, value);
    }

    /// <summary>
    /// Gets a value indicating whether the title bar decoration part is enabled.
    /// </summary>
    public bool HasTitleBar
    {
        get;
        private set => SetAndRaise(HasTitleBarProperty, ref field, value);
    }

    static WindowDrawnDecorations()
    {
        TemplateProperty.Changed.AddClassHandler<WindowDrawnDecorations>((x, _) => x.InvalidateTemplate());
        EnabledPartsProperty.Changed.AddClassHandler<WindowDrawnDecorations>((x, _) =>
        {
            x.UpdateEnabledPartsPseudoClasses();
            x.UpdateEffectiveGeometry();
        });

        DefaultTitleBarHeightProperty.Changed.AddClassHandler<WindowDrawnDecorations>((x, _) => x.UpdateEffectiveGeometry());
        DefaultFrameThicknessProperty.Changed.AddClassHandler<WindowDrawnDecorations>((x, _) => x.UpdateEffectiveGeometry());
        DefaultShadowThicknessProperty.Changed.AddClassHandler<WindowDrawnDecorations>((x, _) => x.UpdateEffectiveGeometry());
    }

    /// <summary>
    /// Applies the template if it has changed.
    /// </summary>
    internal void ApplyTemplate()
    {
        var template = Template;
        if (template == _appliedTemplate)
            return;

        // Clean up old content
        if (Content != null)
        {
            DetachCaptionButtons();
            LogicalChildren.Remove(Content);
            ((ISetLogicalParent)Content).SetParent(null);
            Content = null;
            _templateNameScope = null;
        }

        _appliedTemplate = template;

        if (template == null)
            return;

        var result = template.Build();
        Content = result.Result;
        _templateNameScope = result.NameScope;

        if (Content != null)
        {
            TemplatedControl.ApplyTemplatedParent(Content, this);
            LogicalChildren.Add(Content);
            ((ISetLogicalParent)Content).SetParent(this);
        }

        AttachCaptionButtons();
    }

    /// <summary>
    /// Attaches to the specified window for caption button interactions and state tracking.
    /// </summary>
    internal void Attach(Window window)
    {
        if (_hostWindow == window)
            return;

        Detach();
        _hostWindow = window;

        _windowSubscriptions = new CompositeDisposable
        {
            window.GetObservable(Window.TitleProperty)
                .Subscribe(title => SetCurrentValue(TitleProperty, title)),
            window.GetObservable(Window.CanMaximizeProperty)
                .Subscribe(_ =>
                {
                    UpdateMaximizeButtonState();
                    UpdateFullScreenButtonState();
                }),
            window.GetObservable(Window.CanMinimizeProperty)
                .Subscribe(_ => UpdateMinimizeButtonState()),
            window.GetObservable(Window.WindowStateProperty)
                .Subscribe(state =>
                {
                    PseudoClasses.Set(":normal", state == WindowState.Normal);
                    PseudoClasses.Set(":maximized", state == WindowState.Maximized);
                    PseudoClasses.Set(":fullscreen", state == WindowState.FullScreen);
                    UpdateMaximizeButtonState();
                    UpdateMinimizeButtonState();
                    UpdateFullScreenButtonState();
                }),
        };

        UpdateMaximizeButtonState();
        UpdateMinimizeButtonState();
        UpdateFullScreenButtonState();
    }

    /// <summary>
    /// Detaches from the current window.
    /// </summary>
    internal void Detach()
    {
        _windowSubscriptions?.Dispose();
        _windowSubscriptions = null;
        _hostWindow = null;
    }

    private void InvalidateTemplate()
    {
        _appliedTemplate = null;
    }

    private void AttachCaptionButtons()
    {
        if (_templateNameScope == null)
            return;

        _closeButton = _templateNameScope.Find<Button>(PART_CloseButton);
        _minimizeButton = _templateNameScope.Find<Button>(PART_MinimizeButton);
        _maximizeButton = _templateNameScope.Find<Button>(PART_MaximizeButton);
        _fullScreenButton = _templateNameScope.Find<Button>(PART_FullScreenButton);
        _popoverCloseButton = _templateNameScope.Find<Button>(PART_PopoverCloseButton);
        _popoverFullScreenButton = _templateNameScope.Find<Button>(PART_PopoverFullScreenButton);

        var titleBar = _templateNameScope.Find<Control>(PART_TitleBar);
        if (titleBar != null)
        {
            AutomationProperties.SetIsControlElementOverride(titleBar, true);
            AutomationProperties.SetAutomationId(titleBar, "AvaloniaTitleBar");
            AutomationProperties.SetName(titleBar, "TitleBar");
        }
        
        if (_closeButton != null)
        {
            AutomationProperties.SetAutomationId(_closeButton, "Close");
            AutomationProperties.SetName(_closeButton, "Close");
            _closeButton.Click += OnCloseButtonClick;
        }

        if (_minimizeButton != null)
        {
            AutomationProperties.SetAutomationId(_minimizeButton, "Minimize");
            AutomationProperties.SetName(_minimizeButton, "Minimize");
            _minimizeButton.Click += OnMinimizeButtonClick;
        }

        if (_maximizeButton != null)
        {
            _maximizeButton.Click += OnMaximizeButtonClick;
            AutomationProperties.SetAutomationId(_maximizeButton, "Maximize");
            AutomationProperties.SetName(_maximizeButton, "Maximize");
        }

        if (_fullScreenButton != null)
        {
            _fullScreenButton.Click += OnFullScreenButtonClick;
            AutomationProperties.SetAutomationId(_fullScreenButton, "Fullscreen");
            AutomationProperties.SetName(_fullScreenButton, "Fullscreen");
        }

        if (_popoverCloseButton != null)
        {
            _popoverCloseButton.Click += OnCloseButtonClick;
            AutomationProperties.SetAutomationId(_popoverCloseButton, "FullscreenClose");
            AutomationProperties.SetName(_popoverCloseButton, "Close");
        }

        if (_popoverFullScreenButton != null)
        {
            _popoverFullScreenButton.Click += OnFullScreenButtonClick;
            AutomationProperties.SetAutomationId(_popoverFullScreenButton, "ExitFullscreen");
            AutomationProperties.SetName(_popoverFullScreenButton, "ExitFullscreen");
        }
    }

    private void DetachCaptionButtons()
    {
        if (_closeButton != null)
            _closeButton.Click -= OnCloseButtonClick;
        if (_minimizeButton != null)
            _minimizeButton.Click -= OnMinimizeButtonClick;
        if (_maximizeButton != null)
            _maximizeButton.Click -= OnMaximizeButtonClick;
        if (_fullScreenButton != null)
            _fullScreenButton.Click -= OnFullScreenButtonClick;
        if (_popoverCloseButton != null)
            _popoverCloseButton.Click -= OnCloseButtonClick;
        if (_popoverFullScreenButton != null)
            _popoverFullScreenButton.Click -= OnFullScreenButtonClick;

        _closeButton = null;
        _minimizeButton = null;
        _maximizeButton = null;
        _fullScreenButton = null;
        _popoverCloseButton = null;
        _popoverFullScreenButton = null;
    }

    private void OnCloseButtonClick(object? sender, Interactivity.RoutedEventArgs e)
    {
        _hostWindow?.Close();
        e.Handled = true;
    }

    private void OnMinimizeButtonClick(object? sender, Interactivity.RoutedEventArgs e)
    {
        if (_hostWindow != null)
            _hostWindow.WindowState = WindowState.Minimized;
        e.Handled = true;
    }

    private void OnMaximizeButtonClick(object? sender, Interactivity.RoutedEventArgs e)
    {
        if (_hostWindow != null)
            _hostWindow.WindowState = _hostWindow.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        e.Handled = true;
    }

    private void OnFullScreenButtonClick(object? sender, Interactivity.RoutedEventArgs e)
    {
        if (_hostWindow != null)
            _hostWindow.WindowState = _hostWindow.WindowState == WindowState.FullScreen
                ? WindowState.Normal
                : WindowState.FullScreen;
        e.Handled = true;
    }

    private void UpdateMaximizeButtonState()
    {
        if (_maximizeButton == null)
            return;
        _maximizeButton.IsEnabled = _hostWindow?.WindowState switch
        {
            WindowState.Maximized or WindowState.FullScreen => _hostWindow.CanResize,
            WindowState.Normal => _hostWindow.CanMaximize,
            _ => true
        };
    }

    private void UpdateMinimizeButtonState()
    {
        if (_minimizeButton == null)
            return;
        _minimizeButton.IsEnabled = _hostWindow?.CanMinimize ?? true;
    }

    private void UpdateFullScreenButtonState()
    {
        if (_fullScreenButton == null)
            return;
        _fullScreenButton.IsEnabled = _hostWindow?.WindowState == WindowState.FullScreen
            ? _hostWindow.CanResize
            : _hostWindow?.CanMaximize ?? true;
    }

    private void UpdateEffectiveGeometry()
    {
        TitleBarHeight = EnabledParts.HasFlag(DrawnWindowDecorationParts.TitleBar)
            ? (TitleBarHeightOverride == -1 ? DefaultTitleBarHeight : TitleBarHeightOverride)
            : 0;

        FrameThickness = EnabledParts.HasFlag(DrawnWindowDecorationParts.Border)
            ? (FrameThicknessOverride ?? DefaultFrameThickness)
            : default;

        ShadowThickness = EnabledParts.HasFlag(DrawnWindowDecorationParts.Shadow)
            ? (ShadowThicknessOverride ?? DefaultShadowThickness)
            : default;

        EffectiveGeometryChanged?.Invoke();
    }

    private void UpdateEnabledPartsPseudoClasses()
    {
        var parts = EnabledParts;
        var hasShadow = parts.HasFlag(DrawnWindowDecorationParts.Shadow);
        var hasBorder = parts.HasFlag(DrawnWindowDecorationParts.Border);
        var hasTitleBar = parts.HasFlag(DrawnWindowDecorationParts.TitleBar);
        HasShadow = hasShadow;
        HasBorder = hasBorder;
        HasTitleBar = hasTitleBar;
        PseudoClasses.Set(":has-shadow", hasShadow);
        PseudoClasses.Set(":has-border", hasBorder);
        PseudoClasses.Set(":has-titlebar", hasTitleBar);
    }
}
