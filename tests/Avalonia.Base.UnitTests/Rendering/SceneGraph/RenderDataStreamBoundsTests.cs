using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.SceneGraph
{
    public class RenderDataStreamBoundsTests
    {
        [Fact]
        public void Empty_Stream_Has_Null_Bounds()
        {
            using var stream = new RenderDataStream();
            Assert.Null(stream.CalculateBounds());
        }

        [Fact]
        public void Filled_Rectangle_Bounds_Are_The_Rectangle()
        {
            using var stream = new RenderDataStream();
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 10, 10)), default);

            Assert.Equal(new Rect(0, 0, 10, 10), stream.CalculateBounds());
        }

        [Fact]
        public void Stroked_Rectangle_Bounds_Are_Inflated_By_Half_Thickness()
        {
            var pen = new ImmutablePen(Brushes.Black, 4);

            using var stream = new RenderDataStream();
            stream.DrawRectangle(null, pen, pen,
                new RoundedRect(new Rect(10, 10, 20, 20)), default);

            Assert.Equal(new Rect(8, 8, 24, 24), stream.CalculateBounds());
        }

        [Fact]
        public void Bounds_Are_The_Union_Of_All_Draws()
        {
            using var stream = new RenderDataStream();
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 10, 10)), default);
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(20, 20, 10, 10)), default);

            Assert.Equal(new Rect(0, 0, 30, 30), stream.CalculateBounds());
        }

        [Fact]
        public void Stroked_Ellipse_Bounds_Are_Inflated_By_Thickness()
        {
            var pen = new ImmutablePen(Brushes.Black, 4);

            using var stream = new RenderDataStream();
            stream.DrawEllipse(null, pen, pen, new Rect(0, 0, 10, 10));

            Assert.Equal(new Rect(-4, -4, 18, 18), stream.CalculateBounds());
        }

        [Fact]
        public void Bitmap_Bounds_Are_The_Destination_Rect()
        {
            using var bitmap = RefCountable.Create(Mock.Of<IBitmapImpl>());

            using var stream = new RenderDataStream();
            stream.DrawBitmap(bitmap, 1, new Rect(0, 0, 10, 10), new Rect(5, 5, 20, 20));

            Assert.Equal(new Rect(5, 5, 20, 20), stream.CalculateBounds());
        }

        [Fact]
        public void Custom_Operation_Bounds_Are_Used()
        {
            var operation = new Mock<ICustomDrawOperation>();
            operation.Setup(x => x.Bounds).Returns(new Rect(1, 2, 3, 4));

            using var stream = new RenderDataStream();
            stream.DrawCustom(operation.Object);

            Assert.Equal(new Rect(1, 2, 3, 4), stream.CalculateBounds());
        }

        [Fact]
        public void Line_Bounds_Cover_The_Segment()
        {
            var pen = new ImmutablePen(Brushes.Black, 2);

            using var stream = new RenderDataStream();
            stream.DrawLine(pen, pen, new Point(0, 0), new Point(100, 0));

            var bounds = Assert.NotNull(stream.CalculateBounds());
            Assert.True(bounds.Contains(new Point(50, 0)));
        }

        [Fact]
        public void Transform_Is_Applied_To_Bounds()
        {
            using var stream = new RenderDataStream();
            stream.PushTransform(Matrix.CreateTranslation(50, 50));
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 10, 10)), default);
            stream.Pop();

            Assert.Equal(new Rect(50, 50, 10, 10), stream.CalculateBounds());
        }

        [Fact]
        public void Nested_Transforms_Compose_For_Bounds()
        {
            using var stream = new RenderDataStream();
            stream.PushTransform(Matrix.CreateTranslation(20, 20));
            stream.PushTransform(Matrix.CreateScale(2, 2));
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 10, 10)), default);
            stream.Pop();
            stream.Pop();

            Assert.Equal(new Rect(20, 20, 20, 20), stream.CalculateBounds());
        }

        [Fact]
        public void Clip_Does_Not_Restrict_Bounds()
        {
            using var stream = new RenderDataStream();
            stream.PushClip(new RoundedRect(new Rect(0, 0, 5, 5)));
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 100, 100)), default);
            stream.Pop();

            Assert.Equal(new Rect(0, 0, 100, 100), stream.CalculateBounds());
        }

        [Fact]
        public void Opacity_Push_Is_Transparent_To_Bounds()
        {
            using var stream = new RenderDataStream();
            stream.PushOpacity(0.5);
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 10, 10)), default);
            stream.Pop();

            Assert.Equal(new Rect(0, 0, 10, 10), stream.CalculateBounds());
        }

        [Fact]
        public void Empty_Push_Scope_Contributes_Nothing()
        {
            using var stream = new RenderDataStream();
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 10, 10)), default);
            stream.PushTransform(Matrix.CreateTranslation(1000, 1000));
            stream.Pop();

            Assert.Equal(new Rect(0, 0, 10, 10), stream.CalculateBounds());
        }
    }
}
