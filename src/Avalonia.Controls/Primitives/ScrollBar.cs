// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Layout;

namespace Avalonia.Controls.Primitives
{
    public class ScrollEventArgs : EventArgs
    {
        public ScrollEventArgs(ScrollEventType eventType, double newValue)
        {
            ScrollEventType = eventType;
            NewValue = newValue;
        }
        public double NewValue { get; private set; }
        public ScrollEventType ScrollEventType { get; private set; }
    }

    /// <summary>
    /// A scrollbar control.
    /// </summary>
    public class ScrollBar : RangeBase
    {
        /// <summary>
        /// Defines the <see cref="ViewportSize"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ViewportSizeProperty =
            AvaloniaProperty.Register<ScrollBar, double>(nameof(ViewportSize), defaultValue: double.NaN);

        /// <summary>
        /// Defines the <see cref="Visibility"/> property.
        /// </summary>
        public static readonly StyledProperty<ScrollBarVisibility> VisibilityProperty =
            AvaloniaProperty.Register<ScrollBar, ScrollBarVisibility>(nameof(Visibility));

        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<ScrollBar, Orientation>(nameof(Orientation), Orientation.Vertical);

        private Button _lineUpButton;
        private Button _lineDownButton;
        private Button _pageUpButton;
        private Button _pageDownButton;

        /// <summary>
        /// Initializes static members of the <see cref="ScrollBar"/> class. 
        /// </summary>
        static ScrollBar()
        {
            PseudoClass<ScrollBar, Orientation>(OrientationProperty, o => o == Orientation.Vertical, ":vertical");
            PseudoClass<ScrollBar, Orientation>(OrientationProperty, o => o == Orientation.Horizontal, ":horizontal");

            Thumb.DragDeltaEvent.AddClassHandler<ScrollBar>(o => o.OnThumbDragDelta, RoutingStrategies.Bubble);
            Thumb.DragCompletedEvent.AddClassHandler<ScrollBar>(o => o.OnThumbDragComplete, RoutingStrategies.Bubble);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScrollBar"/> class.
        /// </summary>
        public ScrollBar()
        {
            var isVisible = Observable.Merge(
                this.GetObservable(MinimumProperty).Select(_ => Unit.Default),
                this.GetObservable(MaximumProperty).Select(_ => Unit.Default),
                this.GetObservable(ViewportSizeProperty).Select(_ => Unit.Default),
                this.GetObservable(VisibilityProperty).Select(_ => Unit.Default))
                .Select(_ => CalculateIsVisible());
            Bind(IsVisibleProperty, isVisible, BindingPriority.Style);
        }

        /// <summary>
        /// Gets or sets the amount of the scrollable content that is currently visible.
        /// </summary>
        public double ViewportSize
        {
            get { return GetValue(ViewportSizeProperty); }
            set { SetValue(ViewportSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the scrollbar should hide itself when it
        /// is not needed.
        /// </summary>
        public ScrollBarVisibility Visibility
        {
            get { return GetValue(VisibilityProperty); }
            set { SetValue(VisibilityProperty, value); }
        }

        /// <summary>
        /// Gets or sets the orientation of the scrollbar.
        /// </summary>
        public Orientation Orientation
        {
            get { return GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        public event EventHandler<ScrollEventArgs> Scroll;

        /// <summary>
        /// Calculates whether the scrollbar should be visible.
        /// </summary>
        /// <returns>The scrollbar's visibility.</returns>
        private bool CalculateIsVisible()
        {
            switch (Visibility)
            {
                case ScrollBarVisibility.Visible:
                    return true;

                case ScrollBarVisibility.Disabled:
                case ScrollBarVisibility.Hidden:
                    return false;

                case ScrollBarVisibility.Auto:
                    return double.IsNaN(ViewportSize) || Maximum > 0;

                default:
                    throw new InvalidOperationException("Invalid value for ScrollBar.Visibility.");
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.PageUp)
            {
                LargeDecrement();
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                LargeIncrement();
                e.Handled = true;
            }
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            if (_lineUpButton != null)
            {
                _lineUpButton.Click -= LineUpClick;
            }

            if (_lineDownButton != null)
            {
                _lineDownButton.Click -= LineDownClick;
            }

            if (_pageUpButton != null)
            {
                _pageUpButton.Click -= PageUpClick;
            }

            if (_pageDownButton != null)
            {
                _pageDownButton.Click -= PageDownClick;
            }

            _lineUpButton = e.NameScope.Find<Button>("PART_LineUpButton");
            _lineDownButton = e.NameScope.Find<Button>("PART_LineDownButton");
            _pageUpButton = e.NameScope.Find<Button>("PART_PageUpButton");
            _pageDownButton = e.NameScope.Find<Button>("PART_PageDownButton");



            if (_lineUpButton != null)
            {
                _lineUpButton.Click += LineUpClick;
            }

            if (_lineDownButton != null)
            {
                _lineDownButton.Click += LineDownClick;
            }

            if (_pageUpButton != null)
            {
                _pageUpButton.Click += PageUpClick;
            }

            if (_pageDownButton != null)
            {
                _pageDownButton.Click += PageDownClick;
            }
        }

        private void LineUpClick(object sender, RoutedEventArgs e)
        {
            SmallDecrement();
        }

        private void LineDownClick(object sender, RoutedEventArgs e)
        {
            SmallIncrement();
        }

        private void PageUpClick(object sender, RoutedEventArgs e)
        {
            LargeDecrement();
        }

        private void PageDownClick(object sender, RoutedEventArgs e)
        {
            LargeIncrement();
        }

        private void SmallDecrement()
        {
            Value = Math.Max(Value - SmallChange, Minimum);
            OnScroll(ScrollEventType.SmallDecrement);
        }

        private void SmallIncrement()
        {
            Value = Math.Min(Value + SmallChange, Maximum);
            OnScroll(ScrollEventType.SmallIncrement);
        }

        private void LargeDecrement()
        {
            Value = Math.Max(Value - LargeChange, Minimum);
            OnScroll(ScrollEventType.LargeDecrement);
        }

        private void LargeIncrement()
        {
            Value = Math.Min(Value + LargeChange, Maximum);
            OnScroll(ScrollEventType.LargeIncrement);
        }

        private void OnThumbDragDelta(VectorEventArgs e)
        {
            OnScroll(ScrollEventType.ThumbTrack);
        }
        private void OnThumbDragComplete(VectorEventArgs e)
        {
            OnScroll(ScrollEventType.EndScroll);
        }

        protected void OnScroll(ScrollEventType scrollEventType)
        {
            Scroll?.Invoke(this, new ScrollEventArgs(scrollEventType, Value));
        }
    }
}
