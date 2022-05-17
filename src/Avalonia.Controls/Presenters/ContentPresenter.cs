using System;

using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Presents a single item of data inside a <see cref="TemplatedControl"/> template.
    /// </summary>
    [PseudoClasses(":empty")]
    public class ContentPresenter : Control, IContentPresenter
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="BorderBrush"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BorderBrushProperty =
            Border.BorderBrushProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="BorderThickness"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> BorderThicknessProperty =
            Border.BorderThicknessProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="CornerRadius"/> property.
        /// </summary>
        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            Border.CornerRadiusProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="BoxShadow"/> property.
        /// </summary>
        public static readonly StyledProperty<BoxShadows> BoxShadowProperty =
            Border.BoxShadowProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="Foreground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> ForegroundProperty =
            TextElement.ForegroundProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="FontFamily"/> property.
        /// </summary>
        public static readonly StyledProperty<FontFamily> FontFamilyProperty =
            TextElement.FontFamilyProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="FontSize"/> property.
        /// </summary>
        public static readonly StyledProperty<double> FontSizeProperty =
            TextElement.FontSizeProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="FontStyle"/> property.
        /// </summary>
        public static readonly StyledProperty<FontStyle> FontStyleProperty =
            TextElement.FontStyleProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly StyledProperty<FontWeight> FontWeightProperty =
            TextElement.FontWeightProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="FontStretch"/> property.
        /// </summary>
        public static readonly StyledProperty<FontStretch> FontStretchProperty =
            TextElement.FontStretchProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="TextAlignment"/> property
        /// </summary>
        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            TextBlock.TextAlignmentProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="TextWrapping"/> property
        /// </summary>
        public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
            TextBlock.TextWrappingProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="TextTrimming"/> property
        /// </summary>
        public static readonly StyledProperty<TextTrimming> TextTrimmingProperty =
            TextBlock.TextTrimmingProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="LineHeight"/> property
        /// </summary>
        public static readonly StyledProperty<double> LineHeightProperty =
            TextBlock.LineHeightProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="MaxLines"/> property
        /// </summary>
        public static readonly StyledProperty<int> MaxLinesProperty =
            TextBlock.MaxLinesProperty.AddOwner<ContentPresenter>();
                
        /// <summary>
        /// Defines the <see cref="Child"/> property.
        /// </summary>
        public static readonly DirectProperty<ContentPresenter, IControl?> ChildProperty =
            AvaloniaProperty.RegisterDirect<ContentPresenter, IControl?>(
                nameof(Child),
                o => o.Child);

        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> ContentProperty =
            ContentControl.ContentProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="ContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
            ContentControl.ContentTemplateProperty.AddOwner<ContentPresenter>();

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
            Decorator.PaddingProperty.AddOwner<ContentPresenter>();

        /// <summary>
        /// Defines the <see cref="RecognizesAccessKey"/> property
        /// </summary>
        public static readonly DirectProperty<ContentPresenter, bool> RecognizesAccessKeyProperty =
            AvaloniaProperty.RegisterDirect<ContentPresenter, bool>(
                nameof(RecognizesAccessKey),
                cp => cp.RecognizesAccessKey, (cp, value) => cp.RecognizesAccessKey = value);

        private IControl? _child;
        private bool _createdChild;
        private IRecyclingDataTemplate? _recyclingDataTemplate;
        private readonly BorderRenderHelper _borderRenderer = new BorderRenderHelper();
        private bool _recognizesAccessKey;

        /// <summary>
        /// Initializes static members of the <see cref="ContentPresenter"/> class.
        /// </summary>
        static ContentPresenter()
        {
            AffectsRender<ContentPresenter>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, CornerRadiusProperty);
            AffectsArrange<ContentPresenter>(HorizontalContentAlignmentProperty, VerticalContentAlignmentProperty);
            AffectsMeasure<ContentPresenter>(BorderThicknessProperty, PaddingProperty);
        }

        public ContentPresenter()
        {
            UpdatePseudoClasses();
        }

        /// <summary>
        /// Gets or sets a brush with which to paint the background.
        /// </summary>
        public IBrush? Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush with which to paint the border.
        /// </summary>
        public IBrush? BorderBrush
        {
            get { return GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the thickness of the border.
        /// </summary>
        public Thickness BorderThickness
        {
            get { return GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets the radius of the border rounded corners.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// Gets or sets the box shadow effect parameters
        /// </summary>
        public BoxShadows BoxShadow
        {
            get => GetValue(BoxShadowProperty);
            set => SetValue(BoxShadowProperty, value);
        }

        /// <summary>
        /// Gets or sets a brush used to paint the text.
        /// </summary>
        public IBrush? Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        public FontFamily FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get => GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        public FontWeight FontWeight
        {
            get => GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the font stretch.
        /// </summary>
        public FontStretch FontStretch
        {
            get => GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        /// <summary>
        /// Gets or sets the text alignment
        /// </summary>
        public TextAlignment TextAlignment
        {
            get => GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the text wrapping
        /// </summary>
        public TextWrapping TextWrapping
        {
            get => GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        /// <summary>
        /// Gets or sets the text trimming
        /// </summary>
        public TextTrimming TextTrimming
        {
            get => GetValue(TextTrimmingProperty);
            set => SetValue(TextTrimmingProperty, value);
        }

        /// <summary>
        /// Gets or sets the line height
        /// </summary>
        public double LineHeight
        {
            get => GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the max lines
        /// </summary>
        public int MaxLines
        {
            get => GetValue(MaxLinesProperty);
            set => SetValue(MaxLinesProperty, value);
        }

        /// <summary>
        /// Gets the control displayed by the presenter.
        /// </summary>
        public IControl? Child
        {
            get { return _child; }
            private set { SetAndRaise(ChildProperty, ref _child, value); }
        }

        /// <summary>
        /// Gets or sets the content to be displayed by the presenter.
        /// </summary>
        [DependsOn(nameof(ContentTemplate))]
        public object? Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the data template used to display the content of the control.
        /// </summary>
        public IDataTemplate? ContentTemplate
        {
            get { return GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the border the control.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the border of the control.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get { return GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the space between the border and the <see cref="Child"/> control.
        /// </summary>
        public Thickness Padding
        {
            get { return GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// Determine if <see cref="ContentPresenter"/> should use <see cref="AccessText"/> in its style
        /// </summary>
        public bool RecognizesAccessKey
        {
            get => _recognizesAccessKey;
            set => SetAndRaise(RecognizesAccessKeyProperty, ref _recognizesAccessKey, value);
        }

        /// <summary>
        /// Gets the host content control.
        /// </summary>
        internal IContentPresenterHost? Host { get; private set; }

        /// <inheritdoc/>
        public sealed override void ApplyTemplate()
        {
            if (!_createdChild && ((ILogical)this).IsAttachedToLogicalTree)
            {
                UpdateChild();
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            switch (change.Property.Name)
            {
                case nameof(Content):
                case nameof(ContentTemplate):
                    ContentChanged(change);
                    break;
                case nameof(TemplatedParent):
                    TemplatedParentChanged(change);
                    break;
                case nameof(UseLayoutRounding):
                case nameof(BorderThickness):
                    _layoutThickness = null;
                    break;
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
            var content = Content;
            UpdateChild(content);
        }

        private void UpdateChild(object? content)
        {
            var contentTemplate = ContentTemplate;
            var oldChild = Child;
            var newChild = CreateChild(content, oldChild, contentTemplate);
            var logicalChildren = Host?.LogicalChildren ?? LogicalChildren;

            // Remove the old child if we're not recycling it.
            if (newChild != oldChild)
            {

                if (oldChild != null)
                {
                    VisualChildren.Remove(oldChild);
                    logicalChildren.Remove(oldChild);
                    ((ISetInheritanceParent)oldChild).SetParent(oldChild.Parent);
                }
            }

            // Set the DataContext if the data isn't a control.
            if (contentTemplate is { } || !(content is IControl))
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

                if (!logicalChildren.Contains(newChild))
                {
                    logicalChildren.Add(newChild);
                }

                VisualChildren.Add(newChild);
            }

            _createdChild = true;

        }

        /// <inheritdoc/>
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            _recyclingDataTemplate = null;
            _createdChild = false;
            InvalidateMeasure();
        }

        private Thickness? _layoutThickness;
        private double _scale;

        private Thickness LayoutThickness
        {
            get
            {
                VerifyScale();

                if (_layoutThickness == null)
                {
                    var borderThickness = BorderThickness;

                    if (UseLayoutRounding)
                        borderThickness = LayoutHelper.RoundLayoutThickness(borderThickness, _scale, _scale);

                    _layoutThickness = borderThickness;
                }

                return _layoutThickness.Value;
            }
        }

        private void VerifyScale()
        {
            var currentScale = LayoutHelper.GetLayoutScale(this);
            if (MathUtilities.AreClose(currentScale, _scale))
                return;

            _scale = currentScale;
            _layoutThickness = null;
        }

        /// <inheritdoc/>
        public override void Render(DrawingContext context)
        {
            _borderRenderer.Render(context, Bounds.Size, LayoutThickness, CornerRadius, Background, BorderBrush,
                BoxShadow);
        }

        /// <summary>
        /// Creates the child control.
        /// </summary>
        /// <returns>The child control or null.</returns>
        protected virtual IControl? CreateChild()
        {
            var content = Content;
            var oldChild = Child;
            return CreateChild(content, oldChild, ContentTemplate);
        }

        private IControl? CreateChild(object? content, IControl? oldChild, IDataTemplate? template)
        {            
            var newChild = content as IControl;

            // We want to allow creating Child from the Template, if Content is null.
            // But it's important to not use DataTemplates, otherwise we will break content presenters in many places,
            // otherwise it will blow up every ContentPresenter without Content set.
            if ((newChild == null 
                && (content != null || template != null)) || (newChild is { } && template is { }))
            {
                var dataTemplate = this.FindDataTemplate(content, template) ??
                    (
                        RecognizesAccessKey
                            ? FuncDataTemplate.Access
                            : FuncDataTemplate.Default
                    );

                if (dataTemplate is IRecyclingDataTemplate rdt)
                {
                    var toRecycle = rdt == _recyclingDataTemplate ? oldChild : null;
                    newChild = rdt.Build(content, toRecycle);
                    _recyclingDataTemplate = rdt;
                }
                else
                {
                    newChild = dataTemplate.Build(content);
                    _recyclingDataTemplate = null;
                }
            }
            else
            {
                _recyclingDataTemplate = null;
            }

            return newChild;
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            return LayoutHelper.MeasureChild(Child, availableSize, Padding, BorderThickness);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            return ArrangeOverrideImpl(finalSize, new Vector());
        }

        internal Size ArrangeOverrideImpl(Size finalSize, Vector offset)
        {
            if (Child == null) return finalSize;

            var useLayoutRounding = UseLayoutRounding;
            var scale = LayoutHelper.GetLayoutScale(this);
            var padding = Padding;
            var borderThickness = BorderThickness;

            if (useLayoutRounding)
            {
                padding = LayoutHelper.RoundLayoutThickness(padding, scale, scale);
                borderThickness = LayoutHelper.RoundLayoutThickness(borderThickness, scale, scale);
            }

            padding += borderThickness;
            var horizontalContentAlignment = HorizontalContentAlignment;
            var verticalContentAlignment = VerticalContentAlignment;
            var availableSize = finalSize;
            var sizeForChild = availableSize;
            var originX = offset.X;
            var originY = offset.Y;

            if (horizontalContentAlignment != HorizontalAlignment.Stretch)
            {
                sizeForChild = sizeForChild.WithWidth(Math.Min(sizeForChild.Width, DesiredSize.Width));
            }

            if (verticalContentAlignment != VerticalAlignment.Stretch)
            {
                sizeForChild = sizeForChild.WithHeight(Math.Min(sizeForChild.Height, DesiredSize.Height));
            }

            if (useLayoutRounding)
            {
                sizeForChild = LayoutHelper.RoundLayoutSize(sizeForChild, scale, scale);
                availableSize = LayoutHelper.RoundLayoutSize(availableSize, scale, scale);
            }

            switch (horizontalContentAlignment)
            {
                case HorizontalAlignment.Center:
                    originX += (availableSize.Width - sizeForChild.Width) / 2;
                    break;
                case HorizontalAlignment.Right:
                    originX += availableSize.Width - sizeForChild.Width;
                    break;
            }

            switch (verticalContentAlignment)
            {
                case VerticalAlignment.Center:
                    originY += (availableSize.Height - sizeForChild.Height) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    originY += availableSize.Height - sizeForChild.Height;
                    break;
            }

            if (useLayoutRounding)
            {
                originX = LayoutHelper.RoundLayoutValue(originX, scale);
                originY = LayoutHelper.RoundLayoutValue(originY, scale);
            }

            var boundsForChild =
                new Rect(originX, originY, sizeForChild.Width, sizeForChild.Height).Deflate(padding);

            Child.Arrange(boundsForChild);

            return finalSize;
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
                if (e.Property.Name == nameof(Content))
                {
                    UpdateChild(e.NewValue);
                }
                else
                {
                    UpdateChild();
                }
            }
            else if (Child != null)
            {
                VisualChildren.Remove(Child);
                LogicalChildren.Remove(Child);
                ((ISetInheritanceParent)Child).SetParent(Child.Parent);
                Child = null;
                _recyclingDataTemplate = null;
            }

            UpdatePseudoClasses();
            InvalidateMeasure();
        }

        private void UpdatePseudoClasses()
        {
            PseudoClasses.Set(":empty", Content is null);
        }

        private void TemplatedParentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var host = e.NewValue as IContentPresenterHost;
            Host = host?.RegisterContentPresenter(this) == true ? host : null;
        }
    }
}
