using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Reactive;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace Avalonia.Controls
{
    public enum PivotHeaderPlacement
    {
        Top,
        Bottom
    }

    /// <summary>
    /// A tab control that displays a tab strip along with the content of the selected tab.
    /// </summary>
    [TemplatePart("PART_Header", typeof(PivotHeader))]
    [TemplatePart("PART_Border", typeof(Border))]
    [TemplatePart("PART_ScrollViewer", typeof(ScrollViewer))]
    [TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
    public class Pivot : SelectingItemsControl
    {
        /// <summary>
        /// Defines the <see cref="PivotHeaderPlacement"/> property.
        /// </summary>
        public static readonly StyledProperty<PivotHeaderPlacement> PivotHeaderPlacementProperty =
            AvaloniaProperty.Register<Pivot, PivotHeaderPlacement>(nameof(PivotHeaderPlacement), defaultValue: PivotHeaderPlacement.Top);

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<Pivot>();

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<Pivot>();

        /// <summary>
        /// Defines the <see cref="HeaderDisplayMemberBinding" /> property
        /// </summary>
        public static readonly StyledProperty<IBinding?> HeaderDisplayMemberBindingProperty =
            AvaloniaProperty.Register<ItemsControl, IBinding?>(nameof(HeaderDisplayMemberBinding));

        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<Panel> DefaultPanel =
            new FuncTemplate<Panel>(() => new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                AreHorizontalSnapPointsRegular = true
            });

        /// <summary>
        /// Defines the <see cref="HeaderTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
            AvaloniaProperty.Register<HeaderedContentControl, IDataTemplate?>(nameof(HeaderTemplate));

        private ImplicitAnimationCollection? _implicitAnimations;
        private bool _animationsDisabled;

        /// <summary>
        /// Initializes static members of the <see cref="Pivot"/> class.
        /// </summary>
        static Pivot()
        {
            SelectionModeProperty.OverrideDefaultValue<Pivot>(SelectionMode.AlwaysSelected);
            ItemsPanelProperty.OverrideDefaultValue<Pivot>(DefaultPanel);
            AffectsMeasure<Pivot>(PivotHeaderPlacementProperty);
            SelectedItemProperty.Changed.AddClassHandler<Pivot>((x, e) => x.UpdateHeaderSelection());
            AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<Pivot>(AutomationControlType.Tab);
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the control.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the control.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get { return GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the PivotHeader placement of the Pivot.
        /// </summary>
        public PivotHeaderPlacement PivotHeaderPlacement
        {
            get { return GetValue(PivotHeaderPlacementProperty); }
            set { SetValue(PivotHeaderPlacementProperty, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="IBinding"/> to use for binding to the header member of each item.
        /// </summary>
        [AssignBinding]
        public IBinding? HeaderDisplayMemberBinding
        {
            get { return GetValue(HeaderDisplayMemberBindingProperty); }
            set { SetValue(HeaderDisplayMemberBindingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the data template used to display the header content of the control.
        /// </summary>
        public IDataTemplate? HeaderTemplate
        {
            get => GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        internal PivotHeader? HeaderPart { get; private set; }
        internal ItemsPresenter? ItemsPresenterPart { get; private set; }
        internal Border? BorderPart { get; private set; }
        internal ScrollViewer? ScrollViewerPart { get; private set; }

        protected internal override Control CreateContainerForItemOverride() => new PivotItem();
        protected internal override bool IsItemItsOwnContainerOverride(Control item) => item is PivotItem;

        protected internal override void PrepareContainerForItemOverride(Control element, object? item, int index)
        {
            base.PrepareContainerForItemOverride(element, item, index);

            if (element is PivotItem pivotItem)
            {
                if (ItemTemplate is { } it)
                    pivotItem.ContentTemplate = it;

                pivotItem.HorizontalAlignment = HorizontalAlignment.Stretch;
                pivotItem.VerticalAlignment = VerticalAlignment.Stretch;
            }

            if (index == SelectedIndex && element is ContentControl container)
            {
                UpdateHeaderSelection();
            }
        }

        protected internal override void ClearContainerForItemOverride(Control element)
        {
            base.ClearContainerForItemOverride(element);
            UpdateHeaderSelection();
        }

        private void UpdateHeaderSelection()
        {
            if (HeaderPart != null)
            {
                HeaderPart.SelectedIndex = SelectedIndex;
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            HeaderPart = e.NameScope.Get<PivotHeader>("PART_Header");
            ItemsPresenterPart = e.NameScope.Get<ItemsPresenter>("PART_ItemsPresenter");

            ItemsPresenterPart.Loaded += (s, e) =>
            {
                if (ItemsPresenterPart != null)
                {
                    var composition = ElementComposition.GetElementVisual(ItemsPresenterPart);

                    if (composition != null)
                    {
                        if (_implicitAnimations == null)
                        {
                            var compositor = composition.Compositor;

                            var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
                            offsetAnimation.Target = "Offset";
                            offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
                            offsetAnimation.Duration = TimeSpan.FromMilliseconds(250);

                            _implicitAnimations = compositor.CreateImplicitAnimationCollection();
                            _implicitAnimations["Offset"] = offsetAnimation;
                        }

                        composition.ImplicitAnimations = _implicitAnimations;
                    }
                }
            };

            if (HeaderPart != null)
            {
                HeaderPart.AddHandler(SelectionChangedEvent, (o, e) => SelectedIndex = HeaderPart.SelectedIndex);

                HeaderPart.SetValue(PivotHeader.PivotHeaderPlacementProperty, PivotHeaderPlacement);

                HeaderPart.DisplayMemberBinding = HeaderDisplayMemberBinding;

                SetHeaderItems();
            }

            BorderPart = e.NameScope.Find<Border>("PART_Border");
            ScrollViewerPart = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");

            if(BorderPart != null)
            {
                BorderPart.AddHandler(Gestures.ScrollGestureEvent, ScrollEventHandler, handledEventsToo:true);
                BorderPart.AddHandler(Gestures.ScrollGestureEndedEvent, ScrollEndedEventHandler, handledEventsToo: true);
            }
        }

        private void ScrollEndedEventHandler(object? sender, ScrollGestureEndedEventArgs e)
        {
            UpdateAnimation(true);

            if (ItemsPresenterPart != null)
            {
                if (ScrollViewerPart != null && BorderPart != null)
                {
                    var xOffset = ScrollViewerPart.Offset.X;

                    var index = xOffset / (long)BorderPart.Bounds.Width;

                    SelectedIndex = (int)Math.Max(0, index);
                }
            }
        }

        private void ScrollEventHandler(object? sender, ScrollGestureEventArgs e)
        {
            UpdateAnimation(false);
        }

        private void UpdateAnimation(bool enable)
        {
            if (ItemsPresenterPart != null && (!_animationsDisabled || enable))
            {
                var composition = ElementComposition.GetElementVisual(ItemsPresenterPart);

                if (composition != null)
                {
                    if (_implicitAnimations != null)
                    {
                        composition.ImplicitAnimations = enable ? _implicitAnimations : null;

                        _animationsDisabled = !enable;
                    }
                }
            }
        }

        protected override void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
        {
            base.ContainerIndexChangedOverride(container, oldIndex, newIndex);

            var selectedIndex = SelectedIndex;

            if (selectedIndex == oldIndex || selectedIndex == newIndex)
                UpdateHeaderSelection();
        }

        /// <inheritdoc/>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            if (e.NavigationMethod == NavigationMethod.Directional)
            {
                e.Handled = UpdateSelectionFromEventSource(e.Source);
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Pointer.Type == PointerType.Mouse)
            {
                e.Handled = UpdateSelectionFromEventSource(e.Source);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = base.ArrangeOverride(finalSize);

            EnsureSelectionInView();

            return size;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if(change.Property == SelectedIndexProperty)
            {
                EnsureSelectionInView();
            }
            else if(change.Property == HeaderDisplayMemberBindingProperty)
            {
                if (HeaderPart != null)
                    HeaderPart.DisplayMemberBinding = HeaderDisplayMemberBinding;
            }
            else if(change.Property == PivotHeaderPlacementProperty)
            {
                HeaderPart?.SetValue(PivotHeader.PivotHeaderPlacementProperty, PivotHeaderPlacement);
            }
        }

        private void EnsureSelectionInView()
        {
            if (SelectedIndex > -1)
            {
                ItemsPresenterPart?.ScrollIntoView(SelectedIndex);
            }
        }

        protected override void ItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            base.ItemsCollectionChanged(sender, e);

            SetHeaderItems();
        }

        private void SetHeaderItems()
        {
            if (HeaderPart != null)
            {
                List<object> headers = new List<object>();

                if(Items == null)
                {
                    return;
                }

                foreach (var item in Items)
                {
                    if (item is IHeadered headered)
                    {
                        var header = new PivotHeaderItem()
                        {
                            Content = headered.Header,
                        };

                        if (headered is HeaderedContentControl contentControl)
                        {
                            header.ContentTemplate = contentControl.HeaderTemplate;
                        }

                        headers.Add(header);
                    }
                    else
                    {
                        headers.Add(item);
                    }
                }

                HeaderPart.Items = headers;

                UpdateHeaderSelection();
            }
        }
    }
}
