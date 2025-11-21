using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace TextTestApp
{
    public class InteractiveLineControl : Control
    {
        /// <summary>
        /// Defines the <see cref="Text" /> property.
        /// </summary>
        public static readonly StyledProperty<string?> TextProperty =
            TextBlock.TextProperty.AddOwner<InteractiveLineControl>();

        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<InteractiveLineControl>();

        public static readonly StyledProperty<IBrush?> ExtentStrokeProperty =
            AvaloniaProperty.Register<InteractiveLineControl, IBrush?>(nameof(ExtentStroke));

        public static readonly StyledProperty<IBrush?> BaselineStrokeProperty =
            AvaloniaProperty.Register<InteractiveLineControl, IBrush?>(nameof(BaselineStroke));

        public static readonly StyledProperty<IBrush?> TextBoundsStrokeProperty =
            AvaloniaProperty.Register<InteractiveLineControl, IBrush?>(nameof(TextBoundsStroke));

        public static readonly StyledProperty<IBrush?> RunBoundsStrokeProperty =
            AvaloniaProperty.Register<InteractiveLineControl, IBrush?>(nameof(RunBoundsStroke));

        public static readonly StyledProperty<IBrush?> NextHitStrokeProperty =
            AvaloniaProperty.Register<InteractiveLineControl, IBrush?>(nameof(NextHitStroke));

        public static readonly StyledProperty<IBrush?> BackspaceHitStrokeProperty =
            AvaloniaProperty.Register<InteractiveLineControl, IBrush?>(nameof(BackspaceHitStroke));

        public static readonly StyledProperty<IBrush?> PreviousHitStrokeProperty =
            AvaloniaProperty.Register<InteractiveLineControl, IBrush?>(nameof(PreviousHitStroke));

        public static readonly StyledProperty<IBrush?> DistanceStrokeProperty =
            AvaloniaProperty.Register<InteractiveLineControl, IBrush?>(nameof(DistanceStroke));

        public IBrush? ExtentStroke
        {
            get => GetValue(ExtentStrokeProperty);
            set => SetValue(ExtentStrokeProperty, value);
        }
        public IBrush? BaselineStroke
        {
            get => GetValue(BaselineStrokeProperty);
            set => SetValue(BaselineStrokeProperty, value);
        }

        public IBrush? TextBoundsStroke
        {
            get => GetValue(TextBoundsStrokeProperty);
            set => SetValue(TextBoundsStrokeProperty, value);
        }

        public IBrush? RunBoundsStroke
        {
            get => GetValue(RunBoundsStrokeProperty);
            set => SetValue(RunBoundsStrokeProperty, value);
        }

        public IBrush? NextHitStroke
        {
            get => GetValue(NextHitStrokeProperty);
            set => SetValue(NextHitStrokeProperty, value);
        }

        public IBrush? BackspaceHitStroke 
        {
            get => GetValue(BackspaceHitStrokeProperty);
            set => SetValue(BackspaceHitStrokeProperty, value);
        }

        public IBrush? PreviousHitStroke
        {
            get => GetValue(PreviousHitStrokeProperty);
            set => SetValue(PreviousHitStrokeProperty, value);
        }

        public IBrush? DistanceStroke 
        {
            get => GetValue(DistanceStrokeProperty);
            set => SetValue(DistanceStrokeProperty, value);
        }

        private IPen? _extentPen;
        protected IPen ExtentPen => _extentPen ??= new Pen(ExtentStroke, dashStyle: DashStyle.Dash);

        private IPen? _baselinePen;
        protected IPen BaselinePen => _baselinePen ??= new Pen(BaselineStroke);

        private IPen? _textBoundsPen;
        protected IPen TextBoundsPen => _textBoundsPen ??= new Pen(TextBoundsStroke);

        private IPen? _runBoundsPen;
        protected IPen RunBoundsPen => _runBoundsPen ??= new Pen(RunBoundsStroke, dashStyle: DashStyle.Dash);

        private IPen? _nextHitPen;
        protected IPen NextHitPen => _nextHitPen ??= new Pen(NextHitStroke);

        private IPen? _previousHitPen;
        protected IPen PreviousHitPen => _previousHitPen ??= new Pen(PreviousHitStroke);

        private IPen? _backspaceHitPen;
        protected IPen BackspaceHitPen => _backspaceHitPen ??= new Pen(BackspaceHitStroke);

        private IPen? _distancePen;
        protected IPen DistancePen => _distancePen ??= new Pen(DistanceStroke);

        /// <summary>
        /// Gets or sets the text to draw.
        /// </summary>
        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Gets or sets a brush used to paint the control's background.
        /// </summary>
        public IBrush? Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        // TextRunProperties

        /// <summary>
        /// Defines the <see cref="FontFamily"/> property.
        /// </summary>
        public static readonly StyledProperty<FontFamily> FontFamilyProperty =
            TextElement.FontFamilyProperty.AddOwner<InteractiveLineControl>();

        /// <summary>
        /// Defines the <see cref="FontFeaturesProperty"/> property.
        /// </summary>
        public static readonly StyledProperty<FontFeatureCollection?> FontFeaturesProperty =
            TextElement.FontFeaturesProperty.AddOwner<InteractiveLineControl>();

        /// <summary>
        /// Defines the <see cref="FontSize"/> property.
        /// </summary>
        public static readonly StyledProperty<double> FontSizeProperty =
            TextElement.FontSizeProperty.AddOwner<InteractiveLineControl>();

        /// <summary>
        /// Defines the <see cref="FontStyle"/> property.
        /// </summary>
        public static readonly StyledProperty<FontStyle> FontStyleProperty =
            TextElement.FontStyleProperty.AddOwner<InteractiveLineControl>();

        /// <summary>
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly StyledProperty<FontWeight> FontWeightProperty =
            TextElement.FontWeightProperty.AddOwner<InteractiveLineControl>();

        /// <summary>
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly StyledProperty<FontStretch> FontStretchProperty =
            TextElement.FontStretchProperty.AddOwner<InteractiveLineControl>();

        /// <summary>
        /// Gets or sets the font family used to draw the control's text.
        /// </summary>
        public FontFamily FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Gets or sets the font features turned on/off.
        /// </summary>
        public FontFeatureCollection? FontFeatures
        {
            get => GetValue(FontFeaturesProperty);
            set => SetValue(FontFeaturesProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the control's text in points.
        /// </summary>
        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the font style used to draw the control's text.
        /// </summary>
        public FontStyle FontStyle
        {
            get => GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the font weight used to draw the control's text.
        /// </summary>
        public FontWeight FontWeight
        {
            get => GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the font stretch used to draw the control's text.
        /// </summary>
        public FontStretch FontStretch
        {
            get => GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        private GenericTextRunProperties? _textRunProperties;
        public GenericTextRunProperties TextRunProperties
        {
            get
            {
                return _textRunProperties ??= CreateTextRunProperties();
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _textRunProperties = value;
                SetCurrentValue(FontFamilyProperty, value.Typeface.FontFamily);
                SetCurrentValue(FontFeaturesProperty, value.FontFeatures);
                SetCurrentValue(FontSizeProperty, value.FontRenderingEmSize);
                SetCurrentValue(FontStyleProperty, value.Typeface.Style);
                SetCurrentValue(FontWeightProperty, value.Typeface.Weight);
                SetCurrentValue(FontStretchProperty, value.Typeface.Stretch);
            }
        }

        private GenericTextRunProperties CreateTextRunProperties()
        {
            Typeface typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            return new GenericTextRunProperties(typeface, FontFeatures, FontSize,
                textDecorations: null,
                foregroundBrush: Brushes.Black,
                backgroundBrush: null,
                baselineAlignment: BaselineAlignment.Baseline,
                cultureInfo: null);
        }

        // TextParagraphProperties

        private GenericTextParagraphProperties? _textParagraphProperties;
        public GenericTextParagraphProperties TextParagraphProperties
        {
            get
            {
                return _textParagraphProperties ??= CreateTextParagraphProperties();
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _textParagraphProperties = null;
                SetCurrentValue(FlowDirectionProperty, value.FlowDirection);
            }
        }

        private GenericTextParagraphProperties CreateTextParagraphProperties()
        {
            return new GenericTextParagraphProperties(
                FlowDirection,
                TextAlignment.Start,
                firstLineInParagraph: false,
                alwaysCollapsible: false,
                TextRunProperties,
                textWrapping: TextWrapping.NoWrap,
                lineHeight: 0,
                indent: 0,
                letterSpacing: 0);
        }

        private readonly ITextSource _textSource;
        private class TextSource : ITextSource
        {
            private readonly InteractiveLineControl _owner;

            public TextSource(InteractiveLineControl owner)
            {
                _owner = owner;
            }

            public TextRun? GetTextRun(int textSourceIndex)
            {
                string text = _owner.Text ?? string.Empty;

                if (textSourceIndex < 0 || textSourceIndex >= text.Length)
                    return null;

                return new TextCharacters(text, _owner.TextRunProperties);
            }
        }

        private TextLine? _textLine;
        public TextLine? TextLine => _textLine ??= TextFormatter.Current.FormatLine(_textSource, 0, Bounds.Size.Width, TextParagraphProperties);

        private TextLayout? _textLayout;
        public TextLayout TextLayout => _textLayout ??= new TextLayout(_textSource, TextParagraphProperties);

        private Size? _textLineSize;
        protected Size TextLineSize => _textLineSize ??= TextLine is { } textLine ? new Size(textLine.WidthIncludingTrailingWhitespace, textLine.Height) : default;

        private Size? _inkSize;
        protected Size InkSize => _inkSize ??= TextLine is { } textLine ? new Size(-textLine.OverhangLeading + textLine.WidthIncludingTrailingWhitespace - textLine.OverhangTrailing, textLine.Extent) : default;

        public event EventHandler? TextLineChanged;

        public InteractiveLineControl()
        {
            _textSource = new TextSource(this);

            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.SubpixelAntialias);
        }

        private void InvalidateTextRunProperties()
        {
            _textRunProperties = null;
            InvalidateTextParagraphProperties();
        }

        private void InvalidateTextParagraphProperties()
        {
            _textParagraphProperties = null;
            InvalidateTextLine();
        }

        private void InvalidateTextLine()
        {
            _textLayout = null;
            _textLine = null;
            _textLineSize = null;
            _inkSize = null;
            InvalidateMeasure();
            InvalidateVisual();
            
            TextLineChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            switch (change.Property.Name)
            {
                case nameof(FontFamily):
                case nameof(FontSize):
                    InvalidateTextRunProperties();
                    break;

                case nameof(FontFeatures):
                    if (change.OldValue is FontFeatureCollection oc)
                        oc.CollectionChanged -= OnFeatureCollectionChanged;
                    if (change.NewValue is FontFeatureCollection nc)
                        nc.CollectionChanged += OnFeatureCollectionChanged;
                    OnFeatureCollectionChanged(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    break;

                case nameof(FontStyle):
                case nameof(FontWeight):
                case nameof(FontStretch):
                    InvalidateTextRunProperties();
                    break;

                case nameof(FlowDirection):
                    InvalidateTextParagraphProperties();
                    break;

                case nameof(Text):
                    InvalidateTextLine();
                    break;

                case nameof(BaselineStroke):
                    _baselinePen = null;
                    InvalidateVisual();
                    break;

                case nameof(TextBoundsStroke):
                    _textBoundsPen = null;
                    InvalidateVisual();
                    break;

                case nameof(RunBoundsStroke):
                    _runBoundsPen = null;
                    InvalidateVisual();
                    break;

                case nameof(NextHitStroke):
                    _nextHitPen = null;
                    InvalidateVisual();
                    break;

                case nameof(PreviousHitStroke):
                    _previousHitPen = null;
                    InvalidateVisual();
                    break;

                case nameof(BackspaceHitStroke):
                    _backspaceHitPen = null;
                    InvalidateVisual();
                    break;
            }

            base.OnPropertyChanged(change);
        }

        private void OnFeatureCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            InvalidateTextRunProperties();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (TextLine == null)
                return default;

            return new Size(Math.Max(TextLineSize.Width, InkSize.Width), Math.Max(TextLineSize.Height, InkSize.Height));
        }

        private const double VerticalSpacing = 5;
        private const double HorizontalSpacing = 5;
        private const double ArrowSize = 5;
        private const double LabelFontSize = 9;

        private Dictionary<string, FormattedText> _labelsCache = new();
        protected FormattedText GetOrCreateLabel(string label, IBrush brush, bool disableCache = false)
        {
            if (_labelsCache.TryGetValue(label, out var text))
                return text;

            text = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, LabelFontSize, brush);

            if (!disableCache)
                _labelsCache[label] = text;

            return text;
        }

        private Rect _inkRenderBounds;
        private Rect _lineRenderBounds;

        public Rect InkRenderBounds => _inkRenderBounds;
        public Rect LineRenderBounds => _lineRenderBounds;

        public override void Render(DrawingContext context)
        {
            TextLine? textLine = TextLine;
            if (textLine == null)
                return;

            // overhang leading should be negative when extending (e.g. for j)   WPF: "When the leading alignment point comes before the leading drawn pixel, the value is negative." - docs wrong but values correct
            // overhang trailing should be negative when extending (e.g. for f)  WPF: "The OverhangTrailing value will be positive when the trailing drawn pixel comes before the trailing alignment point."
            // overhang after should be negative when inside (e.g. for x) WPF: "The value is positive if the bottommost drawn pixel goes below the line bottom, and is negative if it is within (on or above) the line."
            // => we want overhang before to be negative when inside (e.g. for x) 

            double overhangBefore = textLine.Extent - textLine.OverhangAfter - textLine.Height;
            Rect inkBounds = new Rect(new Point(textLine.OverhangLeading, -overhangBefore), InkSize);
            Rect lineBounds = new Rect(new Point(0, 0), TextLineSize);

            if (inkBounds.Left < 0)
                lineBounds = lineBounds.Translate(new Vector(-inkBounds.Left, 0));

            if (inkBounds.Top < 0)
                lineBounds = lineBounds.Translate(new Vector(0, -inkBounds.Top));

            _inkRenderBounds = inkBounds;
            _lineRenderBounds = lineBounds;

            Rect bounds = new Rect(0, 0, Math.Max(inkBounds.Right, lineBounds.Right), Math.Max(inkBounds.Bottom, lineBounds.Bottom));
            double labelX = bounds.Right + HorizontalSpacing;

            if (Background is IBrush background)
                context.FillRectangle(background, lineBounds);

            if (ExtentStroke != null)
            {
                context.DrawRectangle(ExtentPen, inkBounds);
                RenderLabel(context, nameof(textLine.Extent), ExtentStroke, labelX, inkBounds.Top);
            }

            using (context.PushTransform(Matrix.CreateTranslation(lineBounds.Left, lineBounds.Top)))
            {
                labelX -= lineBounds.Left; // labels to ignore horizontal transform

                if (BaselineStroke != null)
                {
                    RenderFontLine(context, textLine.Baseline, lineBounds.Width, BaselinePen); // no other lines currently available in Avalonia
                    RenderLabel(context, nameof(textLine.Baseline), BaselineStroke, labelX, textLine.Baseline);
                }

                textLine.Draw(context, lineOrigin: default);

                var runBoundsStroke = RunBoundsStroke;
                if (TextBoundsStroke != null || runBoundsStroke != null)
                {
                    IReadOnlyList<TextBounds> textBounds = textLine.GetTextBounds(textLine.FirstTextSourceIndex, textLine.Length);
                    foreach (var textBound in textBounds)
                    {
                        if (runBoundsStroke != null)
                        {
                            var runBounds = textBound.TextRunBounds;
                            foreach (var runBound in runBounds)
                                context.DrawRectangle(RunBoundsPen, runBound.Rectangle);
                        }

                        context.DrawRectangle(TextBoundsPen, textBound.Rectangle);
                    }
                }

                double y = Math.Max(inkBounds.Bottom, lineBounds.Bottom) + VerticalSpacing * 2;

                if (NextHitStroke != null)
                {
                    RenderHits(context, NextHitPen, textLine, textLine.GetNextCaretCharacterHit, new CharacterHit(0), ref y);
                    RenderLabel(context, nameof(textLine.GetNextCaretCharacterHit), NextHitStroke, labelX, y);
                    y += VerticalSpacing * 2;
                }

                if (PreviousHitStroke != null)
                {
                    RenderLabel(context, nameof(textLine.GetPreviousCaretCharacterHit), PreviousHitStroke, labelX, y);
                    RenderHits(context, PreviousHitPen, textLine, textLine.GetPreviousCaretCharacterHit, new CharacterHit(textLine.Length), ref y);
                    y += VerticalSpacing * 2;
                }

                if (BackspaceHitStroke != null)
                {
                    RenderLabel(context, nameof(textLine.GetBackspaceCaretCharacterHit), BackspaceHitStroke, labelX, y);
                    RenderHits(context, BackspaceHitPen, textLine, textLine.GetBackspaceCaretCharacterHit, new CharacterHit(textLine.Length), ref y);
                    y += VerticalSpacing * 2;
                }

                if (DistanceStroke != null)
                {
                    y += VerticalSpacing;

                    var label = RenderLabel(context, nameof(textLine.GetDistanceFromCharacterHit), DistanceStroke, 0, y);
                    y += label.Height;

                    for (int i = 0; i < textLine.Length; i++)
                    {
                        var hit = new CharacterHit(i);
                        CharacterHit prevHit = default, nextHit = default;

                        double leftLabelX = -HorizontalSpacing;

                        // we want z-order to be previous, next, distance
                        // but labels need to be ordered next, distance, previous
                        if (NextHitStroke != null)
                        {
                            nextHit = textLine.GetNextCaretCharacterHit(hit);
                            var nextLabel = RenderLabel(context, $" > {nextHit.FirstCharacterIndex}+{nextHit.TrailingLength}", NextHitStroke, leftLabelX, y, TextAlignment.Right, disableCache: true);
                            leftLabelX -= nextLabel.WidthIncludingTrailingWhitespace;
                        }

                        if (BackspaceHitStroke != null)
                        {
                            CharacterHit backHit = textLine.GetBackspaceCaretCharacterHit(hit);
                            var x1 = textLine.GetDistanceFromCharacterHit(new CharacterHit(backHit.FirstCharacterIndex, 0));
                            var x2 = textLine.GetDistanceFromCharacterHit(new CharacterHit(backHit.FirstCharacterIndex + backHit.TrailingLength, 0));
                            RenderHorizontalPoint(context, x1, x2, y, BackspaceHitPen, ArrowSize);
                        }

                        if (PreviousHitStroke != null)
                        {
                            prevHit = textLine.GetPreviousCaretCharacterHit(hit);
                            var x1 = textLine.GetDistanceFromCharacterHit(new CharacterHit(prevHit.FirstCharacterIndex, 0));
                            var x2 = textLine.GetDistanceFromCharacterHit(new CharacterHit(prevHit.FirstCharacterIndex + prevHit.TrailingLength, 0));
                            RenderHorizontalPoint(context, x1, x2, y, PreviousHitPen, ArrowSize);
                        }

                        if (NextHitStroke != null)
                        {
                            var x1 = textLine.GetDistanceFromCharacterHit(new CharacterHit(nextHit.FirstCharacterIndex, 0));
                            var x2 = textLine.GetDistanceFromCharacterHit(new CharacterHit(nextHit.FirstCharacterIndex + nextHit.TrailingLength, 0));
                            RenderHorizontalPoint(context, x1, x2, y, NextHitPen, ArrowSize);
                        }

                        label = RenderLabel(context, $"[{i}]", DistanceStroke, leftLabelX, y, TextAlignment.Right);
                        leftLabelX -= label.WidthIncludingTrailingWhitespace;

                        if (PreviousHitStroke != null)
                            RenderLabel(context, $"{prevHit.FirstCharacterIndex}+{prevHit.TrailingLength} < ", PreviousHitStroke, leftLabelX, y, TextAlignment.Right, disableCache: true);

                        double distance = textLine.GetDistanceFromCharacterHit(new CharacterHit(i));
                        RenderHorizontalBar(context, 0, distance, y, DistancePen, ArrowSize);
                        //RenderLabel(context, distance.ToString("F2"), DistanceStroke, distance + HorizontalSpacing, y, disableCache: true);

                        y += label.Height;
                    }
                }
            }
        }

        [return: NotNullIfNotNull("brush")]
        private FormattedText? RenderLabel(DrawingContext context, string label, IBrush? brush, double x, double y, TextAlignment alignment = TextAlignment.Left, bool disableCache = false)
        {
            if (brush == null)
                return null;

            var text = GetOrCreateLabel(label, brush, disableCache);
            
            if (alignment == TextAlignment.Right)
                context.DrawText(text, new Point(x - text.WidthIncludingTrailingWhitespace, y - text.Height / 2));
            else
                context.DrawText(text, new Point(x, y - text.Height / 2));

            return text;
        }

        private void RenderHits(DrawingContext context, IPen hitPen, TextLine textLine, Func<CharacterHit, CharacterHit> nextHit, CharacterHit startingHit, ref double y)
        {
            CharacterHit lastHit = startingHit;
            double lastX = textLine.GetDistanceFromCharacterHit(lastHit);
            double lastDirection = 0;
            y -= VerticalSpacing; // we always start with adding one below

            while (true)
            {
                CharacterHit hit = nextHit(lastHit);
                if (hit == lastHit)
                    break;

                double x = textLine.GetDistanceFromCharacterHit(hit);
                double direction = Math.Sign(x - lastX);

                if (direction == 0 || lastDirection != direction)
                    y += VerticalSpacing;

                if (direction == 0)
                    RenderPoint(context, x, y, hitPen, ArrowSize);
                else
                    RenderHorizontalArrow(context, lastX, x, y, hitPen, ArrowSize);
                 
                lastX = x;
                lastHit = hit;
                lastDirection = direction;
            }
        }

        private void RenderPoint(DrawingContext context, double x, double y, IPen pen, double arrowHeight)
        {
            context.DrawEllipse(pen.Brush, pen, new Point(x, y), ArrowSize / 2, ArrowSize / 2);
        }

        private void RenderHorizontalPoint(DrawingContext context, double xStart, double xEnd, double y, IPen pen, double size)
        {
            PathGeometry startCap = new PathGeometry();
            PathFigure startFigure = new PathFigure();
            startFigure.StartPoint = new Point(xStart, y - size / 2);
            startFigure.IsClosed = true;
            startFigure.IsFilled = true;
            startFigure.Segments!.Add(new ArcSegment { Size = new Size(size / 2, size / 2), Point = new Point(xStart, y + size / 2), SweepDirection = SweepDirection.CounterClockwise });
            startCap.Figures!.Add(startFigure);

            context.DrawGeometry(pen.Brush, pen, startCap);

            PathGeometry endCap = new PathGeometry();
            PathFigure endFigure = new PathFigure();
            endFigure.StartPoint = new Point(xEnd, y - size / 2);
            endFigure.IsClosed = true;
            endFigure.IsFilled = false;
            endFigure.Segments!.Add(new ArcSegment { Size = new Size(size / 2, size / 2), Point = new Point(xEnd, y + size / 2), SweepDirection = SweepDirection.Clockwise });
            endCap.Figures!.Add(endFigure);

            context.DrawGeometry(pen.Brush, pen, endCap);
        }

        private void RenderHorizontalArrow(DrawingContext context, double xStart, double xEnd, double y, IPen pen, double size)
        {
            context.DrawLine(pen, new Point(xStart, y), new Point(xEnd, y));
            context.DrawLine(pen, new Point(xStart, y - size / 2), new Point(xStart, y + size / 2)); // start cap

            if (xEnd >= xStart)
                context.DrawGeometry(pen.Brush, pen, new PolylineGeometry(
                [
                    new Point(xEnd - size, y - size / 2),
                    new Point(xEnd - size, y + size/2),
                    new Point(xEnd, y)
                ], isFilled: true));
            else
                context.DrawGeometry(pen.Brush, pen, new PolylineGeometry(
                [
                    new Point(xEnd + size, y - size / 2),
                    new Point(xEnd + size, y + size/2),
                    new Point(xEnd, y)
                ], isFilled: true));
        }
        private void RenderHorizontalBar(DrawingContext context, double xStart, double xEnd, double y, IPen pen, double size)
        {
            context.DrawLine(pen, new Point(xStart, y), new Point(xEnd, y));
            context.DrawLine(pen, new Point(xStart, y - size / 2), new Point(xStart, y + size / 2)); // start cap
            context.DrawLine(pen, new Point(xEnd, y - size / 2), new Point(xEnd, y + size / 2)); // end cap
        }

        private void RenderFontLine(DrawingContext context, double y, double width, IPen pen)
        {
            context.DrawLine(pen, new Point(0, y), new Point(width, y));
        }
    }
}
