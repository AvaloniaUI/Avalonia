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

    public class Control : Interactive, ILayoutable, IStyleable, IStyled
    {
        public static readonly PerspexProperty<Brush> BackgroundProperty =
            PerspexProperty.Register<Control, Brush>("Background", inherits: true);

        public static readonly PerspexProperty<Brush> BorderBrushProperty =
            PerspexProperty.Register<Control, Brush>("BorderBrush");

        public static readonly PerspexProperty<double> BorderThicknessProperty =
            PerspexProperty.Register<Control, double>("BorderThickness");

        public static readonly PerspexProperty<Brush> ForegroundProperty =
            PerspexProperty.Register<Control, Brush>("Foreground", new SolidColorBrush(0xff000000), true);

        public static readonly PerspexProperty<bool> IsMouseOverProperty =
            PerspexProperty.Register<Visual, bool>("IsMouseOver");

        public static readonly PerspexProperty<HorizontalAlignment> HorizontalAlignmentProperty =
            PerspexProperty.Register<Control, HorizontalAlignment>("HorizontalAlignment");

        public static readonly PerspexProperty<Thickness> MarginProperty =
            PerspexProperty.Register<Control, Thickness>("Margin");

        public static readonly PerspexProperty<VerticalAlignment> VerticalAlignmentProperty =
            PerspexProperty.Register<Control, VerticalAlignment>("VerticalAlignment");

        public static readonly RoutedEvent<MouseEventArgs> MouseLeftButtonDownEvent =
            RoutedEvent.Register<Control, MouseEventArgs>("MouseLeftButtonDown", RoutingStrategy.Bubble);

        public static readonly RoutedEvent<MouseEventArgs> MouseLeftButtonUpEvent =
            RoutedEvent.Register<Control, MouseEventArgs>("MouseLeftButtonUp", RoutingStrategy.Bubble);

        private Classes classes;

        private string id;

        private Styles styles;

        public Control()
        {
            this.classes = new Classes();
            this.classes.BeforeChanged.Subscribe(x => this.BeginDeferStyleChanges());
            this.classes.AfterChanged.Subscribe(x => this.EndDeferStyleChanges());

            this.GetObservable(IsMouseOverProperty).Subscribe(x =>
            {
                if (x)
                {
                    this.Classes.Add(":mouseover");
                }
                else
                {
                    this.Classes.Remove(":mouseover");
                }
            });

            // Hacky hack hack!
            this.GetObservable(BackgroundProperty).Skip(1).Subscribe(_ => this.InvalidateMeasure());
        }

        public event EventHandler<MouseEventArgs> MouseLeftButtonDown
        {
            add
            {
                Contract.Requires<NullReferenceException>(value != null);
                this.AddHandler(MouseLeftButtonDownEvent, value); 
            }

            remove 
            {
                Contract.Requires<NullReferenceException>(value != null);
                this.RemoveHandler(MouseLeftButtonDownEvent, value); 
            }
        }

        public event EventHandler<MouseEventArgs> MouseLeftButtonUp
        {
            add 
            {
                Contract.Requires<NullReferenceException>(value != null);
                this.AddHandler(MouseLeftButtonUpEvent, value); 
            }
            remove 
            {
                Contract.Requires<NullReferenceException>(value != null);
                this.RemoveHandler(MouseLeftButtonUpEvent, value); 
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
                    this.BeginDeferStyleChanges();
                    this.classes.Clear();
                    this.classes.Add(value);
                    this.EndDeferStyleChanges();
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

        public bool IsMouseOver
        {
            get { return this.GetValue(IsMouseOverProperty); }
            set { this.SetValue(IsMouseOverProperty, value); }
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

        public ILayoutRoot GetLayoutRoot()
        {
            Control c = this;

            while (c != null && !(c is ILayoutRoot))
            {
                c = c.Parent;
            }

            return (ILayoutRoot)c;
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
