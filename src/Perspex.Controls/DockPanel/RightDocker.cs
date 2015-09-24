namespace Perspex.Controls
{
    using Layout;

    public class RightDocker : Docker
    {
        public RightDocker(Size availableSize) : base(availableSize)
        {
        }

        public Rect GetDockingRect(Size sizeToDock, Margins margins, Alignments alignments)
        {
            var marginsCutout = margins.AsThickness();
            var withoutMargins = OriginalRect.Deflate(marginsCutout);
            var finalRect = withoutMargins.AlignChild(sizeToDock, Alignment.End, alignments.Vertical);

            AccumulatedOffset += sizeToDock.Width;
            margins.HorizontalMargin = margins.HorizontalMargin.Offset(0, sizeToDock.Width);

            return finalRect;
        }
    }
}