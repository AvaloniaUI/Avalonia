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

    public class Control : Interactive, ILayoutable, ILogical, IStyleable, IStyled
    {
        public static readonly PerspexProperty<Brush> BackgroundProperty =
            PerspexProperty.Register<Control, Brush>("Background", inherits: true);

        public static readonly PerspexProperty<Brush> BorderBrushProperty =
            PerspexProperty.Register<Control, Brush>("BorderBrush");

        public static readonly PerspexProperty<double> BorderThicknessProperty =
            PerspexProperty.Register<Control, double>("BorderThickness");

        public static readonly PerspexProperty<Brush> ForegroundProperty =
            PerspexProperty.Register<Control, Brush>("Foreground", new SolidColorBrush(0xff000000), true);

        public static readonly PerspexProperty<double> HeightProperty =
            PerspexProperty.Register<Control, double>("Height", double.NaN);

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

        public static readonly RoutedEvent<PointerEventArgs> PointerPressedEvent =
            RoutedEvent.Register<Control, PointerEventArgs>("PointerPressed", RoutingStrategy.Bubble);

        public static readonly RoutedEvent<PointerEventArgs> PointerReleasedEvent =
            RoutedEvent.Register<Control, PointerEventArgs>("PointerReleased", RoutingStrategy.Bubble);

        private Classes classes;

        private string id;

        private Styles styles;

        public Control()
        {
            this.classes = new Classes();

            this.GetObservable(IsPointerOverProperty).Subscribe(x =>
            {
                if (x)
                {
                    this.Classes.Add(":pointerover");
                }
                else
                {
                    this.Classes.Remove(":pointerover");
                }
            });

            // Hacky hack hack!
            this.GetObservable(BackgroundProperty).Skip(1).Subscribe(_ => this.InvalidateMeasure());
        }

        public event EventHandler<PointerEventArgs> PointerPressed
        {
            add
            {
                Contract.Requires<NullReferenceException>(value != null);
                this.AddHandler(PointerPressedEvent, value); 
            }

            remove 
            {
                Contract.Requires<NullReferenceException>(value != null);
                this.RemoveHandler(PointerPressedEvent, value); 
            }
        }

        public event EventHandler<PointerEventArgs> PointerReleased
        {
            add 
            {
                Contract.Requires<NullReferenceException>(value != null);
                this.AddHandler(PointerReleasedEvent, value); 
            }
            remove 
            {
                Contract.Requires<NullReferenceException>(value != null);
                this.RemoveHandler(PointerReleasedEvent, value); 
            }
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

        public Brush Foreground
        {
            get { return this.GetValue(ForegroundProperty); }
            set { this.SetValue(ForegroundProperty, value); }
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

        public double Height
        {
            get { return this.GetValue(HeightProperty); }
            set { this.SetValue(HeightProperty, value); }
        }

        public bool IsPointerOver
        {
            get { return this.GetValue(IsPointerOverProperty); }
            set { this.SetValue(IsPointerOverProperty, value); }
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

        public ILayoutRoot GetLayoutRoot()
        {
            return this.GetVisualAncestorOrSelf<ILayoutRoot>();
        }

        public void Arrange(Rect rect)
        {
            this.Bounds = new Rect(
                rect.Position,
                this.ArrangeContent(rect.Size.Deflate(this.Margin).Constrain(rect.Size)));
        }

        public void Measure(Size availableSize)
        {
            availableSize = availableSize.Deflate(this.Margin);
            this.DesiredSize = this.MeasureContent(availableSize).Constrain(availableSize);
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

            if (root != null)
            {
                root.LayoutManager.InvalidateMeasure(this);
            }
        }

        protected virtual Size ArrangeContent(Size finalSize)
        {
            return finalSize;
        }

        protected virtual Size MeasureContent(Size availableSize)
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
