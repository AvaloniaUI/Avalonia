namespace Perspex.Controls
{
    public struct Alignments
    {
        private readonly Alignment _horizontal;
        private readonly Alignment _vertical;

        public Alignments(Alignment horizontal, Alignment vertical)
        {
            _horizontal = horizontal;
            _vertical = vertical;
        }

        public Alignment Horizontal
        {
            get { return _horizontal; }
        }

        public Alignment Vertical
        {
            get { return _vertical; }
        }
    }
}