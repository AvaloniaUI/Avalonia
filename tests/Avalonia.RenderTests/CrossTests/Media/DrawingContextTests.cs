using Avalonia.Media;
using CrossUI;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests.CrossTests;
#else
namespace Avalonia.RenderTests.WpfCompare.CrossTests;
#endif


public class DrawingContextTests : CrossTestBase
{
    public DrawingContextTests() : base("Media/DrawingContext")
    {
    }

    [CrossFact]
    public void Transform_Should_Work_As_Expected()
    {
        RenderAndCompare(

            new CrossFuncControl(ctx =>
            {
                ctx.PushTransform(Matrix.CreateTranslation(100, 100));
                ctx.DrawLine(new CrossPen { Brush = new CrossSolidColorBrush(Colors.Red), Thickness = 1 },
                    new Point(0, 0), new Point(100, 0));
                ctx.Pop();

                ctx.PushTransform(Matrix.CreateTranslation(200, 100));
                ctx.DrawLine(new CrossPen { Brush = new CrossSolidColorBrush(Colors.Orange), Thickness = 1 },
                    new Point(0, 0), new Point(0, 100));
                ctx.Pop();

                ctx.PushTransform(Matrix.CreateTranslation(200, 200));
                ctx.DrawLine(
                    new CrossPen { Brush = new CrossSolidColorBrush(Colors.Yellow), Thickness = 1 },
                    new Point(0, 0), new Point(-100, 0));
                ctx.Pop();

                ctx.PushTransform(Matrix.CreateTranslation(100, 200));
                ctx.DrawLine(new CrossPen { Brush = new CrossSolidColorBrush(Colors.Green), Thickness = 1 },
                    new Point(0, 0), new Point(0, -100));
                ctx.Pop();
            }) { Width = 300, Height = 300 }

        );

    }
}
