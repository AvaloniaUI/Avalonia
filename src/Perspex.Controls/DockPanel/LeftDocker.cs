namespace Perspex.Controls
{
    public class LeftDocker : Docker
    {
        public LeftDocker(Size availableSize) : base(availableSize)
        {
        }

        public Rect GetDockingRect(Size sizeToDock, Margins margins, Alignments alignments)
        {
            var marginsCutout = margins.AsThickness();
            var withoutMargins = OriginalRect.Deflate(marginsCutout);
            var finalRect = withoutMargins.AlignChild(sizeToDock, Alignment.Start, alignments.Vertical);

            AccumulatedOffset += sizeToDock.Width;
            margins.HorizontalMargin = margins.HorizontalMargin.Offset(sizeToDock.Width, 0);

            return finalRect;
        }
    }
}