using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Threading.Tasks;

#if AVALONIA_CAIRO
namespace Avalonia.Cairo.RenderTests
#elif AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests
#endif
{
    public class SVGPathTests : TestBase
    {
        public SVGPathTests()
            :base("SVGPath")
        {
        }

        [Fact]
        public async Task SVGPath()
        {
            var target = new Canvas
            {
                Background = Brushes.Yellow,
                Width = 76,
                Height = 76,
                Children =
                {
                    new Path
                    {
                        Width = 32,
                        Height = 40,
                        [Canvas.LeftProperty] = 23,
                        [Canvas.TopProperty] = 18,
                        Stretch = Stretch.Fill,
                        Fill = Brushes.Black,
                        //Coffee Maker by Becris from the Noun Project
                        Data = StreamGeometry.Parse("M5,51v4c0,1.654,1.346,3,3,3h7v3c0,0.552,0.447,1,1,1h8c0.553,0,1-0.448,1-1v-3h18v3c0,0.552,0.447,1,1,1h8  c0.553,0,1-0.448,1-1v-3c2.757,0,5-2.243,5-5V13V7c0-2.757-2.243-5-5-5H11C8.243,2,6,4.243,6,7v2c0,2.757,2.243,5,5,5h1.743  l-2.717,11.775c-0.068,0.297,0.002,0.609,0.192,0.848C10.407,26.861,10.695,27,11,27h4c0.431,0,0.812-0.275,0.948-0.684L18.721,18  h1.499l1.811,7.243C22.142,25.688,22.541,26,23,26h12c0.459,0,0.858-0.312,0.97-0.757L37.78,18h6.658l-3.235,29.11  C41.147,47.618,40.72,48,40.21,48h-4.167c0.873-1.159,1.203-2.622,0.897-4.047L35,34.895v-2.481l2.707-2.707  c0.286-0.286,0.372-0.716,0.217-1.09C37.77,28.244,37.404,28,37,28H22c-0.553,0-1,0.448-1,1v0.719l-2.758-0.689  c-0.443-0.111-0.906,0.094-1.123,0.496l-7,13l1.762,0.948l6.631-12.315L21,31.781v3.115l-1.94,9.057  c-0.306,1.426,0.025,2.889,0.897,4.048H8C6.346,48,5,49.346,5,51z M23,60h-6v-2h6V60z M51,60h-6v-2h6V60z M8,9V7  c0-1.654,1.346-3,3-3h42c1.654,0,3,1.346,3,3v5H46H14h-3C9.346,12,8,10.654,8,9z M34.219,24H23.781l-1.5-6h13.438L34.219,24z   M44.66,16H37H21h-3c-0.431,0-0.812,0.275-0.948,0.684L14.279,25h-2.022l2.539-11h30.087l-0.185,1.662L44.66,16z M43.191,47.331  L46.896,14H56v39c0,1.654-1.346,3-3,3h-1h-8H24h-8H8c-0.552,0-1-0.449-1-1v-4c0-0.551,0.448-1,1-1h15.948h8.104h8.158  C41.741,50,43.022,48.853,43.191,47.331z M23,30h11.586l-1.293,1.293C33.105,31.48,33,31.735,33,32v2H23V30z M21.614,46.886  c-0.571-0.708-0.79-1.624-0.6-2.514L22.809,36h10.383l1.794,8.372c0.19,0.89-0.028,1.806-0.6,2.514  C33.813,47.594,32.963,48,32.052,48h-8.104C23.037,48,22.187,47.594,21.614,46.886z")
                    }
                }
            };

            await RenderToFile(target);
            CompareImages();
        }
    }
}
