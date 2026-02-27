using System;
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
    public static readonly StyledProperty<double> TitleBarHeightProperty =
        AvaloniaProperty.Register<WindowDrawnDecorations, double>(nameof(TitleBarHeight), defaultValue: double.NaN);

    /// <summary>
    /// Defines the <see cref="FrameThickness"/> property.
    /// </summary>
    public static readonly StyledProperty<Thickness> FrameThicknessProperty =
        AvaloniaProperty.Register<WindowDrawnDecorations, Thickness>(nameof(FrameThickness),
            defaultValue: new Thickness(double.NaN));

    /// <summary>
    /// Defines the <see cref="ShadowThickness"/> property.
    /// </summary>
    public static readonly StyledProperty<Thickness> ShadowThicknessProperty =
        AvaloniaProperty.Register<WindowDrawnDecorations, Thickness>(nameof(ShadowThickness),
            defaultValue: new Thickness(double.NaN));

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
            defaultValue: DrawnWindowDecorationParts.All);

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
    /// Gets or sets the effective titlebar height.
    /// When NaN, falls back to <see cref="DefaultTitleBarHeight"/>.
    /// Window can override by setting a local value.
    /// </summary>
    public double TitleBarHeight
    {
        get => GetValue(TitleBarHeightProperty);
        set => SetValue(TitleBarHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the effective frame thickness.
    /// When any component is NaN, that component falls back to <see cref="DefaultFrameThickness"/>.
    /// Window can override by setting a local value.
    /// </summary>
    public Thickness FrameThickness
    {
        get => GetValue(FrameThicknessProperty);
        set => SetValue(FrameThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets the effective shadow area thickness.
    /// When any component is NaN, that component falls back to <see cref="DefaultShadowThickness"/>.
    /// Window can override by setting a local value.
    /// </summary>
    public Thickness ShadowThickness
    {
        get => GetValue(ShadowThicknessProperty);
        set => SetValue(ShadowThicknessProperty, value);
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
    /// Gets the effective titlebar height, resolving NaN to the default.
    /// Returns 0 if titlebar part is disabled.
    /// </summary>
    public double EffectiveTitleBarHeight =>
        EnabledParts.HasFlag(DrawnWindowDecorationParts.TitleBar)
            ? (double.IsNaN(TitleBarHeight) ? DefaultTitleBarHeight : TitleBarHeight)
            : 0;

    /// <summary>
    /// Gets the effective frame thickness, resolving NaN components to defaults.
    /// Returns zero if border part is disabled.
    /// </summary>
    public Thickness EffectiveFrameThickness =>
        EnabledParts.HasFlag(DrawnWindowDecorationParts.Border)
            ? ResolveThickness(FrameThickness, DefaultFrameThickness)
            : default;

    /// <summary>
    /// Gets the effective shadow thickness, resolving NaN components to defaults.
    /// Returns zero if shadow part is disabled.
    /// </summary>
    public Thickness EffectiveShadowThickness =>
        EnabledParts.HasFlag(DrawnWindowDecorationParts.Shadow)
            ? ResolveThickness(ShadowThickness, DefaultShadowThickness)
            : default;

    static WindowDrawnDecorations()
    {
        TemplateProperty.Changed.AddClassHandler<WindowDrawnDecorations>((x, _) => x.InvalidateTemplate());
        EnabledPartsProperty.Changed.AddClassHandler<WindowDrawnDecorations>((x, _) =>
        {
            x.UpdateEnabledPartsPseudoClasses();
            x.EffectiveGeometryChanged?.Invoke();
        });

        // Notify geometry changes when any thickness/height property changes
        DefaultTitleBarHeightProperty.Changed.AddClassHandler<WindowDrawnDecorations>((x, _) => x.EffectiveGeometryChanged?.Invoke());
        DefaultFrameThicknessProperty.Changed.AddClassHandler<WindowDrawnDecorations>((x, _) => x.EffectiveGeometryChanged?.Invoke());
        DefaultShadowThicknessProperty.Changed.AddClassHandler<WindowDrawnDecorations>((x, _) => x.EffectiveGeometryChanged?.Invoke());
        TitleBarHeightProperty.Changed.AddClassHandler<WindowDrawnDecorations>((x, _) => x.EffectiveGeometryChanged?.Invoke());
        FrameThicknessProperty.Changed.AddClassHandler<WindowDrawnDecorations>((x, _) => x.EffectiveGeometryChanged?.Invoke());
        ShadowThicknessProperty.Changed.AddClassHandler<WindowDrawnDecorations>((x, _) => x.EffectiveGeometryChanged?.Invoke());
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

        if (_closeButton != null)
        {
            _closeButton.Click += OnCloseButtonClick;
            WindowDecorations.SetIsHitTestVisibleInChrome(_closeButton, true);
        }
        if (_minimizeButton != null)
        {
            _minimizeButton.Click += OnMinimizeButtonClick;
            WindowDecorations.SetIsHitTestVisibleInChrome(_minimizeButton, true);
        }
        if (_maximizeButton != null)
        {
            _maximizeButton.Click += OnMaximizeButtonClick;
            WindowDecorations.SetIsHitTestVisibleInChrome(_maximizeButton, true);
        }
        if (_fullScreenButton != null)
        {
            _fullScreenButton.Click += OnFullScreenButtonClick;
            WindowDecorations.SetIsHitTestVisibleInChrome(_fullScreenButton, true);
        }
        if (_popoverCloseButton != null)
        {
            _popoverCloseButton.Click += OnCloseButtonClick;
            WindowDecorations.SetIsHitTestVisibleInChrome(_popoverCloseButton, true);
        }
        if (_popoverFullScreenButton != null)
        {
            _popoverFullScreenButton.Click += OnFullScreenButtonClick;
            WindowDecorations.SetIsHitTestVisibleInChrome(_popoverFullScreenButton, true);
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

    private void UpdateEnabledPartsPseudoClasses()
    {
        var parts = EnabledParts;
        PseudoClasses.Set(":has-shadow", parts.HasFlag(DrawnWindowDecorationParts.Shadow));
        PseudoClasses.Set(":has-border", parts.HasFlag(DrawnWindowDecorationParts.Border));
        PseudoClasses.Set(":has-titlebar", parts.HasFlag(DrawnWindowDecorationParts.TitleBar));
    }

    private static Thickness ResolveThickness(Thickness value, Thickness defaultValue)
    {
        return new Thickness(
            double.IsNaN(value.Left) ? defaultValue.Left : value.Left,
            double.IsNaN(value.Top) ? defaultValue.Top : value.Top,
            double.IsNaN(value.Right) ? defaultValue.Right : value.Right,
            double.IsNaN(value.Bottom) ? defaultValue.Bottom : value.Bottom);
    }
}
