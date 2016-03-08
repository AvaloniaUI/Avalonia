// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Layout;
using Perspex.LogicalTree;
using Perspex.Media;

namespace Perspex.Controls.Presenters
{
    /// <summary>
    /// Presents a single item of data inside a <see cref="TemplatedControl"/> template.
    /// </summary>
    public class ContentPresenter : Control, IContentPresenter
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<Brush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="BorderBrush"/> property.
        /// </summary>
        public static readonly PerspexProperty<Brush> BorderBrushProperty =
            Border.BorderBrushProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="BorderThickness"/> property.
        /// </summary>
        public static readonly StyledProperty<double> BorderThicknessProperty =
            Border.BorderThicknessProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="Child"/> property.
        /// </summary>
        public static readonly DirectProperty<ContentPresenter, IControl> ChildProperty =
            PerspexProperty.RegisterDirect<ContentPresenter, IControl>(
                nameof(Child),
                o => o.Child);

        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly StyledProperty<object> ContentProperty =
            ContentControl.ContentProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="CornerRadius"/> property.
        /// </summary>
        public static readonly StyledProperty<float> CornerRadiusProperty =
            Border.CornerRadiusProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="Padding"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> PaddingProperty =
            Border.PaddingProperty.AddOwner<ContentPresenter>();

        private IControl _child;
        private bool _createdChild;

        /// <summary>
        /// Initializes static members of the <see cref="ContentPresenter"/> class.
        /// </summary>
        static ContentPresenter()
        {
            ContentProperty.Changed.AddClassHandler<ContentPresenter>(x => x.ContentChanged);
            TemplatedParentProperty.Changed.AddClassHandler<ContentPresenter>(x => x.TemplatedParentChanged);
        }

        /// <summary>
        /// Gets or sets a brush with which to paint the background.
        /// </summary>
        public Brush Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush with which to paint the border.
        /// </summary>
        public Brush BorderBrush
        {
            get { return GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the thickness of the border.
        /// </summary>
        public double BorderThickness
        {
            get { return GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        /// <summary>
        /// Gets the control displayed by the presenter.
        /// </summary>
        public IControl Child
        {
            get { return _child; }
            private set { SetAndRaise(ChildProperty, ref _child, value); }
        }

        /// <summary>
        /// Gets or sets the content to be displayed by the presenter.
        /// </summary>
        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the radius of the border rounded corners.
        /// </summary>
        public float CornerRadius
        {
            get { return GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the control.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the control.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get { return GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the padding to place around the <see cref="Child"/> control.
        /// </summary>
        public Thickness Padding
        {
            get { return GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <inheritdoc/>
        public override sealed void ApplyTemplate()
        {
            if (!_createdChild && ((ILogical)this).IsAttachedToLogicalTree)
            {
                UpdateChild();
            }
        }

        /// <summary>
        /// Updates the <see cref="Child"/> control based on the control's <see cref="Content"/>.
        /// </summary>
        /// <remarks>
        /// Usually the <see cref="Child"/> control is created automatically when 
        /// <see cref="ApplyTemplate"/> is called; however for this to happen, the control needs to
        /// be attached to a logical tree (if the control is not attached to the logical tree, it
        /// is reasonable to expect that the DataTemplates needed for the child are not yet 
        /// available). This method forces the <see cref="Child"/> control's creation at any point, 
        /// and is particularly useful in unit tests.
        /// </remarks>
        public void UpdateChild()
        {
            var old = Child;
            var content = Content;
            var result = this.MaterializeDataTemplate(content);

            if (old != null)
            {
                VisualChildren.Remove(old);
            }

            if (result != null)
            {
                if (!(content is IControl))
                {
                    result.DataContext = content;
                }

                Child = result;

                if (result.Parent == null)
                {
                    ((ISetLogicalParent)result).SetParent((ILogical)this.TemplatedParent ?? this);
                }

                VisualChildren.Add(result);
            }
            else
            {
                Child = null;
            }

            _createdChild = true;
        }

        /// <inheritdoc/>
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            _createdChild = false;
            InvalidateMeasure();
        }

        /// <inheritdoc/>
        public override void Render(DrawingContext context)
        {
            var background = Background;
            var borderBrush = BorderBrush;
            var borderThickness = BorderThickness;
            var cornerRadius = CornerRadius;
            var rect = new Rect(Bounds.Size).Deflate(BorderThickness);

            if (background != null)
            {
                context.FillRectangle(background, rect, cornerRadius);
            }

            if (borderBrush != null && borderThickness > 0)
            {
                context.DrawRectangle(new Pen(borderBrush, borderThickness), rect, cornerRadius);
            }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var child = Child;
            var padding = Padding + new Thickness(BorderThickness);

            if (child != null)
            {
                child.Measure(availableSize.Deflate(padding));
                return child.DesiredSize.Inflate(padding);
            }
            else
            {
                return new Size(padding.Left + padding.Right, padding.Bottom + padding.Top);
            }
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var child = Child;

            if (child != null)
            {
                var padding = Padding + new Thickness(BorderThickness);
                var sizeMinusPadding = finalSize.Deflate(padding);
                var size = sizeMinusPadding;
                var horizontalAlignment = HorizontalContentAlignment;
                var verticalAlignment = VerticalContentAlignment;
                var originX = padding.Left;
                var originY = padding.Top;

                if (horizontalAlignment != HorizontalAlignment.Stretch)
                {
                    size = size.WithWidth(child.DesiredSize.Width);
                }

                if (verticalAlignment != VerticalAlignment.Stretch)
                {
                    size = size.WithHeight(child.DesiredSize.Height);
                }

                switch (horizontalAlignment)
                {
                    case HorizontalAlignment.Stretch:
                    case HorizontalAlignment.Center:
                        originX += (sizeMinusPadding.Width - size.Width) / 2;
                        break;
                    case HorizontalAlignment.Right:
                        originX = size.Width - child.DesiredSize.Width;
                        break;
                }

                switch (verticalAlignment)
                {
                    case VerticalAlignment.Stretch:
                    case VerticalAlignment.Center:
                        originY += (sizeMinusPadding.Height - size.Height) / 2;
                        break;
                    case VerticalAlignment.Bottom:
                        originY = size.Height - child.DesiredSize.Height;
                        break;
                }

                child.Arrange(new Rect(originX, originY, size.Width, size.Height));
            }

            return finalSize;
        }

        /// <summary>
        /// Called when the <see cref="Content"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ContentChanged(PerspexPropertyChangedEventArgs e)
        {
            _createdChild = false;
            InvalidateMeasure();
        }

        private void TemplatedParentChanged(PerspexPropertyChangedEventArgs e)
        {
            (e.NewValue as IContentPresenterHost)?.RegisterContentPresenter(this);
        }
    }
}
