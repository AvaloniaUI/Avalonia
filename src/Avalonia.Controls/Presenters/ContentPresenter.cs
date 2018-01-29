// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Presents a single item of data inside a <see cref="TemplatedControl"/> template.
    /// </summary>
    public class ContentPresenter : Control, IContentPresenter
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="BorderBrush"/> property.
        /// </summary>
        public static readonly AvaloniaProperty<IBrush> BorderBrushProperty =
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
            AvaloniaProperty.RegisterDirect<ContentPresenter, IControl>(
                nameof(Child),
                o => o.Child);

        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly StyledProperty<object> ContentProperty =
            ContentControl.ContentProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="ContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> ContentTemplateProperty =
            ContentControl.ContentTemplateProperty.AddOwner<ContentPresenter>();

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
        private IDataTemplate _dataTemplate;

        /// <summary>
        /// Initializes static members of the <see cref="ContentPresenter"/> class.
        /// </summary>
        static ContentPresenter()
        {
            ContentProperty.Changed.AddClassHandler<ContentPresenter>(x => x.ContentChanged);
            ContentTemplateProperty.Changed.AddClassHandler<ContentPresenter>(x => x.ContentChanged);
            TemplatedParentProperty.Changed.AddClassHandler<ContentPresenter>(x => x.TemplatedParentChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentPresenter"/> class.
        /// </summary>
        public ContentPresenter()
        {
        }

        /// <summary>
        /// Gets or sets a brush with which to paint the background.
        /// </summary>
        public IBrush Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush with which to paint the border.
        /// </summary>
        public IBrush BorderBrush
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
        [DependsOn(nameof(ContentTemplate))]
        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the data template used to display the content of the control.
        /// </summary>
        public IDataTemplate ContentTemplate
        {
            get { return GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
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

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _dataTemplate = null;
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
            var content = Content;
            var oldChild = Child;
            var newChild = CreateChild();            

            // Remove the old child if we're not recycling it.
            if (oldChild != null && newChild != oldChild)
            {
                VisualChildren.Remove(oldChild);
            }

            // Set the DataContext if the data isn't a control.
            if (!(content is IControl))
            {
                DataContext = content;
            }
            else
            {
                ClearValue(DataContextProperty);
            }

            // Update the Child.
            if (newChild == null)
            {
                Child = null;
            }
            else if (newChild != oldChild)
            {
                ((ISetInheritanceParent)newChild).SetParent(this);

                Child = newChild;

                if (oldChild?.Parent == this)
                {
                    LogicalChildren.Remove(oldChild);
                }

                if (newChild.Parent == null && TemplatedParent == null)
                {
                    LogicalChildren.Add(newChild);
                }

                VisualChildren.Add(newChild);
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

        /// <summary>
        /// Creates the child control.
        /// </summary>
        /// <returns>The child control or null.</returns>
        protected virtual IControl CreateChild()
        {
            var content = Content;
            var oldChild = Child;
            var newChild = content as IControl;

            if (content != null && newChild == null)
            {
                var dataTemplate = this.FindDataTemplate(content, ContentTemplate) ?? FuncDataTemplate.Default;

                // We have content and it isn't a control, so if the new data template is the same
                // as the old data template, try to recycle the existing child control to display
                // the new data.
                if (dataTemplate == _dataTemplate && dataTemplate.SupportsRecycling)
                {
                    newChild = oldChild;
                }
                else
                {
                    _dataTemplate = dataTemplate;
                    newChild = _dataTemplate.Build(content);

                    // Give the new control its own name scope.
                    if (newChild is Control controlResult)
                    {
                        NameScope.SetNameScope(controlResult, new NameScope());
                    }
                }
            }
            else
            {
                _dataTemplate = null;
            }

            return newChild;
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            return Border.MeasureOverrideImpl(availableSize, Child, Padding, BorderThickness);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            return ArrangeOverrideImpl(finalSize, new Vector());
        }

        /// <summary>
        /// Called when the <see cref="Content"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ContentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            _createdChild = false;

            if (((ILogical)this).IsAttachedToLogicalTree)
            {
                UpdateChild();
            }
            else if (Child != null)
            {
                VisualChildren.Remove(Child);
                LogicalChildren.Remove(Child);
                Child = null;
                _dataTemplate = null;
            }

            InvalidateMeasure();
        }

        internal Size ArrangeOverrideImpl(Size finalSize, Vector offset)
        {
            if (Child != null)
            {
                var padding = Padding;
                var borderThickness = BorderThickness;
                var horizontalContentAlignment = HorizontalContentAlignment;
                var verticalContentAlignment = VerticalContentAlignment;
                var useLayoutRounding = UseLayoutRounding;
                var availableSizeMinusMargins = new Size(
                    Math.Max(0, finalSize.Width - padding.Left - padding.Right - borderThickness),
                    Math.Max(0, finalSize.Height - padding.Top - padding.Bottom - borderThickness));
                var size = availableSizeMinusMargins;
                var scale = GetLayoutScale();
                var originX = offset.X + padding.Left + borderThickness;
                var originY = offset.Y + padding.Top + borderThickness;

                if (horizontalContentAlignment != HorizontalAlignment.Stretch)
                {
                    size = size.WithWidth(Math.Min(size.Width, DesiredSize.Width - padding.Left - padding.Right));
                }

                if (verticalContentAlignment != VerticalAlignment.Stretch)
                {
                    size = size.WithHeight(Math.Min(size.Height, DesiredSize.Height - padding.Top - padding.Bottom));
                }

                size = LayoutHelper.ApplyLayoutConstraints(Child, size);

                if (useLayoutRounding)
                {
                    size = new Size(
                        Math.Ceiling(size.Width * scale) / scale,
                        Math.Ceiling(size.Height * scale) / scale);
                    availableSizeMinusMargins = new Size(
                        Math.Ceiling(availableSizeMinusMargins.Width * scale) / scale,
                        Math.Ceiling(availableSizeMinusMargins.Height * scale) / scale);
                }

                switch (horizontalContentAlignment)
                {
                    case HorizontalAlignment.Center:
                    case HorizontalAlignment.Stretch:
                        originX += (availableSizeMinusMargins.Width - size.Width) / 2;
                        break;
                    case HorizontalAlignment.Right:
                        originX += availableSizeMinusMargins.Width - size.Width;
                        break;
                }

                switch (verticalContentAlignment)
                {
                    case VerticalAlignment.Center:
                    case VerticalAlignment.Stretch:
                        originY += (availableSizeMinusMargins.Height - size.Height) / 2;
                        break;
                    case VerticalAlignment.Bottom:
                        originY += availableSizeMinusMargins.Height - size.Height;
                        break;
                }

                if (useLayoutRounding)
                {
                    originX = Math.Floor(originX * scale) / scale;
                    originY = Math.Floor(originY * scale) / scale;
                }

                Child.Arrange(new Rect(originX, originY, size.Width, size.Height));
            }

            return finalSize;
        }

        private double GetLayoutScale()
        {
            var result = (VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1.0;

            if (result == 0 || double.IsNaN(result) || double.IsInfinity(result))
            {
                throw new Exception($"Invalid LayoutScaling returned from {VisualRoot.GetType()}");
            }

            return result;
        }

        private void TemplatedParentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            (e.NewValue as IContentPresenterHost)?.RegisterContentPresenter(this);
        }
    }
}
