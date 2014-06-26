// -----------------------------------------------------------------------
// <copyright file="Control.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Input;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.Styling;
    using Splat;

    public enum HorizontalAlignment
    {
        Stretch,
        Left,
        Center,
        Right,
    }

    public enum VerticalAlignment
    {
        Stretch,
        Top,
        Center,
        Bottom,
    }

    public class Control : Interactive, ILayoutable, IFocusable, ILogical, IStyleable, IStyled
    {
        public static readonly PerspexProperty<Brush> BackgroundProperty =
            PerspexProperty.Register<Control, Brush>("Background", inherits: true);

        public static readonly PerspexProperty<Brush> BorderBrushProperty =
            PerspexProperty.Register<Control, Brush>("BorderBrush");

        public static readonly PerspexProperty<double> BorderThicknessProperty =
            PerspexProperty.Register<Control, double>("BorderThickness");

        public static readonly PerspexProperty<bool> FocusableProperty =
            PerspexProperty.Register<Control, bool>("Focusable");

        public static readonly PerspexProperty<double> FontSizeProperty =
            PerspexProperty.Register<Control, double>(
                "FontSize",
                defaultValue: 12.0,
                inherits: true);

        public static readonly PerspexProperty<Brush> ForegroundProperty =
            PerspexProperty.Register<Control, Brush>("Foreground", new SolidColorBrush(0xff000000), true);

        public static readonly PerspexProperty<double> HeightProperty =
            PerspexProperty.Register<Control, double>("Height", double.NaN);

        public static readonly PerspexProperty<bool> IsFocusedProperty =
            PerspexProperty.Register<Control, bool>("IsFocused", false);

        public static readonly PerspexProperty<bool> IsPointerOverProperty =
            PerspexProperty.Register<Control, bool>("IsPointerOver");

        public static readonly PerspexProperty<HorizontalAlignment> HorizontalAlignmentProperty =
            PerspexProperty.Register<Control, HorizontalAlignment>("HorizontalAlignment");

        public static readonly PerspexProperty<Thickness> MarginProperty =
            PerspexProperty.Register<Control, Thickness>("Margin");

        public static readonly PerspexProperty<double> MaxHeightProperty =
            PerspexProperty.Register<Control, double>("MaxHeight", double.PositiveInfinity);

        public static readonly PerspexProperty<double> MaxWidthProperty =
            PerspexProperty.Register<Control, double>("MaxWidth", double.PositiveInfinity);

        public static readonly PerspexProperty<double> MinHeightProperty =
            PerspexProperty.Register<Control, double>("MinHeight");

        public static readonly PerspexProperty<double> MinWidthProperty =
            PerspexProperty.Register<Control, double>("MinWidth");

        public static readonly PerspexProperty<Control> ParentProperty =
            PerspexProperty.Register<Control, Control>("Parent");

        public static readonly PerspexProperty<VerticalAlignment> VerticalAlignmentProperty =
            PerspexProperty.Register<Control, VerticalAlignment>("VerticalAlignment");

        public static readonly PerspexProperty<double> WidthProperty =
            PerspexProperty.Register<Control, double>("Width", double.NaN);

        public static readonly RoutedEvent<RoutedEventArgs> GotFocusEvent =
            RoutedEvent.Register<Control, RoutedEventArgs>("GotFocus", RoutingStrategy.Bubble);

        public static readonly RoutedEvent<RoutedEventArgs> LostFocusEvent =
            RoutedEvent.Register<Control, RoutedEventArgs>("LostFocus", RoutingStrategy.Bubble);

        public static readonly RoutedEvent<KeyEventArgs> KeyDownEvent =
            RoutedEvent.Register<Control, KeyEventArgs>("KeyDown", RoutingStrategy.Bubble);

        public static readonly RoutedEvent<PointerEventArgs> PointerEnterEvent =
            RoutedEvent.Register<Control, PointerEventArgs>("PointerEnter", RoutingStrategy.Direct);

        public static readonly RoutedEvent<PointerEventArgs> PointerLeaveEvent =
            RoutedEvent.Register<Control, PointerEventArgs>("PointerLeave", RoutingStrategy.Direct);

        public static readonly RoutedEvent<PointerEventArgs> PointerPressedEvent =
            RoutedEvent.Register<Control, PointerEventArgs>("PointerPressed", RoutingStrategy.Bubble);

        public static readonly RoutedEvent<PointerEventArgs> PointerReleasedEvent =
            RoutedEvent.Register<Control, PointerEventArgs>("PointerReleased", RoutingStrategy.Bubble);

        private Classes classes;

        private string id;

        private Styles styles;

        static Control()
        {
            AffectsMeasure(IsVisibleProperty);
        }

        public Control()
        {
            this.classes = new Classes();
            this.GotFocus += (s, e) => this.IsFocused = true;
            this.LostFocus += (s, e) => this.IsFocused = false;
            this.PointerEnter += (s, e) => this.IsPointerOver = true;
            this.PointerLeave += (s, e) => this.IsPointerOver = false;
            this.AddPseudoClass(IsPointerOverProperty, ":pointerover");
            this.AddPseudoClass(IsFocusedProperty, ":focus");
        }

        public event EventHandler<RoutedEventArgs> GotFocus
        {
            add { this.AddHandler(GotFocusEvent, value); }
            remove { this.RemoveHandler(GotFocusEvent, value); }
        }

        public event EventHandler<RoutedEventArgs> LostFocus
        {
            add { this.AddHandler(LostFocusEvent, value); }
            remove { this.RemoveHandler(LostFocusEvent, value); }
        }

        public event EventHandler<KeyEventArgs> KeyDown
        {
            add { this.AddHandler(KeyDownEvent, value); }
            remove { this.RemoveHandler(KeyDownEvent, value); }
        }

        public event EventHandler<PointerEventArgs> PointerEnter
        {
            add { this.AddHandler(PointerEnterEvent, value); }
            remove { this.RemoveHandler(PointerEnterEvent, value); }
        }

        public event EventHandler<PointerEventArgs> PointerLeave
        {
            add { this.AddHandler(PointerLeaveEvent, value); }
            remove { this.RemoveHandler(PointerLeaveEvent, value); }
        }

        public event EventHandler<PointerEventArgs> PointerPressed
        {
            add { this.AddHandler(PointerPressedEvent, value); }
            remove { this.RemoveHandler(PointerPressedEvent, value); }
        }

        public event EventHandler<PointerEventArgs> PointerReleased
        {
            add { this.AddHandler(PointerReleasedEvent, value); }
            remove { this.RemoveHandler(PointerReleasedEvent, value); }
        }

        public Size ActualSize
        {
            get { return ((IVisual)this).Bounds.Size; }
        }

        public Brush Background
        {
            get { return this.GetValue(BackgroundProperty); }
            set { this.SetValue(BackgroundProperty, value); }
        }

        public Brush BorderBrush
        {
            get { return this.GetValue(BorderBrushProperty); }
            set { this.SetValue(BorderBrushProperty, value); }
        }

        public double BorderThickness
        {
            get { return this.GetValue(BorderThicknessProperty); }
            set { this.SetValue(BorderThicknessProperty, value); }
        }

        public Classes Classes
        {
            get 
            {
                return this.classes; 
            }
            
            set
            {
                if (this.classes != value)
                {
                    this.classes.Clear();
                    this.classes.Add(value);
                }
            }
        }

        public Size? DesiredSize
        {
            get;
            set;
        }

        public double FontSize
        {
            get { return this.GetValue(FontSizeProperty); }
            set { this.SetValue(FontSizeProperty, value); }
        }

        public bool Focusable
        {
            get { return this.GetValue(FocusableProperty); }
            set { this.SetValue(FocusableProperty, value); }
        }

        public Brush Foreground
        {
            get { return this.GetValue(ForegroundProperty); }
            set { this.SetValue(ForegroundProperty, value); }
        }

        public double Height
        {
            get { return this.GetValue(HeightProperty); }
            set { this.SetValue(HeightProperty, value); }
        }

        public bool IsFocused
        {
            get { return this.GetValue(IsFocusedProperty); }
            private set { this.SetValue(IsFocusedProperty, value); }
        }

        public string Id
        {
            get
            {
                return this.id;
            }

            set
            {
                if (this.id != null)
                {
                    throw new InvalidOperationException("ID already set.");
                }

                if (((IVisual)this).VisualParent != null)
                {
                    throw new InvalidOperationException("Cannot set ID : control already added to tree.");
                }

                this.id = value;
            }
        }

        public bool IsPointerOver
        {
            get { return this.GetValue(IsPointerOverProperty); }
            internal set { this.SetValue(IsPointerOverProperty, value); }
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get { return this.GetValue(HorizontalAlignmentProperty); }
            set { this.SetValue(HorizontalAlignmentProperty, value); }
        }

        public Thickness Margin
        {
            get { return this.GetValue(MarginProperty); }
            set { this.SetValue(MarginProperty, value); }
        }

        public double MaxHeight
        {
            get { return this.GetValue(MaxHeightProperty); }
            set { this.SetValue(MaxHeightProperty, value); }
        }

        public double MaxWidth
        {
            get { return this.GetValue(MaxWidthProperty); }
            set { this.SetValue(MaxWidthProperty, value); }
        }

        public double MinHeight
        {
            get { return this.GetValue(MinHeightProperty); }
            set { this.SetValue(MinHeightProperty, value); }
        }

        public double MinWidth
        {
            get { return this.GetValue(MinWidthProperty); }
            set { this.SetValue(MinWidthProperty, value); }
        }

        public Control Parent
        {
            get { return this.GetValue(ParentProperty); }
            protected set { this.SetValue(ParentProperty, value); }
        }

        public Styles Styles
        {
            get
            {
                if (this.styles == null)
                {
                    this.styles = new Styles();
                }

                return this.styles;
            }

            set
            {
                this.styles = value;
            }
        }

        public ITemplatedControl TemplatedParent
        {
            get;
            internal set;
        }

        public VerticalAlignment VerticalAlignment
        {
            get { return this.GetValue(VerticalAlignmentProperty); }
            set { this.SetValue(VerticalAlignmentProperty, value); }
        }

        public double Width
        {
            get { return this.GetValue(WidthProperty); }
            set { this.SetValue(WidthProperty, value); }
        }

        ILogical ILogical.LogicalParent
        {
            get { return this.Parent; }
            set { this.Parent = (Control)value; }
        }

        IEnumerable<ILogical> ILogical.LogicalChildren
        {
            get { return Enumerable.Empty<ILogical>(); }
        }

        protected static void AffectsArrange(PerspexProperty property)
        {
            property.Changed.Subscribe(AffectsArrangeInvalidate);
        }

        protected static void AffectsMeasure(PerspexProperty property)
        {
            property.Changed.Subscribe(AffectsMeasureInvalidate);
        }

        private static void AffectsArrangeInvalidate(PerspexPropertyChangedEventArgs e)
        {
            Control control = e.Sender as Control;

            if (control != null)
            {
                control.InvalidateArrange();
            }
        }

        private static void AffectsMeasureInvalidate(PerspexPropertyChangedEventArgs e)
        {
            Control control = e.Sender as Control;

            if (control != null)
            {
                control.InvalidateMeasure();
            }
        }

        public ILayoutRoot GetLayoutRoot()
        {
            return this.GetVisualAncestorOrSelf<ILayoutRoot>();
        }

        public void Arrange(Rect rect)
        {
            this.ArrangeCore(rect);
        }

        public void Measure(Size availableSize)
        {
            availableSize = availableSize.Deflate(this.Margin);
            this.DesiredSize = this.MeasureCore(availableSize).Constrain(availableSize);
        }

        public void Focus()
        {
            Locator.Current.GetService<IFocusManager>().Focus(this);
        }

        public void InvalidateArrange()
        {
            ILayoutRoot root = this.GetLayoutRoot();

            if (root != null)
            {
                root.LayoutManager.InvalidateArrange(this);
            }
        }

        public void InvalidateMeasure()
        {
            ILayoutRoot root = this.GetLayoutRoot();

            if (root != null && root.LayoutManager != null)
            {
                root.LayoutManager.InvalidateMeasure(this);
            }
        }

        protected void AddPseudoClass(PerspexProperty<bool> property, string className)
        {
            this.GetObservable(property).Subscribe(x =>
            {
                if (x)
                {
                    this.classes.Add(className);
                }
                else
                {
                    this.classes.Remove(className);
                }
            });
        }

        protected virtual void ArrangeCore(Rect finalRect)
        {
            double originX = finalRect.X + this.Margin.Left;
            double originY = finalRect.Y + this.Margin.Top;
            double sizeX = Math.Max(0, finalRect.Width - this.Margin.Left - this.Margin.Right);
            double sizeY = Math.Max(0, finalRect.Height - this.Margin.Top - this.Margin.Bottom);

            if (this.HorizontalAlignment != HorizontalAlignment.Stretch)
            {
                sizeX = Math.Min(sizeX, this.DesiredSize.Value.Width);
            }

            if (this.VerticalAlignment != VerticalAlignment.Stretch)
            {
                sizeY = Math.Min(sizeY, this.DesiredSize.Value.Height);
            }

            Size taken = this.ArrangeOverride(new Size(sizeX, sizeY));

            sizeX = Math.Min(taken.Width, sizeX);
            sizeY = Math.Min(taken.Height, sizeY);

            switch (this.HorizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    originX += (finalRect.Width - sizeX) / 2;
                    break;
                case HorizontalAlignment.Right:
                    originX += finalRect.Width - sizeX;
                    break;
            }

            switch (this.VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    originY += (finalRect.Height - sizeY) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    originY += finalRect.Height - sizeY;
                    break;
            }

            ((IVisual)this).Bounds = new Rect(originX, originY, sizeX, sizeY);
        }

        protected virtual Size ArrangeOverride(Size finalSize)
        {
            return finalSize;
        }

        protected virtual Size MeasureCore(Size availableSize)
        {
            if (this.IsVisible)
            {
                Size measuredSize = this.MeasureOverride(availableSize.Deflate(this.Margin));
                double width = (this.Width > 0) ? this.Width : measuredSize.Width;
                double height = (this.Height > 0) ? this.Height : measuredSize.Height;

                width = Math.Min(width, this.MaxWidth);
                width = Math.Max(width, this.MinWidth);
                height = Math.Min(height, this.MaxHeight);
                height = Math.Max(height, this.MinHeight);

                return new Size(width, height);
            }
            else
            {
                return new Size();
            }
        }

        protected virtual Size MeasureOverride(Size availableSize)
        {
            return new Size();
        }

        protected override void AttachedToVisualTree()
        {
            IStyler styler = Locator.Current.GetService<IStyler>();
            styler.ApplyStyles(this);
            base.AttachedToVisualTree();
        }
    }
}
