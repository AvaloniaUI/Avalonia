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

using System.Collections.Generic;
using Avalonia.Media;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;

namespace TheArtOfDev.HtmlRenderer.Avalonia.Adapters
{
    /// <summary>
    /// Adapter for Avalonia pens objects for core.
    /// </summary>
    internal sealed class PenAdapter : RPen
    {
        /// <summary>
        /// The actual Avalonia brush instance.
        /// </summary>
        private readonly IBrush _brush;

        /// <summary>
        /// the width of the pen
        /// </summary>
        private double _width;

        private DashStyle _dashStyle;

        /// <summary>
        /// the dash style of the pen
        /// </summary>
        //private DashStyle _dashStyle = DashStyles.Solid;

        /// <summary>
        /// Init.
        /// </summary>
        public PenAdapter(IBrush brush)
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
            set { DashStyles.TryGetValue(value, out _dashStyle); }
        }

        private static readonly Dictionary<RDashStyle, DashStyle> DashStyles = new Dictionary<RDashStyle, DashStyle>
        {
            {RDashStyle.Solid,null },
            {RDashStyle.Dash, global::Avalonia.Media.DashStyle.Dash },
            {RDashStyle.DashDot, global::Avalonia.Media.DashStyle.DashDot },
            {RDashStyle.DashDotDot, global::Avalonia.Media.DashStyle.DashDotDot },
            {RDashStyle.Dot, global::Avalonia.Media.DashStyle.Dot }
        };

        /// <summary>
        /// Create the actual Avalonia pen instance.
        /// </summary>
        public Pen CreatePen()
        {
            var pen = new Pen(_brush, _width, _dashStyle);
            return pen;
        }
    }
}