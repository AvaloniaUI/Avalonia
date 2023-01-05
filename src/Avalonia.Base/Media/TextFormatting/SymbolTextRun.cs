namespace Avalonia.Media.TextFormatting;

/// <summary>
/// A text run that contains GlyphRun
/// </summary>
public abstract class SymbolTextRun : DrawableTextRun
{
    private GlyphRun? _glyphRun;
    
    protected SymbolTextRun(CharacterBufferReference characterBufferReference, TextRunProperties properties, 
        int length, sbyte biDiLevel)
    {
        CharacterBufferReference = characterBufferReference;
        Properties = properties;
        TextMetrics = new TextMetrics(properties.Typeface.GlyphTypeface, properties.FontRenderingEmSize);
        BidiLevel = biDiLevel;
        Length = length;
    }
    
    /// <inheritdoc/>
    public override CharacterBufferReference CharacterBufferReference { get; }
    
    /// <inheritdoc/>
    public override int Length { get; }
    
    public sbyte BidiLevel { get; }

    public TextMetrics TextMetrics { get; }

    public override double Baseline => -TextMetrics.Ascent;

    public override Size Size => GlyphRun.Size;

    public bool IsLeftToRight => (BidiLevel & 1) == 0;

    public bool IsReversed { get; private set; }
    
    public GlyphRun GlyphRun
    {
        get
        {
            if(_glyphRun is null)
            {
                _glyphRun = CreateGlyphRun();
            }

            return _glyphRun;
        }
    }

    /// <inheritdoc/>
    public override TextRunProperties Properties { get; }
        
    /// <inheritdoc/>
    public override void Draw(DrawingContext drawingContext, Point origin)
    {
        using (drawingContext.PushPreTransform(Matrix.CreateTranslation(origin)))
        {
            if (GlyphRun.GlyphIndices.Count == 0)
            {
                return;
            }

            if (Properties.Typeface == default)
            {
                return;
            }

            if (Properties.ForegroundBrush == null)
            {
                return;
            }

            if (Properties.BackgroundBrush != null)
            {
                drawingContext.DrawRectangle(Properties.BackgroundBrush, null, new Rect(Size));
            }

            drawingContext.DrawGlyphRun(Properties.ForegroundBrush, GlyphRun);

            if (Properties.TextDecorations == null)
            {
                return;
            }

            foreach (var textDecoration in Properties.TextDecorations)
            {
                textDecoration.Draw(drawingContext, GlyphRun, TextMetrics, Properties.ForegroundBrush);
            }
        }
    }

    internal void Reverse()
    {
        _glyphRun = null;

        ReverseInternal();

        IsReversed = !IsReversed;
    }

    internal abstract void ReverseInternal();
    
    internal abstract SplitResult<SymbolTextRun> Split(int length);
    
    internal abstract bool TryMeasureCharacters(double availableWidth, out int length);
    
    internal abstract bool TryMeasureCharactersBackwards(double availableWidth, out int length, out double width);

    internal abstract GlyphRun CreateGlyphRun();
}
