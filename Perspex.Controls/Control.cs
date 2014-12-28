// -----------------------------------------------------------------------
// <copyright file="Control.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Collections;
    using Perspex.Input;
    using Perspex.Interactivity;
    using Perspex.Media;
    using Perspex.Rendering;
    using Perspex.Styling;
    using Splat;

    public class Control : InputElement, ILogical, IStyleable, IStyleHost
    {
        public static readonly PerspexProperty<Brush> BorderBrushProperty =
            PerspexProperty.Register<Control, Brush>("BorderBrush");

        public static readonly PerspexProperty<double> BorderThicknessProperty =
            PerspexProperty.Register<Control, double>("BorderThickness");

        public static readonly PerspexProperty<Brush> ForegroundProperty =
            PerspexProperty.Register<Control, Brush>("Foreground", new SolidColorBrush(0xff000000), inherits: true);

        public static readonly PerspexProperty<Control> ParentProperty =
            PerspexProperty.Register<Control, Control>("Parent");

        public static readonly PerspexProperty<object> TagProperty =
            PerspexProperty.Register<Control, object>("Tag");

        public static readonly PerspexProperty<ITemplatedControl> TemplatedParentProperty =
            PerspexProperty.Register<Control, ITemplatedControl>("TemplatedParent", inherits: true);

        public static readonly RoutedEvent<RequestBringIntoViewEventArgs> RequestBringIntoViewEvent =
            RoutedEvent.Register<Control, RequestBringIntoViewEventArgs>("RequestBringIntoView", RoutingStrategy.Bubble);

        private static readonly IReadOnlyPerspexList<ILogical> EmptyChildren = new SingleItemPerspexList<ILogical>();

        private Classes classes = new Classes();

        private DataTemplates dataTemplates;

        private string id;

        private Styles styles;

        static Control()
        {
            Control.AffectsMeasure(Control.IsVisibleProperty);
            PseudoClass(InputElement.IsEnabledCoreProperty, x => !x, ":disabled");
            PseudoClass(InputElement.IsFocusedProperty, ":focus");
            PseudoClass(InputElement.IsPointerOverProperty, ":pointerover");
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

        public DataTemplates DataTemplates
        {
            get
            {
                if (this.dataTemplates == null)
                {
                    this.dataTemplates = new DataTemplates();
                }

                return this.dataTemplates;
            }

            set
            {
                this.dataTemplates = value;
            }
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

        public Control Parent
        {
            get { return this.GetValue(ParentProperty); }
            internal set { this.SetValue(ParentProperty, value); }
        }

        public object Tag
        {
            get { return this.GetValue(TagProperty); }
            set { this.SetValue(TagProperty, value); }
        }

        public ITemplatedControl TemplatedParent
        {
            get { return this.GetValue(TemplatedParentProperty); }
            internal set { this.SetValue(TemplatedParentProperty, value); }
        }

        ILogical ILogical.LogicalParent
        {
            get { return this.Parent; }
        }

        IReadOnlyPerspexList<ILogical> ILogical.LogicalChildren
        {
            get { return EmptyChildren; }
        }

        public void BringIntoView()
        {
            this.BringIntoView(new Rect(this.ActualSize));
        }

        public void BringIntoView(Rect rect)
        {
            var ev = new RequestBringIntoViewEventArgs
            {
                RoutedEvent = RequestBringIntoViewEvent,
                TargetObject = this,
                TargetRect = rect,
            };

            this.RaiseEvent(ev);
        }

        protected static void PseudoClass(PerspexProperty<bool> property, string className)
        {
            PseudoClass(property, x => x, className);
        }

        protected static void PseudoClass<T>(
            PerspexProperty<T> property, 
            Func<T, bool> selector, 
            string className)
        {
            Contract.Requires<ArgumentNullException>(property != null);
            Contract.Requires<ArgumentNullException>(selector != null);
            Contract.Requires<ArgumentNullException>(className != null);
            Contract.Requires<ArgumentNullException>(property != null);

            if (string.IsNullOrWhiteSpace(className))
            {
                throw new ArgumentException("Cannot supply an empty className.");
            }

            Observable.Merge(property.Changed, property.Initialized)
                .Subscribe(e =>
                {
                    if (selector((T)e.NewValue))
                    {
                        ((Control)e.Sender).Classes.Add(className);
                    }
                    else
                    {
                        ((Control)e.Sender).Classes.Remove(className);
                    }
                });
        }

        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            base.OnAttachedToVisualTree(root);

            IStyler styler = Locator.Current.GetService<IStyler>();
            styler.ApplyStyles(this);
        }
    }
}
