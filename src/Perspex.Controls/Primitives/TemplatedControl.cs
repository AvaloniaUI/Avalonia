// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Perspex.Controls.Presenters;
using Perspex.Controls.Templates;
using Perspex.Media;
using Perspex.Styling;
using Perspex.VisualTree;
using Serilog;
using Serilog.Core.Enrichers;

namespace Perspex.Controls.Primitives
{
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

        private bool _templateApplied;

        private readonly ILogger _templateLog;

        /// <summary>
        /// Initializes static members of the <see cref="TemplatedControl"/> class.
        /// </summary>
        static TemplatedControl()
        {
            TemplateProperty.Changed.Subscribe(e =>
            {
                var templatedControl = (TemplatedControl)e.Sender;
                templatedControl._templateApplied = false;
                templatedControl.InvalidateMeasure();
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatedControl"/> class.
        /// </summary>
        public TemplatedControl()
        {
            _templateLog = Log.ForContext(new[]
            {
                new PropertyEnricher("Area", "Template"),
                new PropertyEnricher("SourceContext", GetType()),
                new PropertyEnricher("Id", GetHashCode()),
            });
        }

        /// <summary>
        /// Gets or sets the brush used to draw the control's background.
        /// </summary>
        public Brush Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the brush used to draw the control's border.
        /// </summary>
        public Brush BorderBrush
        {
            get { return GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the thickness of the control's border.
        /// </summary>
        public double BorderThickness
        {
            get { return GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font family used to draw the control's text.
        /// </summary>
        public string FontFamily
        {
            get { return GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// Gets or sets the size of the control's text in points.
        /// </summary>
        public double FontSize
        {
            get { return GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font style used to draw the control's text.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the brush used to draw the control's text and other foreground elements.
        /// </summary>
        public Brush Foreground
        {
            get { return GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the padding placed between the border of the control and its content.
        /// </summary>
        public Thickness Padding
        {
            get { return GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the template that defines the control's appearance.
        /// </summary>
        public ControlTemplate Template
        {
            get { return GetValue(TemplateProperty); }
            set { SetValue(TemplateProperty, value); }
        }

        /// <inheritdoc/>
        public sealed override void ApplyTemplate()
        {
            if (!_templateApplied)
            {
                ClearVisualChildren();

                if (Template != null)
                {
                    _templateLog.Verbose("Creating control template");

                    var child = Template.Build(this);

                    // We need to call SetTemplatedParentAndApplyChildTemplates twice - once
                    // before the controls are added to the visual tree so that the logical
                    // tree can be set up before styling is applied.
                    ((ISetLogicalParent)child).SetParent(this);
                    SetTemplatedParentAndApplyChildTemplates(child);

                    // And again after the controls are added to the visual tree, and have their
                    // styling and thus Template property set.
                    AddVisualChild((Visual)child);
                    SetTemplatedParentAndApplyChildTemplates(child);

                    OnTemplateApplied();
                }

                _templateApplied = true;
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
                    SetTemplatedParentAndApplyChildTemplates(child);
                }
            }
        }
    }
}
