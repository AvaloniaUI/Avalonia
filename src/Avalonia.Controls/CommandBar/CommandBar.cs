using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    /// <summary>
    /// A command bar that provides primary commands displayed inline and secondary commands
    /// accessible via an overflow menu.
    /// </summary>
    [TemplatePart("PART_OverflowButton",   typeof(Button))]
    [TemplatePart("PART_OverflowPopup",    typeof(Popup))]
    [TemplatePart("PART_ContentPresenter", typeof(Control))]
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
        private Control? _contentPresenter;

        private readonly ObservableCollection<ICommandBarElement> _visiblePrimaryCommands = new();
        private readonly ObservableCollection<ICommandBarElement> _overflowItems = new();
        private readonly AppBarSeparator _overflowPrimarySecondarySeparator = new();
        private readonly CompositeDisposable _secondaryCommandVisibilitySubscriptions = new();
        private bool _isDynamicUpdateInProgress;
        private double _constraintWidth = double.PositiveInfinity;

        public CommandBar()
        {
            VisiblePrimaryCommands = new ReadOnlyObservableCollection<ICommandBarElement>(_visiblePrimaryCommands);
            OverflowItems = new ReadOnlyObservableCollection<ICommandBarElement>(_overflowItems);

            var primaryCommands = new ObservableCollection<ICommandBarElement>();
            SetCurrentValue(PrimaryCommandsProperty, (IList<ICommandBarElement>)primaryCommands);

            var secondaryCommands = new ObservableCollection<ICommandBarElement>();
            SetCurrentValue(SecondaryCommandsProperty, (IList<ICommandBarElement>)secondaryCommands);

            RebuildSecondaryCommandVisibilitySubscriptions();
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
        public double ItemWidthBottom
        {
            get => GetValue(ItemWidthBottomProperty);
            set => SetValue(ItemWidthBottomProperty, value);
        }

        /// <summary>
        /// Gets or sets the estimated item width used in dynamic-overflow calculations
        /// when <see cref="DefaultLabelPosition"/> is <see cref="CommandBarDefaultLabelPosition.Right"/>.
        /// </summary>
        public double ItemWidthRight
        {
            get => GetValue(ItemWidthRightProperty);
            set => SetValue(ItemWidthRightProperty, value);
        }

        /// <summary>
        /// Gets or sets the estimated item width used in dynamic-overflow calculations
        /// when <see cref="DefaultLabelPosition"/> is <see cref="CommandBarDefaultLabelPosition.Collapsed"/>.
        /// </summary>
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
                _overflowButton.Click -= OnOverflowButtonClick;

            _overflowButton = e.NameScope.Find<Button>("PART_OverflowButton");
            _overflowPopup = e.NameScope.Find<Popup>("PART_OverflowPopup");
            _contentPresenter = e.NameScope.Find<Control>("PART_ContentPresenter");

            if (_overflowButton != null)
                _overflowButton.Click += OnOverflowButtonClick;

            ApplyLabelPositionToChildren();
            UpdateOverflowButtonVisibility();
            UpdateStickyBehavior();
            UpdateDynamicOverflow();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            RebuildSecondaryCommandVisibilitySubscriptions();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _secondaryCommandVisibilitySubscriptions.Clear();
            base.OnDetachedFromVisualTree(e);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsOpenProperty)
            {
                var isOpen = (bool)change.NewValue!;
                if (isOpen)
                {
                    RaiseEvent(new RoutedEventArgs(OpeningEvent));
                    if (_overflowPopup != null)
                        _overflowPopup.IsOpen = true;
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
            else if (change.Property == DefaultLabelPositionProperty)
            {
                ApplyLabelPositionToChildren();
            }
            else if (change.Property == OverflowButtonVisibilityProperty)
            {
                UpdateOverflowButtonVisibility();
            }
            else if (change.Property == IsStickyProperty)
            {
                UpdateStickyBehavior();
            }
            else if (change.Property == IsDynamicOverflowEnabledProperty)
            {
                UpdateDynamicOverflow();
            }
            else if (change.Property == PrimaryCommandsProperty)
            {
                if (change.OldValue is INotifyCollectionChanged oldPrimary)
                    oldPrimary.CollectionChanged -= OnPrimaryCommandsChanged;
                if (change.NewValue is INotifyCollectionChanged newPrimary)
                    newPrimary.CollectionChanged += OnPrimaryCommandsChanged;
                ApplyLabelPositionToChildren();
                UpdateDynamicOverflow();
            }
            else if (change.Property == SecondaryCommandsProperty)
            {
                if (change.OldValue is INotifyCollectionChanged oldSecondary)
                    oldSecondary.CollectionChanged -= OnSecondaryCommandsChanged;
                if (change.NewValue is INotifyCollectionChanged newSecondary)
                    newSecondary.CollectionChanged += OnSecondaryCommandsChanged;
                RebuildSecondaryCommandVisibilitySubscriptions();
                UpdateDynamicOverflow();
            }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var newConstraint = double.IsFinite(availableSize.Width)
                ? availableSize.Width
                : double.PositiveInfinity;

            if (newConstraint != _constraintWidth)
            {
                _constraintWidth = newConstraint;
                UpdateDynamicOverflow();
            }

            return base.MeasureOverride(availableSize);
        }

        private void CommandBar_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            UpdateDynamicOverflow();
        }

        private void OnOverflowButtonClick(object? sender, Interactivity.RoutedEventArgs e)
        {
            SetCurrentValue(IsOpenProperty, !IsOpen);
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
            UpdateDynamicOverflow();
        }

        private void OnSecondaryCommandsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildSecondaryCommandVisibilitySubscriptions();
            UpdateDynamicOverflow();
        }

        private void UpdateDynamicOverflow()
        {
            if (PrimaryCommands == null || SecondaryCommands == null)
                return;
            if (_isDynamicUpdateInProgress)
                return;
            _isDynamicUpdateInProgress = true;

            try
            {
                _visiblePrimaryCommands.Clear();
                _overflowItems.Clear();
                SetOverflowMode(_overflowPrimarySecondarySeparator, false);

                var overflowedPrimaryCommands = new List<ICommandBarElement>();

                var availableWidth = double.IsFinite(_constraintWidth) ? _constraintWidth : Bounds.Width;

                if (!IsDynamicOverflowEnabled || availableWidth <= 0)
                {
                    foreach (var item in PrimaryCommands)
                    {
                        SetOverflowMode(item, false);
                        _visiblePrimaryCommands.Add(item);
                    }
                }
                else
                {
                    var contentWidth = _contentPresenter?.DesiredSize.Width ?? 0;

                    if (availableWidth < 50)
                    {
                        foreach (var item in PrimaryCommands)
                        {
                            SetOverflowMode(item, false);
                            _visiblePrimaryCommands.Add(item);
                        }
                    }
                    else
                    {
                        var itemWidth = DefaultLabelPosition switch
                        {
                            CommandBarDefaultLabelPosition.Right     => ItemWidthRight,
                            CommandBarDefaultLabelPosition.Collapsed => ItemWidthCollapsed,
                            _                                        => ItemWidthBottom
                        };

                        int primaryNonSepCount = 0;
                        foreach (var item in PrimaryCommands)
                            if (item is not AppBarSeparator)
                                primaryNonSepCount++;

                        const double overflowButtonWidth = 48;
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
                            prioritized.Add((i, GetDynamicOverflowOrder(PrimaryCommands[i])));

                        prioritized.Sort((a, b) => a.Order != b.Order
                            ? a.Order.CompareTo(b.Order)
                            : a.Index.CompareTo(b.Index));

                        var visibleIndices = new HashSet<int>();
                        int nonSeparatorCount = 0;
                        for (var i = 0; i < prioritized.Count; i++)
                        {
                            var idx = prioritized[i].Index;
                            if (PrimaryCommands[idx] is AppBarSeparator)
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

                        for (var i = 0; i < PrimaryCommands.Count; i++)
                        {
                            if (visibleIndices.Contains(i))
                            {
                                SetOverflowMode(PrimaryCommands[i], false);
                                _visiblePrimaryCommands.Add(PrimaryCommands[i]);
                            }
                            else if (PrimaryCommands[i] is AppBarSeparator)
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
                }

                AddOverflowItems(overflowedPrimaryCommands);
                HasSecondaryCommands = _overflowItems.Count > 0;
                UpdateOverflowButtonVisibility();
            }
            finally
            {
                _isDynamicUpdateInProgress = false;
            }
        }

        private static void SetOverflowMode(ICommandBarElement element, bool inOverflow)
            => element.IsInOverflow = inOverflow;

        private static int GetDynamicOverflowOrder(ICommandBarElement element) => element switch
        {
            AppBarButton b => b.DynamicOverflowOrder,
            AppBarToggleButton t => t.DynamicOverflowOrder,
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
            element.IsCompact = DefaultLabelPosition == CommandBarDefaultLabelPosition.Collapsed;

            if (element is AppBarButton abb)
                abb.LabelPosition = DefaultLabelPosition;
            else if (element is AppBarToggleButton atb)
                atb.LabelPosition = DefaultLabelPosition;
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

        private void AddOverflowItems(IReadOnlyList<ICommandBarElement> overflowedPrimaryCommands)
        {
            for (var i = 0; i < overflowedPrimaryCommands.Count; i++)
                _overflowItems.Add(overflowedPrimaryCommands[i]);

            if (overflowedPrimaryCommands.Count > 0 && HasVisibleElements(SecondaryCommands))
            {
                SetOverflowMode(_overflowPrimarySecondarySeparator, true);
                _overflowItems.Add(_overflowPrimarySecondarySeparator);
            }

            foreach (var item in SecondaryCommands)
            {
                SetOverflowMode(item, true);
                _overflowItems.Add(item);
            }
        }

        private void RebuildSecondaryCommandVisibilitySubscriptions()
        {
            _secondaryCommandVisibilitySubscriptions.Clear();

            if (SecondaryCommands is null)
                return;

            for (var i = 0; i < SecondaryCommands.Count; i++)
            {
                if (SecondaryCommands[i] is Avalonia.Visual visual)
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

                            UpdateDynamicOverflow();
                        })
                        .DisposeWith(_secondaryCommandVisibilitySubscriptions);
                }
            }
        }

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
                if (!visibleIndices.Contains(i) || commands[i] is not AppBarSeparator)
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

                if (commands[i] is AppBarSeparator)
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
                if (visibleIndices.Contains(i) && commands[i] is not AppBarSeparator)
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
