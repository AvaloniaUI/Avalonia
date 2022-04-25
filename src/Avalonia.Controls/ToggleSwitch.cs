﻿using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// A Toggle Switch control.
    /// </summary>
    [TemplatePart("MovingKnobs", typeof(Panel))]
    [TemplatePart("SwitchKnob",  typeof(Panel))]
    [PseudoClasses(":dragging")]
    public class ToggleSwitch : ToggleButton
    {
        private Panel? _knobsPanel;
        private Panel? _switchKnob;
        private bool _knobsPanelPressed = false;
        private Point _switchStartPoint = new Point();
        private double _initLeft = -1;
        private bool _isDragging = false;

        static ToggleSwitch()
        {
            OffContentProperty.Changed.AddClassHandler<ToggleSwitch>((x, e) => x.OffContentChanged(e));
            OnContentProperty.Changed.AddClassHandler<ToggleSwitch>((x, e) => x.OnContentChanged(e));
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
        public static readonly StyledProperty<object?> OffContentProperty =
         AvaloniaProperty.Register<ToggleSwitch, object?>(nameof(OffContent), defaultValue: "Off");

        /// <summary>
        /// Defines the <see cref="OffContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> OffContentTemplateProperty =
            AvaloniaProperty.Register<ToggleSwitch, IDataTemplate?>(nameof(OffContentTemplate));

        /// <summary>
        /// Defines the <see cref="OnContent"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> OnContentProperty =
            AvaloniaProperty.Register<ToggleSwitch, object?>(nameof(OnContent), defaultValue: "On");

        /// <summary>
        /// Defines the <see cref="OnContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> OnContentTemplateProperty =
            AvaloniaProperty.Register<ToggleSwitch, IDataTemplate?>(nameof(OnContentTemplate));

        /// <summary>
        /// Gets or Sets the Content that is displayed when in the On State.
        /// </summary>
        public object? OnContent
        {
            get { return GetValue(OnContentProperty); }
            set { SetValue(OnContentProperty, value); }
        }

        /// <summary>
        /// Gets or Sets the Content that is displayed when in the Off State.
        /// </summary>
        public object? OffContent
        {
            get { return GetValue(OffContentProperty); }
            set { SetValue(OffContentProperty, value); }
        }

        public IContentPresenter? OffContentPresenter
        {
            get;
            private set;
        }

        public IContentPresenter? OnContentPresenter
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or Sets the <see cref="IDataTemplate"/> used to display the <see cref="OffContent"/>.
        /// </summary>
        public IDataTemplate? OffContentTemplate
        {
            get { return GetValue(OffContentTemplateProperty); }
            set { SetValue(OffContentTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or Sets the <see cref="IDataTemplate"/> used to display the <see cref="OnContent"/>.
        /// </summary>
        public IDataTemplate? OnContentTemplate
        {
            get { return GetValue(OnContentTemplateProperty); }
            set { SetValue(OnContentTemplateProperty, value); }
        }

        private void OffContentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue is ILogical oldChild)
            {
                LogicalChildren.Remove(oldChild);
            }

            if (e.NewValue is ILogical newChild)
            {
                LogicalChildren.Add(newChild);
            }
        }

        private void OnContentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue is ILogical oldChild)
            {
                LogicalChildren.Remove(oldChild);
            }

            if (e.NewValue is ILogical newChild)
            {
                LogicalChildren.Add(newChild);
            }
        }

        protected override bool RegisterContentPresenter(IContentPresenter presenter)
        {
            var result = base.RegisterContentPresenter(presenter);

            if (presenter.Name == "Part_OnContentPresenter")
            {
                OnContentPresenter = presenter;
                result = true;
            }
            else if (presenter.Name == "PART_OffContentPresenter")
            {
                OffContentPresenter = presenter;
                result = true;
            }

            return result;
        }


        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _switchKnob = e.NameScope.Find<Panel>("SwitchKnob");
            _knobsPanel = e.NameScope.Get<Panel>("MovingKnobs");
            
            _knobsPanel.PointerPressed += KnobsPanel_PointerPressed;
            _knobsPanel.PointerReleased += KnobsPanel_PointerReleased;
            _knobsPanel.PointerMoved += KnobsPanel_PointerMoved;

            if (IsChecked.HasValue)
            {
                UpdateKnobPos(IsChecked.Value);
            }
        }
        
        private void KnobsPanel_PointerPressed(object? sender, Input.PointerPressedEventArgs e)
        {
            _switchStartPoint = e.GetPosition(_switchKnob);
            _initLeft = Canvas.GetLeft(_knobsPanel!);
            _isDragging = false;
            _knobsPanelPressed = true;
        }

        private void KnobsPanel_PointerReleased(object? sender, Input.PointerReleasedEventArgs e)
        {
            if (_isDragging)
            {
                bool shouldBecomeChecked = Canvas.GetLeft(_knobsPanel!) >= (_switchKnob!.Bounds.Width / 2);
                _knobsPanel!.ClearValue(Canvas.LeftProperty);

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

        private void KnobsPanel_PointerMoved(object? sender, Input.PointerEventArgs e)
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
                    Canvas.SetLeft(_knobsPanel!, System.Math.Min(_switchKnob!.Bounds.Width, System.Math.Max(0, (_initLeft + difference.X))));
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
    }
}

