using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Xunit;
#pragma warning disable CS0649

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests;

public class DrawingContextTests : TestBase
{
    public DrawingContextTests() : base(@"Media\DrawingContext")
    {
    }

    [Fact]
    public async Task Should_Render_LinesAndText()
    {
        var target = new Border
        {
            Width = 300,
            Height = 300,
            Background = Brushes.White,
            Child = new RenderControl()
        };
        
        await RenderToFile(target);
        CompareImages(skipImmediate: true);
    }

    internal class RenderControl : Control
    {
        private static readonly Typeface s_typeface = new Typeface(TestFontFamily);

        public override void Render(DrawingContext context)
        {
            var pen = new Pen(Brushes.LightGray, 10);
            RenderLine1(context, pen);
            RenderLine2(context, pen);
            RenderLine3(context, pen);
            RenderLine4(context, pen);

            RenderLine1(context, new Pen(Brushes.Red));
            RenderAText(context, new Point(50, 20));
            RenderLine2(context, new Pen(Brushes.Orange));
            RenderAText(context, new Point(50, -50));
            RenderLine3(context, new Pen(Brushes.Yellow));
            RenderAText(context, new Point(0, 0));
            RenderLine4(context, new Pen(Brushes.Green));
        }

        private static void RenderLine1(DrawingContext context, IPen pen) => context.DrawLine(pen, new Point(100, 100), new Point(200, 100));
        private static void RenderLine2(DrawingContext context, IPen pen) => context.DrawLine(pen, new Point(200, 100), new Point(200, 200));
        private static void RenderLine3(DrawingContext context, IPen pen) => context.DrawLine(pen, new Point(200, 200), new Point(100, 200));
        private static void RenderLine4(DrawingContext context, IPen pen) => context.DrawLine(pen, new Point(100, 200), new Point(100, 100));

        private static void RenderAText(DrawingContext context, Point point)
        {
            using (context.PushOpacity(0.7))
            {
                context.DrawText(
                    new FormattedText("any text to render", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        s_typeface, 12, Brushes.Black), point);
            }
        }
    }
}
#endif
