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
    using Perspex.Input;
    using Perspex.Media;
    using Perspex.Rendering;
    using Perspex.Styling;
    using Splat;

    public class Control : InputElement, IStyleable, IStyleHost
    {
        public static readonly PerspexProperty<Brush> BackgroundProperty =
            PerspexProperty.Register<Control, Brush>("Background");

        public static readonly PerspexProperty<Brush> BorderBrushProperty =
            PerspexProperty.Register<Control, Brush>("BorderBrush");

        public static readonly PerspexProperty<double> BorderThicknessProperty =
            PerspexProperty.Register<Control, double>("BorderThickness");

        public static readonly PerspexProperty<Brush> ForegroundProperty =
            PerspexProperty.Register<Control, Brush>("Foreground", new SolidColorBrush(0xff000000), true);

        public static readonly PerspexProperty<ITemplatedControl> TemplatedParentProperty =
            PerspexProperty.Register<Control, ITemplatedControl>("TemplatedParent", inherits: true);

        private Classes classes = new Classes();

        private DataTemplates dataTemplates;

        private string id;

        private Styles styles;

        static Control()
        {
            Control.AffectsMeasure(Control.IsVisibleProperty);
            PseudoClass(Control.IsPointerOverProperty, ":pointerover");
            PseudoClass(Control.IsFocusedProperty, ":focus");
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

        public ITemplatedControl TemplatedParent
        {
            get { return this.GetValue(TemplatedParentProperty); }
            internal set { this.SetValue(TemplatedParentProperty, value); }
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
            IStyler styler = Locator.Current.GetService<IStyler>();
            styler.ApplyStyles(this);
        }
    }
}
