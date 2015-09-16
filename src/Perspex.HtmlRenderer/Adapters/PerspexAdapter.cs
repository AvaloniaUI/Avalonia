using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Perspex;
using Perspex.Media;
using Perspex.Media.Imaging;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using TheArtOfDev.HtmlRenderer.Perspex.Utilities;

namespace TheArtOfDev.HtmlRenderer.Perspex.Adapters
{
    class PerspexAdapter : RAdapter
    {
        public static PerspexAdapter Instance { get; } = new PerspexAdapter();

        /// <summary>
        /// List of valid predefined color names in lower-case
        /// </summary>
        private static readonly Dictionary<string, Color> ColorNameDic = new Dictionary<string, Color>();
        

        static PerspexAdapter()
        {
            foreach (var colorProp in typeof(Colors).GetRuntimeProperties()
                .Where(p=>p.PropertyType == typeof(Color)))
            {
                ColorNameDic[colorProp.Name.ToLower()] = (Color)colorProp.GetValue(null);
            }
        }

        protected override RColor GetColorInt(string colorName)
        {
            Color c;
            if(!ColorNameDic.TryGetValue(colorName.ToLower(), out c))
                return RColor.Empty;
            return Util.Convert(c);
        }

        protected override RPen CreatePen(RColor color)
        {
            return new PenAdapter(GetSolidColorBrush(color));
        }

        /// <summary>
        /// Get solid color brush for the given color.
        /// </summary>
        private static Brush GetSolidColorBrush(RColor color)
        {
            Brush solidBrush;
            if (color == RColor.White)
                solidBrush = Brushes.White;
            else if (color == RColor.Black)
                solidBrush = Brushes.Black;
            else if (color.A < 1)
                solidBrush = Brushes.Transparent;
            else
                solidBrush = new SolidColorBrush(Util.Convert(color));
            return solidBrush;
        }

        protected override RBrush CreateSolidBrush(RColor color)
        {
            return new BrushAdapter(GetSolidColorBrush(color));
        }

        protected override RBrush CreateLinearGradientBrush(RRect rect, RColor color1, RColor color2, double angle)
        {
            var startColor = angle <= 180 ? Util.Convert(color1) : Util.Convert(color2);
            var endColor = angle <= 180 ? Util.Convert(color2) : Util.Convert(color1);
            angle = angle <= 180 ? angle : angle - 180;
            double x = angle < 135 ? Math.Max((angle - 45) / 90, 0) : 1;
            double y = angle <= 45 ? Math.Max(0.5 - angle / 90, 0) : angle > 135 ? Math.Abs(1.5 - angle / 90) : 0;
            return new BrushAdapter(new LinearGradientBrush
            {
                StartPoint = new RelativePoint(x, y, RelativeUnit.Percent), 
                EndPoint = new RelativePoint(1 - x, 1 - y, RelativeUnit.Percent),
                GradientStops =
                {
                    new GradientStop(startColor, 0),
                    new GradientStop(endColor, 1)
                }
            });

        }

        protected override RImage ConvertImageInt(object image)
        {
            return image != null ? new ImageAdapter((Bitmap)image) : null;
        }

        protected override RImage ImageFromStreamInt(Stream memoryStream)
        {
            //TODO: Implement bitmap loader
            return null;
        }

        protected override RFont CreateFontInt(string family, double size, RFontStyle style)
        {
            return new FontAdapter(family, size, style);
        }

        protected override RFont CreateFontInt(RFontFamily family, double size, RFontStyle style)
        {
            return new FontAdapter(family.Name, size, style);
        }
    }
}
