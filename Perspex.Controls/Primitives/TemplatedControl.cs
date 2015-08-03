// -----------------------------------------------------------------------
// <copyright file="TemplatedControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;
    using System.Linq;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Templates;
    using Perspex.Media;
    using Perspex.Styling;
    using Perspex.VisualTree;
    using Serilog;
    using Serilog.Core.Enrichers;

    /// <summary>
    /// A lookless control whose visual appearance is defined by its <see cref="Template"/>.
    /// </summary>
    public class TemplatedControl : Control, ITemplatedControl
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly PerspexProperty<Brush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="BorderBrushProperty"/> property.
        /// </summary>
        public static readonly PerspexProperty<Brush> BorderBrushProperty =
            Border.BorderBrushProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="BorderThicknessProperty"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> BorderThicknessProperty =
            Border.BorderThicknessProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="FontFamilyProperty"/> property.
        /// </summary>
        public static readonly PerspexProperty<string> FontFamilyProperty =
            TextBlock.FontFamilyProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="FontSizeProperty"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> FontSizeProperty =
            TextBlock.FontSizeProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="FontStyleProperty"/> property.
        /// </summary>
        public static readonly PerspexProperty<FontStyle> FontStyleProperty =
            TextBlock.FontStyleProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="ForegroundProperty"/> property.
        /// </summary>
        public static readonly PerspexProperty<Brush> ForegroundProperty =
            TextBlock.ForegroundProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="PaddingProperty"/> property.
        /// </summary>
        public static readonly PerspexProperty<Thickness> PaddingProperty =
            Decorator.PaddingProperty.AddOwner<TemplatedControl>();

        /// <summary>
        /// Defines the <see cref="TemplateProperty"/> property.
        /// </summary>
        public static readonly PerspexProperty<ControlTemplate> TemplateProperty =
            PerspexProperty.Register<TemplatedControl, ControlTemplate>("Template");

        private bool templateApplied;

        private ILogger templateLog;

        /// <summary>
        /// Initializes static members of the <see cref="TemplatedControl"/> class.
        /// </summary>
        static TemplatedControl()
        {
            TemplateProperty.Changed.Subscribe(e =>
            {
                var templatedControl = (TemplatedControl)e.Sender;
                templatedControl.templateApplied = false;
                templatedControl.InvalidateMeasure();
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatedControl"/> class.
        /// </summary>
        public TemplatedControl()
        {
            this.templateLog = Log.ForContext(new[]
            {
                new PropertyEnricher("Area", "Template"),
                new PropertyEnricher("SourceContext", this.GetType()),
                new PropertyEnricher("Id", this.GetHashCode()),
            });
        }

        /// <summary>
        /// Gets or sets the brush used to draw the control's background.
        /// </summary>
        public Brush Background
        {
            get { return this.GetValue(BackgroundProperty); }
            set { this.SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the brush used to draw the control's border.
        /// </summary>
        public Brush BorderBrush
        {
            get { return this.GetValue(BorderBrushProperty); }
            set { this.SetValue(BorderBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the thickness of the control's border.
        /// </summary>
        public double BorderThickness
        {
            get { return this.GetValue(BorderThicknessProperty); }
            set { this.SetValue(BorderThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font family used to draw the control's text.
        /// </summary>
        public string FontFamily
        {
            get { return this.GetValue(FontFamilyProperty); }
            set { this.SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// Gets or sets the size of the control's text in points.
        /// </summary>
        public double FontSize
        {
            get { return this.GetValue(FontSizeProperty); }
            set { this.SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font style used to draw the control's text.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return this.GetValue(FontStyleProperty); }
            set { this.SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the brush used to draw the control's text and other foreground elements.
        /// </summary>
        public Brush Foreground
        {
            get { return this.GetValue(ForegroundProperty); }
            set { this.SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the padding placed between the border of the control and its content.
        /// </summary>
        public Thickness Padding
        {
            get { return this.GetValue(PaddingProperty); }
            set { this.SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the template that defines the control's appearance.
        /// </summary>
        public ControlTemplate Template
        {
            get { return this.GetValue(TemplateProperty); }
            set { this.SetValue(TemplateProperty, value); }
        }

        /// <inheritdoc/>
        public sealed override void ApplyTemplate()
        {
            if (!this.templateApplied)
            {
                this.ClearVisualChildren();

                if (this.Template != null)
                {
                    this.templateLog.Verbose("Creating control template");

                    var child = this.Template.Build(this);

                    // We need to call this twice - once before the controls are added to the
                    // visual tree so that the logical tree can be set up before styling is
                    // applied.
                    this.SetTemplatedParentAndApplyChildTemplates(child);

                    this.AddVisualChild((Visual)child);
                    ((ISetLogicalParent)child).SetParent(this);

                    // And again after the controls are added to the visual tree, and have their
                    // styling and thus Template property set.
                    this.SetTemplatedParentAndApplyChildTemplates(child);

                    this.OnTemplateApplied();
                }

                this.templateApplied = true;
            }
        }

        /// <summary>
        /// Called when the control's template is applied.
        /// </summary>
        protected virtual void OnTemplateApplied()
        {
        }

        /// <summary>
        /// Sets the TemplatedParent property for a control created from the control template and
        /// applies the templates of nested templated controls.
        /// </summary>
        /// <param name="control">The control.</param>
        private void SetTemplatedParentAndApplyChildTemplates(IControl control)
        {
            if (control.TemplatedParent == null)
            {
                control.SetValue(TemplatedParentProperty, this);
            }

            control.ApplyTemplate();

            if (!(control is IPresenter && control.TemplatedParent == this))
            {
                foreach (IControl child in control.GetVisualChildren())
                {
                    this.SetTemplatedParentAndApplyChildTemplates(child);
                }
            }
        }
    }
}
