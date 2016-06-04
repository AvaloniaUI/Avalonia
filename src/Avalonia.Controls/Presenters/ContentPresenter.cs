// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;

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
            TemplatedParentProperty.Changed.AddClassHandler<ContentPresenter>(x => x.TemplatedParentChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentPresenter"/> class.
        /// </summary>
        public ContentPresenter()
        {
            var dataContext = this.GetObservable(ContentProperty)
                .Select(x => x is IControl ? AvaloniaProperty.UnsetValue : x);

            Bind(Control.DataContextProperty, dataContext);
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
            var old = Child;
            var content = Content;
            var result = content as IControl;

            if (result == null)
            {
                DataContext = content;

                if (content != null)
                {
                    if (old != null && 
                        _dataTemplate != null &&
                        _dataTemplate.SupportsRecycling && 
                        _dataTemplate.Match(content))
                    {
                        result = old;
                    }
                    else
                    {
                        _dataTemplate = this.FindDataTemplate(content, ContentTemplate) ?? FuncDataTemplate.Default;
                        result = _dataTemplate.Build(content);

                        var controlResult = result as Control;

                        if (controlResult != null)
                        {
                            NameScope.SetNameScope(controlResult, new NameScope());
                        }
                    }
                }
                else
                {
                    _dataTemplate = null;
                }
            }
            else
            {
                _dataTemplate = null;
            }

            if (result != old)
            {
                if (old != null)
                {
                    VisualChildren.Remove(old);
                }

                if (result != null)
                {
                    ((ISetInheritanceParent)result).SetParent(this);

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
        private void ContentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            _createdChild = false;
            InvalidateMeasure();
        }

        private void TemplatedParentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            (e.NewValue as IContentPresenterHost)?.RegisterContentPresenter(this);
        }
    }
}
