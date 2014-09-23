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
            PerspexProperty.Register<Control, Brush>("Background", inherits: true);

        public static readonly PerspexProperty<Brush> BorderBrushProperty =
            PerspexProperty.Register<Control, Brush>("BorderBrush");

        public static readonly PerspexProperty<double> BorderThicknessProperty =
            PerspexProperty.Register<Control, double>("BorderThickness");

        public static readonly PerspexProperty<Brush> ForegroundProperty =
            PerspexProperty.Register<Control, Brush>("Foreground", new SolidColorBrush(0xff000000), true);

        public static readonly PerspexProperty<Control> ParentProperty =
            PerspexProperty.Register<Control, Control>("Parent");

        public static readonly PerspexProperty<ITemplatedControl> TemplatedParentProperty =
            PerspexProperty.Register<Control, ITemplatedControl>("TemplatedParent", inherits: true);

        private Classes classes;

        private DataTemplates dataTemplates;

        private string id;

        private Styles styles;

        static Control()
        {
            AffectsMeasure(IsVisibleProperty);
        }

        public Control()
        {
            this.classes = new Classes();
            this.AddPseudoClass(IsPointerOverProperty, ":pointerover");
            this.AddPseudoClass(IsFocusedProperty, ":focus");
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
            get { return this.GetValue(TemplatedParentProperty); }
            internal set { this.SetValue(TemplatedParentProperty, value); }
        }

        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            IStyler styler = Locator.Current.GetService<IStyler>();
            styler.ApplyStyles(this);
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

        protected virtual DataTemplate FindDataTemplate(object content)
        {
            // TODO: This needs to traverse the logical tree, not the visual.
            foreach (var i in this.GetSelfAndVisualAncestors().OfType<Control>())
            {
                foreach (DataTemplate dt in i.DataTemplates.Reverse())
                {
                    if (dt.Match(content))
                    {
                        return dt;
                    }
                }
            }

            IGlobalDataTemplates global = Locator.Current.GetService<IGlobalDataTemplates>();

            if (global != null)
            {
                foreach (DataTemplate dt in global.DataTemplates.Reverse())
                {
                    if (dt.Match(content))
                    {
                        return dt;
                    }
                }
            }

            return null;
        }

        protected Control ApplyDataTemplate(object content)
        {
            DataTemplate result = this.FindDataTemplate(content);

            if (result != null)
            {
                return result.Build(content);
            }
            else if (content is Control)
            {
                return (Control)content;
            }
            else
            {
                return DataTemplate.Default.Build(content);
            }
        }
    }
}
