// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they begin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using Perspex.Media;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;

namespace TheArtOfDev.HtmlRenderer.Perspex.Adapters
{
    /// <summary>
    /// Adapter for Perspex pens objects for core.
    /// </summary>
    internal sealed class PenAdapter : RPen
    {
        /// <summary>
        /// The actual Perspex brush instance.
        /// </summary>
        private readonly Brush _brush;

        /// <summary>
        /// the width of the pen
        /// </summary>
        private double _width;

        /// <summary>
        /// the dash style of the pen
        /// </summary>
        //private DashStyle _dashStyle = DashStyles.Solid;

        /// <summary>
        /// Init.
        /// </summary>
        public PenAdapter(Brush brush)
        {
            _brush = brush;
        }

        public override double Width
        {
            get { return _width; }
            set { _width = value; }
        }

        public override RDashStyle DashStyle
        {
            set
            {
                //TODO: Implement DashStyles
                /*
                switch (value)
                {
                    case RDashStyle.Solid:
                        _dashStyle = DashStyles.Solid;
                        break;
                    case RDashStyle.Dash:
                        _dashStyle = DashStyles.Dash;
                        break;
                    case RDashStyle.Dot:
                        _dashStyle = DashStyles.Dot;
                        break;
                    case RDashStyle.DashDot:
                        _dashStyle = DashStyles.DashDot;
                        break;
                    case RDashStyle.DashDotDot:
                        _dashStyle = DashStyles.DashDotDot;
                        break;
                    default:
                        _dashStyle = DashStyles.Solid;
                        break;
                }*/
            }
        }

        /// <summary>
        /// Create the actual Perspex pen instance.
        /// </summary>
        public Pen CreatePen()
        {
            var pen = new Pen(_brush, _width);
            //pen.DashStyle = _dashStyle;
            return pen;
        }
    }
}