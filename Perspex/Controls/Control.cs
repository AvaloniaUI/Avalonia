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
    using System.Diagnostics.Contracts;
    using System.Reactive.Linq;
    using Perspex.Input;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.Styling;

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

    public abstract class Control : Interactive, ILayoutable, IStyleable
    {
        public static readonly ReadOnlyPerspexProperty<Control> ParentProperty =
            new ReadOnlyPerspexProperty<Control>(ParentPropertyRW);

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

        internal static readonly PerspexProperty<Control> ParentPropertyRW =
            PerspexProperty.Register<Control, Control>("Parent");

        private Classes classes;

        private Styles styles;

        public Control()
        {
            this.classes = new Classes();
            this.classes.BeforeChanged.Subscribe(x => this.BeginDeferStyleChanges());
            this.classes.AfterChanged.Subscribe(x => this.EndDeferStyleChanges());

            this.GetObservableWithHistory(ParentPropertyRW).Subscribe(this.ParentChanged);

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

        public Brush Foreground
        {
            get { return this.GetValue(ForegroundProperty); }
            set { this.SetValue(ForegroundProperty, value); }
        }

        public Size? DesiredSize
        {
            get;
            set;
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

        public Control Parent
        {
            get { return this.GetValue(ParentPropertyRW); }
            internal set { this.SetValue(ParentPropertyRW, value); }
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

        protected abstract Size MeasureContent(Size availableSize);

        private void AttachStyles(Control control)
        {
            if (control != null)
            {
                Control parent = control.Parent;
                this.AttachStyles(parent);
                control.Styles.Attach(this);
            }
            else
            {
                Application.Current.Styles.Attach(this);
            }
        }

        private void ParentChanged(Tuple<Control, Control> values)
        {
            Contract.Requires<ArgumentNullException>(values != null);

            if (values.Item1 != null)
            {
                ////this.DetatchStyles(values.Item1);
            }

            if (values.Item2 != null)
            {
                this.AttachStyles(this);
            }
        }
    }
}
