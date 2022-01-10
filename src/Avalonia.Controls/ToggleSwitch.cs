using System;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// A Toggle Switch control.
    /// </summary>
    [PseudoClasses(":dragging")]
    public class ToggleSwitch : ToggleButton
    {
        private Panel _knobsPanel;
        private Panel _switchKnob;
        private bool _knobsPanelPressed = false;
        private Point _switchStartPoint = new Point();
        private double _initLeft = -1;
        private bool _isDragging = false;
        private ILogical _onChild;
        private ILogical _offChild;

        static ToggleSwitch()
        {
            IsCheckedProperty.Changed.AddClassHandler<ToggleSwitch>((x, e) =>
            {
                if ((e.NewValue != null) && (e.NewValue is bool val))
                {
                    x.UpdateKnobPos(val);
                }
            });

            BoundsProperty.Changed.AddClassHandler<ToggleSwitch>((x, e) =>
            {
                if (x.IsChecked != null)
                {
                    x.UpdateKnobPos(x.IsChecked.Value);
                }
            });
        }

        /// <summary>
        /// Defines the <see cref="OffContent"/> property.
        /// </summary>
        public static readonly StyledProperty<object> OffContentProperty =
         AvaloniaProperty.Register<ToggleSwitch, object>(nameof(OffContent), defaultValue: "Off");

        /// <summary>
        /// Defines the <see cref="OffContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> OffContentTemplateProperty =
            AvaloniaProperty.Register<ToggleSwitch, IDataTemplate>(nameof(OffContentTemplate));

        /// <summary>
        /// Defines the <see cref="OnContent"/> property.
        /// </summary>
        public static readonly StyledProperty<object> OnContentProperty =
            AvaloniaProperty.Register<ToggleSwitch, object>(nameof(OnContent), defaultValue: "On");

        /// <summary>
        /// Defines the <see cref="OnContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> OnContentTemplateProperty =
            AvaloniaProperty.Register<ToggleSwitch, IDataTemplate>(nameof(OnContentTemplate));

        /// <summary>
        /// Gets or Sets the Content that is displayed when in the On State.
        /// </summary>
        public object OnContent
        {
            get { return GetValue(OnContentProperty); }
            set { SetValue(OnContentProperty, value); }
        }

        /// <summary>
        /// Gets or Sets the Content that is displayed when in the Off State.
        /// </summary>
        public object OffContent
        {
            get { return GetValue(OffContentProperty); }
            set { SetValue(OffContentProperty, value); }
        }

        public IContentPresenter OffContentPresenter
        {
            get;
            private set;
        }

        public IContentPresenter OnContentPresenter
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or Sets the <see cref="IDataTemplate"/> used to display the <see cref="OffContent"/>.
        /// </summary>
        public IDataTemplate OffContentTemplate
        {
            get { return GetValue(OffContentTemplateProperty); }
            set { SetValue(OffContentTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or Sets the <see cref="IDataTemplate"/> used to display the <see cref="OnContent"/>.
        /// </summary>
        public IDataTemplate OnContentTemplate
        {
            get { return GetValue(OnContentTemplateProperty); }
            set { SetValue(OnContentTemplateProperty, value); }
        }

        protected override int LogicalChildrenCount
        {
            get
            {
                var result = base.LogicalChildrenCount;
                if (_onChild is not null)
                    ++result;
                if (_offChild is not null)
                    ++result;
                return result;
            }
        }

        protected override ILogical GetLogicalChild(int index)
        {
            var baseCount = base.LogicalChildrenCount;
            if (index < baseCount)
                return base.GetLogicalChild(index);
            
            index -= baseCount;

            if (_onChild is not null && index-- == 0)
                return _onChild;
            if (_offChild is not null && index-- == 0)
                return _offChild;

            throw new IndexOutOfRangeException(nameof(index));
        }

        protected override void RegisterContentPresenter(IContentPresenter presenter)
        {
            base.RegisterContentPresenter(presenter);

            if (presenter.Name == "Part_OnContentPresenter")
                OnContentPresenter = presenter;
            else if (presenter.Name == "PART_OffContentPresenter")
                OffContentPresenter = presenter;
        }

        protected override void RegisterLogicalChild(IContentPresenter presenter, ILogical child)
        {
            if (presenter == OnContentPresenter)
                SetLogicalChild(ref _onChild, child);
            else if (presenter == OffContentPresenter)
                SetLogicalChild(ref _offChild, child);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _switchKnob = e.NameScope.Find<Panel>("SwitchKnob");
            _knobsPanel = e.NameScope.Find<Panel>("MovingKnobs");
            
            _knobsPanel.PointerPressed += KnobsPanel_PointerPressed;
            _knobsPanel.PointerReleased += KnobsPanel_PointerReleased;
            _knobsPanel.PointerMoved += KnobsPanel_PointerMoved;

            if (IsChecked.HasValue)
            {
                UpdateKnobPos(IsChecked.Value);
            }
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == OnContentProperty)
                SetLogicalChild(ref _onChild, change.NewValue.GetValueOrDefault<ILogical>());
            if (change.Property == OffContentProperty)
                SetLogicalChild(ref _offChild, change.NewValue.GetValueOrDefault<ILogical>());
        }

        private void KnobsPanel_PointerPressed(object sender, Input.PointerPressedEventArgs e)
        {
            _switchStartPoint = e.GetPosition(_switchKnob);
            _initLeft = Canvas.GetLeft(_knobsPanel);
            _isDragging = false;
            _knobsPanelPressed = true;
        }

        private void KnobsPanel_PointerReleased(object sender, Input.PointerReleasedEventArgs e)
        {
            if (_isDragging)
            {
                bool shouldBecomeChecked = Canvas.GetLeft(_knobsPanel) >= (_switchKnob.Bounds.Width / 2);
                _knobsPanel.ClearValue(Canvas.LeftProperty);

                PseudoClasses.Set(":dragging", false);

                if (shouldBecomeChecked == IsChecked)
                {
                    UpdateKnobPos(shouldBecomeChecked);
                }
                else
                {
                    IsChecked = shouldBecomeChecked;
                }
            }
            else
            {
                base.Toggle();
            }

            _isDragging = false;

            _knobsPanelPressed = false;
        }

        private void KnobsPanel_PointerMoved(object sender, Input.PointerEventArgs e)
        {
            if (_knobsPanelPressed)
            {
                var difference = e.GetPosition(_switchKnob) - _switchStartPoint;

                if ((!_isDragging) && (System.Math.Abs(difference.X) > 3))
                {
                    _isDragging = true;
                    PseudoClasses.Set(":dragging", true);
                }

                if (_isDragging)
                {
                    Canvas.SetLeft(_knobsPanel, System.Math.Min(_switchKnob.Bounds.Width, System.Math.Max(0, (_initLeft + difference.X))));
                }
            }
        }

        protected override void Toggle()
        {
            if ((_switchKnob != null) && (!_switchKnob.IsPointerOver))
            {
                base.Toggle();
            }
        }

        protected void UpdateKnobPos(bool value)
        {
            if ((_switchKnob != null) && (_knobsPanel != null))
            {
                if (value)
                {
                    Canvas.SetLeft(_knobsPanel, _switchKnob.Bounds.Width);
                }
                else
                {
                    Canvas.SetLeft(_knobsPanel, 0);
                }
            }
        }

        private void SetLogicalChild(ref ILogical field, ILogical child)
        {
            if (field != child)
            {
                if (field?.LogicalParent == this)
                    ((ISetLogicalParent)field).SetParent(null);

                field = child;

                if (field is not null && field.LogicalParent is null)
                    ((ISetLogicalParent)field).SetParent(this);

                OnLogicalChildrenChanged(EventArgs.Empty);
            }
        }
    }
}

