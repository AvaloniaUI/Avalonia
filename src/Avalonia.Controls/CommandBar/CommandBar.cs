using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// A command bar that provides primary commands displayed inline and secondary commands
    /// accessible via an overflow menu.
    /// </summary>
    [TemplatePart("PART_OverflowButton",    typeof(Button))]
    [TemplatePart("PART_OverflowPopup",    typeof(Popup))]
    [TemplatePart("PART_OverflowPresenter", typeof(ItemsControl))]
    [TemplatePart("PART_ContentPresenter",  typeof(Control))]
    [TemplatePart("PART_PrimaryCommands",   typeof(ItemsControl))]
    public class CommandBar : TemplatedControl
    {
        /// <summary>
        /// Defines the <see cref="PrimaryCommands"/> property.
        /// </summary>
        public static readonly StyledProperty<IList<ICommandBarElement>?> PrimaryCommandsProperty =
            AvaloniaProperty.Register<CommandBar, IList<ICommandBarElement>?>(nameof(PrimaryCommands));

        /// <summary>
        /// Defines the <see cref="SecondaryCommands"/> property.
        /// </summary>
        public static readonly StyledProperty<IList<ICommandBarElement>?> SecondaryCommandsProperty =
            AvaloniaProperty.Register<CommandBar, IList<ICommandBarElement>?>(nameof(SecondaryCommands));

        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> ContentProperty =
            ContentControl.ContentProperty.AddOwner<CommandBar>();

        /// <summary>
        /// Defines the <see cref="DefaultLabelPosition"/> property.
        /// </summary>
        public static readonly StyledProperty<CommandBarDefaultLabelPosition> DefaultLabelPositionProperty =
            AvaloniaProperty.Register<CommandBar, CommandBarDefaultLabelPosition>(nameof(DefaultLabelPosition), CommandBarDefaultLabelPosition.Bottom);

        /// <summary>
        /// Defines the <see cref="IsDynamicOverflowEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsDynamicOverflowEnabledProperty =
            AvaloniaProperty.Register<CommandBar, bool>(nameof(IsDynamicOverflowEnabled));

        /// <summary>
        /// Defines the <see cref="OverflowButtonVisibility"/> property.
        /// </summary>
        public static readonly StyledProperty<CommandBarOverflowButtonVisibility> OverflowButtonVisibilityProperty =
            AvaloniaProperty.Register<CommandBar, CommandBarOverflowButtonVisibility>(nameof(OverflowButtonVisibility), CommandBarOverflowButtonVisibility.Auto);

        /// <summary>
        /// Defines the <see cref="IsOpen"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsOpenProperty =
            AvaloniaProperty.Register<CommandBar, bool>(nameof(IsOpen));

        /// <summary>
        /// Defines the <see cref="IsSticky"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsStickyProperty =
            AvaloniaProperty.Register<CommandBar, bool>(nameof(IsSticky));

        /// <summary>
        /// Defines the <see cref="ItemWidthBottom"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ItemWidthBottomProperty =
            AvaloniaProperty.Register<CommandBar, double>(nameof(ItemWidthBottom), defaultValue: 70d);

        /// <summary>
        /// Defines the <see cref="ItemWidthRight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ItemWidthRightProperty =
            AvaloniaProperty.Register<CommandBar, double>(nameof(ItemWidthRight), defaultValue: 102d);

        /// <summary>
        /// Defines the <see cref="ItemWidthCollapsed"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ItemWidthCollapsedProperty =
            AvaloniaProperty.Register<CommandBar, double>(nameof(ItemWidthCollapsed), defaultValue: 42d);

        private bool _hasSecondaryCommands;
        /// <summary>
        /// Defines the <see cref="HasSecondaryCommands"/> property.
        /// </summary>
        public static readonly DirectProperty<CommandBar, bool> HasSecondaryCommandsProperty =
            AvaloniaProperty.RegisterDirect<CommandBar, bool>(
                nameof(HasSecondaryCommands),
                o => o._hasSecondaryCommands);

        private bool _isOverflowButtonVisible;
        /// <summary>
        /// Defines the <see cref="IsOverflowButtonVisible"/> property.
        /// </summary>
        public static readonly DirectProperty<CommandBar, bool> IsOverflowButtonVisibleProperty =
            AvaloniaProperty.RegisterDirect<CommandBar, bool>(
                nameof(IsOverflowButtonVisible),
                o => o._isOverflowButtonVisible);

        /// <summary>
        /// Defines the <see cref="Opening"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> OpeningEvent =
            RoutedEvent.Register<CommandBar, RoutedEventArgs>(nameof(Opening), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Opened"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> OpenedEvent =
            RoutedEvent.Register<CommandBar, RoutedEventArgs>(nameof(Opened), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Closing"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ClosingEvent =
            RoutedEvent.Register<CommandBar, RoutedEventArgs>(nameof(Closing), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Closed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent =
            RoutedEvent.Register<CommandBar, RoutedEventArgs>(nameof(Closed), RoutingStrategies.Bubble);

        private Button? _overflowButton;
        private Popup? _overflowPopup;
        private ItemsControl? _overflowPresenter;
        private Control? _contentPresenter;
        private ItemsControl? _primaryCommandsPresenter;

        private readonly ObservableCollection<ICommandBarElement> _visiblePrimaryCommands = new();
        private readonly ObservableCollection<ICommandBarElement> _overflowItems = new();
        private readonly CommandBarSeparator _overflowPrimarySecondarySeparator = new();
        private readonly CompositeDisposable _primaryCommandVisibilitySubscriptions = new();
        private readonly CompositeDisposable _secondaryCommandVisibilitySubscriptions = new();
        private bool _isDynamicUpdateInProgress;
        private bool _isOpeningOverflowPopup;
        private bool _hasDeferredDynamicOverflowUpdate;
        private double _constraintWidth = double.PositiveInfinity;
        private readonly HashSet<ICommandBarElement> _overflowedPrimaryCommands = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<ICommandBarElement, double> _measuredWidths = new(ReferenceEqualityComparer.Instance);
        private double _itemGap;
        private double _overflowButtonWidth = DefaultOverflowButtonWidth;
        private bool _isOverflowButtonMeasured;
        private double _separatorWidth;
        private ThemeVariant? _measuredThemeVariant;
        private bool _isMeasuring;
        private bool _openedViaKeyboard;

        // The width the overflow button is assumed to take before it has been measured.
        private const double DefaultOverflowButtonWidth = 48;

        public CommandBar()
        {
            VisiblePrimaryCommands = new ReadOnlyObservableCollection<ICommandBarElement>(_visiblePrimaryCommands);
            OverflowItems = new ReadOnlyObservableCollection<ICommandBarElement>(_overflowItems);

            var primaryCommands = new ObservableCollection<ICommandBarElement>();
            SetCurrentValue(PrimaryCommandsProperty, (IList<ICommandBarElement>)primaryCommands);

            var secondaryCommands = new ObservableCollection<ICommandBarElement>();
            SetCurrentValue(SecondaryCommandsProperty, (IList<ICommandBarElement>)secondaryCommands);

            RebuildCommandVisibilitySubscriptions(PrimaryCommands, _primaryCommandVisibilitySubscriptions, isPrimary: true);
            RebuildCommandVisibilitySubscriptions(SecondaryCommands, _secondaryCommandVisibilitySubscriptions);
            SizeChanged += CommandBar_SizeChanged;
        }

        /// <summary>
        /// Gets the read-only collection of primary commands currently visible in the bar
        /// (may be a subset when dynamic overflow is active).
        /// </summary>
        public ReadOnlyObservableCollection<ICommandBarElement> VisiblePrimaryCommands { get; }

        /// <summary>
        /// Gets the read-only collection of items shown in the overflow popup (secondary commands
        /// plus any primary commands moved to overflow by dynamic overflow).
        /// </summary>
        public ReadOnlyObservableCollection<ICommandBarElement> OverflowItems { get; }

        /// <summary>
        /// Gets or sets the collection of primary commands displayed in the bar.
        /// </summary>
        [Content]
        [SuppressMessage("AvaloniaProperty", "AVP1030:StyledProperty accessors should not have side effects", Justification = "Necessary for now to avoid returning null.")]
        public IList<ICommandBarElement> PrimaryCommands
        {
            get
            {
                var value = GetValue(PrimaryCommandsProperty);
                if (value is null)
                {
                    var list = new ObservableCollection<ICommandBarElement>();
                    SetCurrentValue(PrimaryCommandsProperty, (IList<ICommandBarElement>)list);
                    return list;
                }
                return value;
            }
            set => SetValue(PrimaryCommandsProperty, value);
        }

        /// <summary>
        /// Gets or sets the collection of secondary commands shown in the overflow menu.
        /// </summary>
        [SuppressMessage("AvaloniaProperty", "AVP1030:StyledProperty accessors should not have side effects", Justification = "Necessary for now to avoid returning null.")]
        public IList<ICommandBarElement> SecondaryCommands
        {
            get
            {
                var value = GetValue(SecondaryCommandsProperty);
                if (value is null)
                {
                    var list = new ObservableCollection<ICommandBarElement>();
                    SetCurrentValue(SecondaryCommandsProperty, (IList<ICommandBarElement>)list);
                    return list;
                }
                return value;
            }
            set => SetValue(SecondaryCommandsProperty, value);
        }

        /// <summary>
        /// Gets or sets custom content displayed at the start (left) of the bar.
        /// </summary>
        public object? Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// Gets or sets how labels are positioned on command buttons.
        /// </summary>
        public CommandBarDefaultLabelPosition DefaultLabelPosition
        {
            get => GetValue(DefaultLabelPositionProperty);
            set => SetValue(DefaultLabelPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets whether primary commands are automatically moved to the overflow menu
        /// when there is not enough space to display them.
        /// </summary>
        public bool IsDynamicOverflowEnabled
        {
            get => GetValue(IsDynamicOverflowEnabledProperty);
            set => SetValue(IsDynamicOverflowEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets the visibility of the overflow button.
        /// </summary>
        public CommandBarOverflowButtonVisibility OverflowButtonVisibility
        {
            get => GetValue(OverflowButtonVisibilityProperty);
            set => SetValue(OverflowButtonVisibilityProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the overflow menu is open.
        /// </summary>
        public bool IsOpen
        {
            get => GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the overflow menu stays open after a command is invoked
        /// (disables light-dismiss behavior).
        /// </summary>
        public bool IsSticky
        {
            get => GetValue(IsStickyProperty);
            set => SetValue(IsStickyProperty, value);
        }

        /// <summary>
        /// Gets or sets the estimated item width used in dynamic-overflow calculations
        /// when <see cref="DefaultLabelPosition"/> is <see cref="CommandBarDefaultLabelPosition.Bottom"/>.
        /// </summary>
        /// <remarks>
        /// A command bar whose template has PART_PrimaryCommands fits its primary commands by the widths they
        /// measured in it, and uses the estimate only for a command it has not shown yet.
        /// </remarks>
        public double ItemWidthBottom
        {
            get => GetValue(ItemWidthBottomProperty);
            set => SetValue(ItemWidthBottomProperty, value);
        }

        /// <summary>
        /// Gets or sets the estimated item width used in dynamic-overflow calculations
        /// when <see cref="DefaultLabelPosition"/> is <see cref="CommandBarDefaultLabelPosition.Right"/>.
        /// </summary>
        /// <remarks>
        /// A command bar whose template has PART_PrimaryCommands fits its primary commands by the widths they
        /// measured in it, and uses the estimate only for a command it has not shown yet.
        /// </remarks>
        public double ItemWidthRight
        {
            get => GetValue(ItemWidthRightProperty);
            set => SetValue(ItemWidthRightProperty, value);
        }

        /// <summary>
        /// Gets or sets the estimated item width used in dynamic-overflow calculations
        /// when <see cref="DefaultLabelPosition"/> is <see cref="CommandBarDefaultLabelPosition.Collapsed"/>.
        /// </summary>
        /// <remarks>
        /// A command bar whose template has PART_PrimaryCommands fits its primary commands by the widths they
        /// measured in it, and uses the estimate only for a command it has not shown yet.
        /// </remarks>
        public double ItemWidthCollapsed
        {
            get => GetValue(ItemWidthCollapsedProperty);
            set => SetValue(ItemWidthCollapsedProperty, value);
        }

        /// <summary>
        /// Gets whether there are any commands (secondary or overflowed primary) in the overflow menu.
        /// </summary>
        public bool HasSecondaryCommands
        {
            get => _hasSecondaryCommands;
            private set => SetAndRaise(HasSecondaryCommandsProperty, ref _hasSecondaryCommands, value);
        }

        /// <summary>
        /// Gets whether the overflow button is currently visible.
        /// </summary>
        public bool IsOverflowButtonVisible
        {
            get => _isOverflowButtonVisible;
            private set => SetAndRaise(IsOverflowButtonVisibleProperty, ref _isOverflowButtonVisible, value);
        }

        /// <summary>
        /// Occurs when the overflow menu is about to open.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? Opening
        {
            add => AddHandler(OpeningEvent, value);
            remove => RemoveHandler(OpeningEvent, value);
        }

        /// <summary>
        /// Occurs when the overflow menu has opened.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? Opened
        {
            add => AddHandler(OpenedEvent, value);
            remove => RemoveHandler(OpenedEvent, value);
        }

        /// <summary>
        /// Occurs when the overflow menu is about to close.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? Closing
        {
            add => AddHandler(ClosingEvent, value);
            remove => RemoveHandler(ClosingEvent, value);
        }

        /// <summary>
        /// Occurs when the overflow menu has closed.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? Closed
        {
            add => AddHandler(ClosedEvent, value);
            remove => RemoveHandler(ClosedEvent, value);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (_overflowButton != null)
            {
                _overflowButton.Click -= OnOverflowButtonClick;
                _overflowButton.GotFocus -= OnOverflowButtonGotFocus;
                _overflowButton.RemoveHandler(Input.InputElement.KeyDownEvent, OnOverflowButtonKeyDown);
                _overflowButton.RemoveHandler(Input.InputElement.PointerPressedEvent, OnOverflowButtonPointerPressed);
            }
            if (_overflowPresenter != null)
                _overflowPresenter.KeyDown -= OnOverflowPresenterKeyDown;
            if (_overflowPopup != null)
                _overflowPopup.Opened -= OnOverflowPopupOpened;

            _overflowButton = e.NameScope.Find<Button>("PART_OverflowButton");
            _overflowPopup = e.NameScope.Find<Popup>("PART_OverflowPopup");
            _overflowPresenter = e.NameScope.Find<ItemsControl>("PART_OverflowPresenter");
            _contentPresenter = e.NameScope.Find<Control>("PART_ContentPresenter");
            _primaryCommandsPresenter = e.NameScope.Find<ItemsControl>("PART_PrimaryCommands");
            ForgetMeasuredWidths();

            if (_overflowButton != null)
            {
                _overflowButton.Click += OnOverflowButtonClick;
                _overflowButton.GotFocus += OnOverflowButtonGotFocus;
                _overflowButton.AddHandler(Input.InputElement.KeyDownEvent, OnOverflowButtonKeyDown, handledEventsToo: true);
                _overflowButton.AddHandler(Input.InputElement.PointerPressedEvent, OnOverflowButtonPointerPressed, handledEventsToo: true);
            }
            if (_overflowPresenter != null)
                _overflowPresenter.KeyDown += OnOverflowPresenterKeyDown;
            if (_overflowPopup != null)
                _overflowPopup.Opened += OnOverflowPopupOpened;

            ApplyLabelPositionToChildren();
            UpdateOverflowButtonVisibility();
            UpdateStickyBehavior();

            // The template is applied as the bar is measured, which places the commands.
            if (IsDynamicOverflowEnabled && _primaryCommandsPresenter is not null)
                _hasDeferredDynamicOverflowUpdate = true;
            else
                RequestDynamicOverflowUpdate();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            RebuildCommandVisibilitySubscriptions(PrimaryCommands, _primaryCommandVisibilitySubscriptions, isPrimary: true);
            RebuildCommandVisibilitySubscriptions(SecondaryCommands, _secondaryCommandVisibilitySubscriptions);
            RequestDynamicOverflowUpdate();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _primaryCommandVisibilitySubscriptions.Clear();
            _secondaryCommandVisibilitySubscriptions.Clear();
            base.OnDetachedFromVisualTree(e);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsOpenProperty)
            {
                var isOpen = change.GetNewValue<bool>();
                if (isOpen)
                {
                    RaiseEvent(new RoutedEventArgs(OpeningEvent));
                    if (_overflowPopup != null)
                    {
                        try
                        {
                            _isOpeningOverflowPopup = true;
                            _overflowPopup.IsOpen = true;
                        }
                        finally
                        {
                            _isOpeningOverflowPopup = false;
                        }

                        ApplyDeferredDynamicOverflowUpdate();
                    }
                    RaiseEvent(new RoutedEventArgs(OpenedEvent));
                }
                else
                {
                    RaiseEvent(new RoutedEventArgs(ClosingEvent));
                    if (_overflowPopup != null)
                        _overflowPopup.IsOpen = false;
                    RaiseEvent(new RoutedEventArgs(ClosedEvent));
                }
            }
            else if (change.Property == ThemeVariant.ActualThemeVariantProperty)
            {
                // Commands can measure differently in another theme variant. A detached bar has none, so only a variant other
                // than the one the widths were measured in counts, also when the bar is attached somewhere else.
                if (change.NewValue is ThemeVariant variant && _measuredThemeVariant is not null && variant != _measuredThemeVariant)
                {
                    ForgetMeasuredWidths();
                    if (IsDynamicOverflowEnabled)
                        RequestDynamicOverflowUpdate();
                }
            }
            else if (change.Property == DefaultLabelPositionProperty)
            {
                // Commands measure differently with another label position.
                ForgetMeasuredWidths();
                ApplyLabelPositionToChildren();
                if (IsDynamicOverflowEnabled)
                    RequestDynamicOverflowUpdate();
            }
            else if (change.Property == OverflowButtonVisibilityProperty)
            {
                UpdateOverflowButtonVisibility();
                RequestDynamicOverflowUpdate();
            }
            else if (change.Property == IsStickyProperty)
            {
                UpdateStickyBehavior();
            }
            else if (change.Property == IsDynamicOverflowEnabledProperty)
            {
                RequestDynamicOverflowUpdate();
            }
            else if (change.Property == PrimaryCommandsProperty)
            {
                if (change.OldValue is INotifyCollectionChanged oldPrimary)
                    oldPrimary.CollectionChanged -= OnPrimaryCommandsChanged;
                if (change.NewValue is INotifyCollectionChanged newPrimary)
                    newPrimary.CollectionChanged += OnPrimaryCommandsChanged;
                RebuildCommandVisibilitySubscriptions(PrimaryCommands, _primaryCommandVisibilitySubscriptions, isPrimary: true);
                ForgetRemovedPrimaryCommands();
                ApplyLabelPositionToChildren();
                RequestDynamicOverflowUpdate();
            }
            else if (change.Property == SecondaryCommandsProperty)
            {
                if (change.OldValue is INotifyCollectionChanged oldSecondary)
                    oldSecondary.CollectionChanged -= OnSecondaryCommandsChanged;
                if (change.NewValue is INotifyCollectionChanged newSecondary)
                    newSecondary.CollectionChanged += OnSecondaryCommandsChanged;
                RebuildCommandVisibilitySubscriptions(SecondaryCommands, _secondaryCommandVisibilitySubscriptions);
                RequestDynamicOverflowUpdate();
            }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var newConstraint = double.IsFinite(availableSize.Width)
                ? availableSize.Width
                : double.PositiveInfinity;

            if (!IsDynamicOverflowEnabled || _primaryCommandsPresenter is null)
            {
                if (newConstraint != _constraintWidth)
                {
                    _constraintWidth = newConstraint;
                    RequestDynamicOverflowUpdate();
                }

                return base.MeasureOverride(availableSize);
            }

            _constraintWidth = newConstraint;
            _isMeasuring = true;
            try
            {
                return MeasureAndPlacePrimaryCommands(availableSize);
            }
            finally
            {
                _isMeasuring = false;
            }
        }

        // Shows the highest-priority visible commands that fit the available width and moves the rest to the overflow, then
        // measures the template for the available size. The width a number of commands needs is computed from the widths the
        // commands, separators and overflow button last measured in the bar and compared with the space the items panel gets,
        // as WinUI's CommandBar does, and checked by measuring the template again. A command moved to the overflow does not come back during the same measure, so one
        // whose visibility or width depends on the placement does not move back and forth; only while the overflow button has
        // not been measured yet may the measured width bring it back. As in WinUI, an overflowed command is not measured again
        // until it shows, so one that gets narrower in the overflow comes back once its last width fits.
        private Size MeasureAndPlacePrimaryCommands(Size availableSize)
        {
            var canOverflow = double.IsFinite(availableSize.Width) && availableSize.Width >= 50;
            if (!canOverflow && _overflowedPrimaryCommands.Count > 0)
            {
                _overflowedPrimaryCommands.Clear();
                UpdateDynamicOverflow();
            }

            var movedOut = new HashSet<ICommandBarElement>(ReferenceEqualityComparer.Instance);
            var maxPasses = 2 * PrimaryCommands.Count + 6;
            // Placement changes since the last measure, such as added commands, must reach the widths read below.
            InvalidateTemplateMeasure();

            for (var pass = 0; ; pass++)
            {
                if (_hasDeferredDynamicOverflowUpdate)
                {
                    _hasDeferredDynamicOverflowUpdate = false;
                    UpdateDynamicOverflow();
                    InvalidateTemplateMeasure();
                }

                if (canOverflow && pass < maxPasses && PlaceFittingCommands(availableSize, movedOut))
                {
                    UpdateDynamicOverflow();
                    InvalidateTemplateMeasure();
                    continue;
                }

                // An update requested meanwhile, such as by the visibility binding of a command attached to the bar, takes
                // another pass; one that comes too late is applied by the next measure.
                var size = base.MeasureOverride(availableSize);
                if (!canOverflow)
                    RecordMeasuredWidths();
                if (!_hasDeferredDynamicOverflowUpdate || pass >= maxPasses)
                    return size;
            }
        }

        // Places the commands that fit the given width by the widths measured in the bar. Returns whether the placement changed.
        private bool PlaceFittingCommands(Size availableSize, HashSet<ICommandBarElement> movedOut)
        {
            // The items panel measures its commands without a width limit, as their widths are what is compared, and gets the
            // space the template leaves it, which grows by the overflow button while that shows.
            base.MeasureOverride(availableSize);
            RecordMeasuredWidths();
            if (_primaryCommandsPresenter?.ItemsPanelRoot is not { } panel || LayoutInformation.GetPreviousMeasureConstraint(panel) is not { } space)
                return false;
            var commands = GetVisibleCommandsByPriority();
            var buttonWidth = _overflowButton is { IsVisible: true } ? _overflowButton.DesiredSize.Width : 0;
            var width = space.Width + buttonWidth;
            if (!double.IsFinite(width))
            {
                // A template that does not limit the items panel, such as one with it in an Auto column, leaves it whatever the
                // rest of the template does not take.
                var templateWidth = base.MeasureOverride(new Size(double.PositiveInfinity, availableSize.Height)).Width;
                width = availableSize.Width - (templateWidth - panel.DesiredSize.Width - buttonWidth);
            }

            var shown = CountFittingCommands(commands, width);
            for (var i = 0; i < shown; i++)
            {
                if (movedOut.Contains(PrimaryCommands[commands[i]]))
                    shown = i;
            }

            var changed = false;
            for (var i = 0; i < commands.Count; i++)
            {
                var command = PrimaryCommands[commands[i]];
                if (i < shown)
                    changed |= _overflowedPrimaryCommands.Remove(command);
                else if (_overflowedPrimaryCommands.Add(command))
                {
                    changed = true;
                    if (_isOverflowButtonMeasured || !CanShowOverflowButton())
                        movedOut.Add(command);
                }
            }

            return changed;
        }

        // The indices of the visible primary commands, highest priority first: lower DynamicOverflowOrder, then earlier.
        private List<int> GetVisibleCommandsByPriority()
        {
            var commands = new List<int>();
            for (var i = 0; i < PrimaryCommands.Count; i++)
            {
                if (PrimaryCommands[i] is not CommandBarSeparator && IsCommandVisible(PrimaryCommands[i]))
                    commands.Add(i);
            }

            commands.Sort((a, b) =>
            {
                var order = GetDynamicOverflowOrder(PrimaryCommands[a]).CompareTo(GetDynamicOverflowOrder(PrimaryCommands[b]));
                return order != 0 ? order : a.CompareTo(b);
            });
            return commands;
        }

        // How many of the given commands, highest priority first, fit the given width together with the separators between
        // them and the overflow button they need. One command stays while it has more than 32px.
        private int CountFittingCommands(List<int> commands, double width)
        {
            if (MathUtilities.LessThanOrClose(GetRequiredWidth(commands, commands.Count), width))
                return commands.Count;

            // With a command in the overflow the button shows, so the width grows with every command shown.
            int fitting = 0, notFitting = commands.Count;
            while (notFitting - fitting > 1)
            {
                var middle = (fitting + notFitting) / 2;
                if (MathUtilities.LessThanOrClose(GetRequiredWidth(commands, middle), width))
                    fitting = middle;
                else
                    notFitting = middle;
            }

            return fitting == 0 && commands.Count > 0 && width - GetRequiredWidth(commands, 0) > 32 ? 1 : fitting;
        }

        // The width the items panel needs to show the first count commands, with the separators that remain between them and
        // the gap between items, plus the overflow button when it shows.
        private double GetRequiredWidth(List<int> commands, int count)
        {
            var shown = new HashSet<int>();
            for (var i = 0; i < count; i++)
                shown.Add(commands[i]);
            if (count > 0)
            {
                for (var i = 0; i < PrimaryCommands.Count; i++)
                {
                    if (PrimaryCommands[i] is CommandBarSeparator && IsCommandVisible(PrimaryCommands[i]))
                        shown.Add(i);
                }
            }

            TrimOrphanedSeparatorsFromVisibleCommands(PrimaryCommands, shown);

            var width = Math.Max(0, shown.Count - 1) * _itemGap;
            foreach (var index in shown)
            {
                var command = PrimaryCommands[index];
                if (_measuredWidths.TryGetValue(command, out var commandWidth))
                    width += commandWidth;
                else
                    width += command is CommandBarSeparator ? _separatorWidth : GetItemWidth();
            }

            var showsButton = CanShowOverflowButton()
                && (OverflowButtonVisibility == CommandBarOverflowButtonVisibility.Visible || count < commands.Count || SecondaryCommands.Count > 0);

            return showsButton ? width + _overflowButtonWidth : width;
        }

        // Whether overflowed commands show the overflow button, whose width then counts.
        private bool CanShowOverflowButton() => _overflowButton is not null && OverflowButtonVisibility != CommandBarOverflowButtonVisibility.Collapsed;

        private double GetItemWidth() => DefaultLabelPosition switch
        {
            CommandBarDefaultLabelPosition.Right => ItemWidthRight,
            CommandBarDefaultLabelPosition.Collapsed => ItemWidthCollapsed,
            _ => ItemWidthBottom
        };

        // Records the widths the shown commands, separators and overflow button measured in the bar and the gap the items panel
        // leaves between its items, such as StackPanel.Spacing.
        private void RecordMeasuredWidths()
        {
            if (_overflowButton is { IsVisible: true })
            {
                _overflowButtonWidth = _overflowButton.DesiredSize.Width;
                _isOverflowButtonMeasured = true;
            }
            _measuredThemeVariant = ActualThemeVariant;
            if (_primaryCommandsPresenter?.ItemsPanelRoot is not { } panel)
                return;

            var itemsWidth = 0.0;
            var items = 0;
            foreach (var child in panel.Children)
            {
                if (!child.IsVisible)
                    continue;
                itemsWidth += child.DesiredSize.Width;
                items++;
                if (child is CommandBarSeparator)
                    _separatorWidth = child.DesiredSize.Width;
                if (child is ICommandBarElement command)
                    _measuredWidths[command] = child.DesiredSize.Width;
            }

            // Another panel tells the gap only while it is not limited to the space it was given.
            var isLimited = LayoutInformation.GetPreviousMeasureConstraint(panel) is { } constraint
                && MathUtilities.AreClose(panel.DesiredSize.Width, constraint.Width);
            if (panel is StackPanel stackPanel)
                _itemGap = stackPanel.Spacing;
            else if (items > 1 && !isLimited)
                _itemGap = Math.Max(0, (panel.DesiredSize.Width - itemsWidth) / (items - 1));
        }

        // A changed items panel, overflow button or content is measured again only when the template is: an ancestor that keeps
        // its measure for the same size would return its last result.
        private void InvalidateTemplateMeasure()
        {
            InvalidateMeasureUpToThis((Visual?)_primaryCommandsPresenter?.ItemsPanelRoot ?? _primaryCommandsPresenter);
            InvalidateMeasureUpToThis(_overflowButton);
            InvalidateMeasureUpToThis(_contentPresenter);
        }

        // Commands measure differently after a change such as another label position, so every command shows to be measured
        // again, as on the first layout.
        private void ForgetMeasuredWidths()
        {
            _measuredWidths.Clear();
            _overflowedPrimaryCommands.Clear();
            _isOverflowButtonMeasured = false;
            _measuredThemeVariant = null;
        }

        private void InvalidateMeasureUpToThis(Visual? visual)
        {
            for (var current = visual; current is not null && current != this; current = current.VisualParent)
                (current as Layoutable)?.InvalidateMeasure();
        }

        private void CommandBar_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            // A bar that fits its commands by measuring them has already placed them for its new size.
            if (!IsDynamicOverflowEnabled || _primaryCommandsPresenter is null)
                RequestDynamicOverflowUpdate();
        }

        private void OnOverflowButtonClick(object? sender, Interactivity.RoutedEventArgs e)
        {
            SetCurrentValue(IsOpenProperty, !IsOpen);
        }

        private void OnOverflowButtonGotFocus(object? sender, Input.FocusChangedEventArgs e)
        {
            _openedViaKeyboard = e.NavigationMethod is Input.NavigationMethod.Directional
                or Input.NavigationMethod.Tab;
        }

        private void OnOverflowButtonKeyDown(object? sender, Input.KeyEventArgs e)
        {
            if (e.Key is Input.Key.Enter or Input.Key.Space)
                _openedViaKeyboard = true;
        }

        private void OnOverflowButtonPointerPressed(object? sender, Input.PointerPressedEventArgs e)
        {
            _openedViaKeyboard = false;
        }

        private void OnOverflowPresenterKeyDown(object? sender, Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Input.Key.Up:
                    NavigateOverflow(forward: false);
                    e.Handled = true;
                    break;
                case Input.Key.Down:
                    NavigateOverflow(forward: true);
                    e.Handled = true;
                    break;
                case Input.Key.Home:
                    FocusOverflowItem(first: true);
                    e.Handled = true;
                    break;
                case Input.Key.End:
                    FocusOverflowItem(first: false);
                    e.Handled = true;
                    break;
                case Input.Key.Escape:
                    SetCurrentValue(IsOpenProperty, false);
                    _overflowButton?.Focus(Input.NavigationMethod.Unspecified);
                    e.Handled = true;
                    break;
            }
        }

        private void OnOverflowPopupOpened(object? sender, EventArgs e)
        {
            var method = _openedViaKeyboard
                ? Input.NavigationMethod.Directional
                : Input.NavigationMethod.Pointer;

            Dispatcher.UIThread.Post(() =>
            {
                if (IsOpen)
                    FocusOverflowItem(first: true, method);
            }, DispatcherPriority.Loaded);
        }

        private void NavigateOverflow(bool forward)
        {
            var items = GetFocusableOverflowItems();
            if (items.Count == 0)
                return;

            int current = -1;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].IsFocused || items[i].IsKeyboardFocusWithin)
                {
                    current = i;
                    break;
                }
            }

            int next = current < 0
                ? (forward ? 0 : items.Count - 1)
                : (forward ? (current + 1) % items.Count : (current - 1 + items.Count) % items.Count);

            items[next].Focus(Input.NavigationMethod.Directional);
        }

        private void FocusOverflowItem(bool first, Input.NavigationMethod method = Input.NavigationMethod.Directional)
        {
            var items = GetFocusableOverflowItems();
            if (items.Count > 0)
                items[first ? 0 : items.Count - 1].Focus(method);
        }

        private List<Control> GetFocusableOverflowItems()
        {
            var result = new List<Control>();
            foreach (var item in _overflowItems)
            {
                if (item is Control { IsEnabled: true, IsVisible: true, Focusable: true } control && item is not CommandBarSeparator)
                    result.Add(control);
            }
            return result;
        }

        private void OnPrimaryCommandsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is ICommandBarElement element)
                        ApplyLabelPositionToElement(element);
                }
            }
            RebuildCommandVisibilitySubscriptions(PrimaryCommands, _primaryCommandVisibilitySubscriptions, isPrimary: true);
            ForgetRemovedPrimaryCommands();
            RequestDynamicOverflowUpdate();
        }

        private void OnSecondaryCommandsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildCommandVisibilitySubscriptions(SecondaryCommands, _secondaryCommandVisibilitySubscriptions);
            RequestDynamicOverflowUpdate();
        }

        private void RequestDynamicOverflowUpdate()
        {
            _hasDeferredDynamicOverflowUpdate = true;
            ApplyDeferredDynamicOverflowUpdate();
        }

        private void ApplyDeferredDynamicOverflowUpdate()
        {
            // While the bar measures, an update waits for its next pass; rebuilding the collections now could change them while
            // the items presenters enumerate them.
            if (_isOpeningOverflowPopup || _isDynamicUpdateInProgress || _isMeasuring)
                return;

            // A visibility change during an update, such as a binding that resolves when a command is re-parented, needs
            // another pass. A command whose visibility depends on the placement of others can invalidate every pass, so
            // the last placement is kept after a few passes instead of looping forever on the UI thread.
            const int maxPasses = 3;
            for (var pass = 0; pass < maxPasses && _hasDeferredDynamicOverflowUpdate; pass++)
            {
                _hasDeferredDynamicOverflowUpdate = false;
                UpdateDynamicOverflow();
            }

            _hasDeferredDynamicOverflowUpdate = false;
        }

        private void UpdateDynamicOverflow()
        {
            if (PrimaryCommands == null || SecondaryCommands == null)
                return;
            _isDynamicUpdateInProgress = true;

            try
            {
                var visiblePrimaryCommands = new List<ICommandBarElement>(PrimaryCommands.Count);
                var overflowedPrimaryCommands = new List<ICommandBarElement>();
                var overflowItems = new List<ICommandBarElement>(PrimaryCommands.Count + SecondaryCommands.Count + 1);

                var availableWidth = double.IsFinite(_constraintWidth) ? _constraintWidth : Bounds.Width;

                if (IsDynamicOverflowEnabled && _primaryCommandsPresenter is not null)
                {
                    // A templated bar keeps its placement, which MeasureOverride fits to the available width.
                    var visibleIndices = new HashSet<int>();
                    for (var i = 0; i < PrimaryCommands.Count; i++)
                    {
                        var command = PrimaryCommands[i];
                        if (command is not CommandBarSeparator && IsCommandVisible(command) && !_overflowedPrimaryCommands.Contains(command))
                            visibleIndices.Add(i);
                    }

                    if (visibleIndices.Count > 0)
                    {
                        for (var i = 0; i < PrimaryCommands.Count; i++)
                        {
                            if (PrimaryCommands[i] is CommandBarSeparator && IsCommandVisible(PrimaryCommands[i]))
                                visibleIndices.Add(i);
                        }
                    }

                    TrimOrphanedSeparatorsFromVisibleCommands(PrimaryCommands, visibleIndices);
                    DistributePrimaryCommands(visibleIndices, visiblePrimaryCommands, overflowedPrimaryCommands);
                    if (!_isMeasuring)
                        InvalidateMeasure();
                }
                else if (!IsDynamicOverflowEnabled || availableWidth <= 0)
                {
                    _overflowedPrimaryCommands.Clear();
                    foreach (var item in PrimaryCommands)
                    {
                        SetOverflowMode(item, false);
                        visiblePrimaryCommands.Add(item);
                    }
                }
                else
                {
                    var contentWidth = _contentPresenter?.DesiredSize.Width ?? 0;

                    if (availableWidth < 50)
                    {
                        _overflowedPrimaryCommands.Clear();
                        foreach (var item in PrimaryCommands)
                        {
                            SetOverflowMode(item, false);
                            visiblePrimaryCommands.Add(item);
                        }
                    }
                    else
                    {
                        var itemWidth = GetItemWidth();

                        int primaryNonSepCount = 0;
                        foreach (var item in PrimaryCommands)
                            if (item is not CommandBarSeparator && IsCommandVisible(item))
                                primaryNonSepCount++;

                        const double overflowButtonWidth = DefaultOverflowButtonWidth;
                        var paddingH = Padding.Left + Padding.Right;
                        var baseAvailable = availableWidth - contentWidth - paddingH;
                        bool allFitWithoutButton = SecondaryCommands.Count == 0
                            && baseAvailable + 2 >= primaryNonSepCount * itemWidth;

                        int maxItems;
                        if (allFitWithoutButton)
                        {
                            maxItems = primaryNonSepCount;
                        }
                        else
                        {
                            var availableForItems = baseAvailable - overflowButtonWidth;
                            maxItems = Math.Max(0, (int)(availableForItems / itemWidth));
                            if (maxItems == 0 && PrimaryCommands.Count > 0 && availableForItems > 32)
                                maxItems = 1;
                        }

                        // Lower DynamicOverflowOrder = higher priority (stays visible longer).
                        var prioritized = new List<(int Index, int Order)>(PrimaryCommands.Count);
                        for (var i = 0; i < PrimaryCommands.Count; i++)
                        {
                            if (IsCommandVisible(PrimaryCommands[i]))
                                prioritized.Add((i, GetDynamicOverflowOrder(PrimaryCommands[i])));
                        }

                        prioritized.Sort((a, b) => a.Order != b.Order
                            ? a.Order.CompareTo(b.Order)
                            : a.Index.CompareTo(b.Index));

                        var visibleIndices = new HashSet<int>();
                        int nonSeparatorCount = 0;
                        for (var i = 0; i < prioritized.Count; i++)
                        {
                            var idx = prioritized[i].Index;
                            if (PrimaryCommands[idx] is CommandBarSeparator)
                                visibleIndices.Add(idx);
                            else if (nonSeparatorCount < maxItems)
                            {
                                visibleIndices.Add(idx);
                                nonSeparatorCount++;
                            }
                        }

                        if (nonSeparatorCount == 0)
                            visibleIndices.Clear();

                        TrimOrphanedSeparatorsFromVisibleCommands(PrimaryCommands, visibleIndices);
                        DistributePrimaryCommands(visibleIndices, visiblePrimaryCommands, overflowedPrimaryCommands);

                        _overflowedPrimaryCommands.Clear();
                        foreach (var command in overflowedPrimaryCommands)
                            _overflowedPrimaryCommands.Add(command);
                    }
                }

                BuildOverflowItems(overflowedPrimaryCommands, overflowItems);
                ApplyCommandCollections(visiblePrimaryCommands, overflowItems);
                HasSecondaryCommands = overflowItems.Count > 0;
                UpdateOverflowButtonVisibility();
            }
            finally
            {
                _isDynamicUpdateInProgress = false;
            }
        }

        private void DistributePrimaryCommands(HashSet<int> visibleIndices, List<ICommandBarElement> visiblePrimaryCommands,
            List<ICommandBarElement> overflowedPrimaryCommands)
        {
            for (var i = 0; i < PrimaryCommands.Count; i++)
            {
                if (!IsCommandVisible(PrimaryCommands[i]))
                {
                    // A hidden command keeps its placement state, so visibility that depends on IsInOverflow does not flip back.
                    visiblePrimaryCommands.Add(PrimaryCommands[i]);
                }
                else if (visibleIndices.Contains(i))
                {
                    SetOverflowMode(PrimaryCommands[i], false);
                    visiblePrimaryCommands.Add(PrimaryCommands[i]);
                }
                else if (PrimaryCommands[i] is CommandBarSeparator)
                {
                    SetOverflowMode(PrimaryCommands[i], false);
                }
                else
                {
                    SetOverflowMode(PrimaryCommands[i], true);
                    overflowedPrimaryCommands.Add(PrimaryCommands[i]);
                }
            }
        }

        private void ForgetRemovedPrimaryCommands()
        {
            if (_overflowedPrimaryCommands.Count == 0 && _measuredWidths.Count == 0)
                return;

            var commands = new HashSet<ICommandBarElement>(ReferenceEqualityComparer.Instance);
            if (PrimaryCommands is not null)
                commands.UnionWith(PrimaryCommands);
            _overflowedPrimaryCommands.IntersectWith(commands);
            foreach (var command in new List<ICommandBarElement>(_measuredWidths.Keys))
            {
                if (!commands.Contains(command))
                    _measuredWidths.Remove(command);
            }
        }

        private void SetOverflowMode(ICommandBarElement element, bool inOverflow)
        {
            element.IsInOverflow = inOverflow;
            ApplyLabelPositionToElement(element);
        }

        private static int GetDynamicOverflowOrder(ICommandBarElement element) => element switch
        {
            CommandBarButton b => b.DynamicOverflowOrder,
            CommandBarToggleButton t => t.DynamicOverflowOrder,
            _ => 0
        };

        private void ApplyLabelPositionToChildren()
        {
            if (PrimaryCommands != null)
                foreach (var cmd in PrimaryCommands)
                    ApplyLabelPositionToElement(cmd);
            if (SecondaryCommands != null)
                foreach (var cmd in SecondaryCommands)
                    ApplyLabelPositionToElement(cmd);

            ApplyLabelPositionToElement(_overflowPrimarySecondarySeparator);
        }

        private void ApplyLabelPositionToElement(ICommandBarElement element)
        {
            var labelPosition = element.IsInOverflow
                ? CommandBarDefaultLabelPosition.Right
                : DefaultLabelPosition;

            element.IsCompact = labelPosition == CommandBarDefaultLabelPosition.Collapsed;

            if (element is CommandBarButton abb)
                abb.LabelPosition = labelPosition;
            else if (element is CommandBarToggleButton atb)
                atb.LabelPosition = labelPosition;
        }

        private void UpdateStickyBehavior()
        {
            if (_overflowPopup != null)
                _overflowPopup.IsLightDismissEnabled = !IsSticky;
        }

        private void UpdateOverflowButtonVisibility()
        {
            IsOverflowButtonVisible = OverflowButtonVisibility switch
            {
                CommandBarOverflowButtonVisibility.Visible => true,
                CommandBarOverflowButtonVisibility.Collapsed => false,
                _ => HasSecondaryCommands // Auto
            };
        }

        private void BuildOverflowItems(IReadOnlyList<ICommandBarElement> overflowedPrimaryCommands, ICollection<ICommandBarElement> overflowItems)
        {
            for (var i = 0; i < overflowedPrimaryCommands.Count; i++)
                overflowItems.Add(overflowedPrimaryCommands[i]);

            if (overflowedPrimaryCommands.Count > 0 && HasVisibleElements(SecondaryCommands))
            {
                SetOverflowMode(_overflowPrimarySecondarySeparator, true);
                overflowItems.Add(_overflowPrimarySecondarySeparator);
            }
            else
                SetOverflowMode(_overflowPrimarySecondarySeparator, false);

            foreach (var item in SecondaryCommands)
            {
                SetOverflowMode(item, true);
                overflowItems.Add(item);
            }
        }

        private void ApplyCommandCollections(IReadOnlyList<ICommandBarElement> visiblePrimaryCommands, IReadOnlyList<ICommandBarElement> overflowItems)
        {
            // Commands that keep their place must not be detached, or a focused command loses focus. Removals from both
            // collections come first, so a command moving between them is never in both at once.
            RemoveMissingCommands(_visiblePrimaryCommands, visiblePrimaryCommands);
            RemoveMissingCommands(_overflowItems, overflowItems);
            ArrangeCommands(_visiblePrimaryCommands, visiblePrimaryCommands);
            ArrangeCommands(_overflowItems, overflowItems);
        }

        private static void RemoveMissingCommands(ObservableCollection<ICommandBarElement> commands, IReadOnlyList<ICommandBarElement> target)
        {
            for (var i = commands.Count - 1; i >= 0; i--)
            {
                if (IndexOfCommand(target, commands[i], 0) < 0)
                    commands.RemoveAt(i);
            }
        }

        private static void ArrangeCommands(ObservableCollection<ICommandBarElement> commands, IReadOnlyList<ICommandBarElement> target)
        {
            for (var i = 0; i < target.Count; i++)
            {
                if (i < commands.Count && ReferenceEquals(commands[i], target[i]))
                    continue;

                var index = IndexOfCommand(commands, target[i], i + 1);
                if (index >= 0)
                    commands.Move(index, i);
                else
                    commands.Insert(i, target[i]);
            }
        }

        private static int IndexOfCommand(IReadOnlyList<ICommandBarElement> commands, ICommandBarElement command, int startIndex)
        {
            for (var i = startIndex; i < commands.Count; i++)
            {
                if (ReferenceEquals(commands[i], command))
                    return i;
            }

            return -1;
        }

        private void RebuildCommandVisibilitySubscriptions(IList<ICommandBarElement>? commands, CompositeDisposable subscriptions, bool isPrimary = false)
        {
            subscriptions.Clear();

            if (commands is null)
                return;

            for (var i = 0; i < commands.Count; i++)
            {
                if (commands[i] is Avalonia.Visual visual)
                {
                    bool isInitialValue = true;
                    visual.GetObservable(Avalonia.Visual.IsVisibleProperty)
                        .Subscribe(_ =>
                        {
                            if (isInitialValue)
                            {
                                isInitialValue = false;
                                return;
                            }

                            RequestDynamicOverflowUpdate();
                        })
                        .DisposeWith(subscriptions);

                    if (isPrimary)
                    {
                        var command = commands[i];
                        // A shown command that changes size does not always change the size of the items panel, which the bar
                        // limits to the space it has, so the bar would not measure again and place the commands.
                        visual.GetPropertyChangedObservable(Visual.BoundsProperty)
                            .Subscribe(_ =>
                            {
                                if (!command.IsInOverflow && IsDynamicOverflowEnabled && _primaryCommandsPresenter is not null
                                    && visual is Layoutable layoutable
                                    && (!_measuredWidths.TryGetValue(command, out var width) || Math.Abs(width - layoutable.DesiredSize.Width) > 0.5))
                                    InvalidateMeasure();
                            })
                            .DisposeWith(subscriptions);
                    }
                }
            }
        }

        private static bool IsCommandVisible(ICommandBarElement command) => command is not Avalonia.Visual visual || visual.IsVisible;

        private static bool HasVisibleElements(IList<ICommandBarElement> commands)
        {
            for (var i = 0; i < commands.Count; i++)
            {
                if (commands[i] is Avalonia.Visual visual && visual.IsVisible)
                    return true;
            }

            return false;
        }

        private static void TrimOrphanedSeparatorsFromVisibleCommands(
            IList<ICommandBarElement> commands, HashSet<int> visibleIndices)
        {
            var toRemove = new List<int>();
            for (var i = 0; i < commands.Count; i++)
            {
                if (!visibleIndices.Contains(i) || commands[i] is not CommandBarSeparator)
                    continue;

                bool hasNonSeparatorBefore = FindNonSeparatorInVisibleCommands(
                    commands, visibleIndices, forward: false, startIndex: i - 1, out _);
                bool hasNonSeparatorAfter = FindNonSeparatorInVisibleCommands(
                    commands, visibleIndices, forward: true, startIndex: i + 1, out _);

                if (!hasNonSeparatorBefore || !hasNonSeparatorAfter)
                    toRemove.Add(i);
            }

            foreach (var idx in toRemove)
                visibleIndices.Remove(idx);

            bool previousWasSeparator = false;
            for (var i = 0; i < commands.Count; i++)
            {
                if (!visibleIndices.Contains(i))
                    continue;

                if (commands[i] is CommandBarSeparator)
                {
                    if (previousWasSeparator)
                        visibleIndices.Remove(i);
                    else
                        previousWasSeparator = true;
                }
                else
                {
                    previousWasSeparator = false;
                }
            }
        }

        private static bool FindNonSeparatorInVisibleCommands(
            IList<ICommandBarElement> commands, HashSet<int> visibleIndices,
            bool forward, int startIndex, out int foundIndex)
        {
            foundIndex = -1;
            var i = startIndex;
            while (forward ? i < commands.Count : i >= 0)
            {
                if (visibleIndices.Contains(i) && commands[i] is not CommandBarSeparator)
                {
                    foundIndex = i;
                    return true;
                }
                i += forward ? 1 : -1;
            }
            return false;
        }
    }
}
