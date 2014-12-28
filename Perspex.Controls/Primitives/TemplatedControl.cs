// -----------------------------------------------------------------------
// <copyright file="TemplatedControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;
    using System.Linq;
    using Perspex.Media;
    using Perspex.Styling;
    using Perspex.VisualTree;
    using Splat;

    public class TemplatedControl : Control, ITemplatedControl
    {
        public static readonly PerspexProperty<Brush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<TemplatedControl>();

        public static readonly PerspexProperty<Brush> BorderBrushProperty =
            Border.BorderBrushProperty.AddOwner<TemplatedControl>();

        public static readonly PerspexProperty<double> BorderThicknessProperty =
            Border.BorderThicknessProperty.AddOwner<TemplatedControl>();

        public static readonly PerspexProperty<string> FontFamilyProperty =
            TextBlock.FontFamilyProperty.AddOwner<TemplatedControl>();

        public static readonly PerspexProperty<double> FontSizeProperty =
            TextBlock.FontSizeProperty.AddOwner<TemplatedControl>();

        public static readonly PerspexProperty<FontStyle> FontStyleProperty =
            TextBlock.FontStyleProperty.AddOwner<TemplatedControl>();

        public static readonly PerspexProperty<Brush> ForegroundProperty =
            TextBlock.ForegroundProperty.AddOwner<TemplatedControl>();

        public static readonly PerspexProperty<ControlTemplate> TemplateProperty =
            PerspexProperty.Register<TemplatedControl, ControlTemplate>("Template");

        private bool templateApplied;

        static TemplatedControl()
        {
            TemplateProperty.Changed.Subscribe(e =>
            {
                var templatedControl = (TemplatedControl)e.Sender;
                templatedControl.templateApplied = false;
                templatedControl.InvalidateMeasure();
            });
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

        public string FontFamily
        {
            get { return this.GetValue(FontFamilyProperty); }
            set { this.SetValue(FontFamilyProperty, value); }
        }

        public double FontSize
        {
            get { return this.GetValue(FontSizeProperty); }
            set { this.SetValue(FontSizeProperty, value); }
        }

        public FontStyle FontStyle
        {
            get { return this.GetValue(FontStyleProperty); }
            set { this.SetValue(FontStyleProperty, value); }
        }

        public Brush Foreground
        {
            get { return this.GetValue(ForegroundProperty); }
            set { this.SetValue(ForegroundProperty, value); }
        }

        public ControlTemplate Template
        {
            get { return this.GetValue(TemplateProperty); }
            set { this.SetValue(TemplateProperty, value); }
        }

        public override void Render(IDrawingContext context)
        {
        }

        public sealed override void ApplyTemplate()
        {
            if (!this.templateApplied)
            {
                this.ClearVisualChildren();

                if (this.Template != null)
                {
                    this.Log().Debug(
                        "Creating template for {0} (#{1:x8})",
                        this.GetType().Name,
                        this.GetHashCode());

                    var child = this.Template.Build(this);
                    this.AddVisualChild(child);

                    var templateChildren = this.GetVisualDescendents()
                        .OfType<Control>()
                        .Where(x => x.TemplatedParent != null);

                    foreach (var i in templateChildren)
                    {
                        i.ApplyTemplate();
                    }

                    this.OnTemplateApplied();
                }

                this.templateApplied = true;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Control child = ((IVisual)this).VisualChildren.SingleOrDefault() as Control;

            if (child != null)
            {
                child.Arrange(new Rect(finalSize));
                return child.ActualSize;
            }
            else
            {
                return new Size();
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Control child = ((IVisual)this).VisualChildren.SingleOrDefault() as Control;

            if (child != null)
            {
                child.Measure(availableSize);
                return child.DesiredSize.Value;
            }

            return new Size();
        }

        protected T FindTemplateChild<T>(string id) where T : Control
        {
            return (T)this.GetTemplateControls().SingleOrDefault(x => x.Id == id);
        }

        protected T GetTemplateChild<T>(string id) where T : Control
        {
            var result = this.FindTemplateChild<T>(id);

            if (result == null)
            {
                throw new InvalidOperationException(string.Format(
                    "Could not find template child '{0}' in template for '{1}'.",
                    id,
                    this.GetType().FullName));
            }

            return result;
        }

        protected virtual void OnTemplateApplied()
        {
        }
    }
}
