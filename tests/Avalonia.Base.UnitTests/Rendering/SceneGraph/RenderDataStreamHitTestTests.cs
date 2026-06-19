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
    public class RenderDataStreamHitTestTests
    {
        [Fact]
        public void Filled_Rectangle_Is_Hit_Inside_And_Missed_Outside()
        {
            using var stream = new RenderDataStream();
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 100, 100)), default);

            Assert.True(stream.HitTest(new Point(50, 50)));
            Assert.False(stream.HitTest(new Point(150, 150)));
        }

        [Fact]
        public void Stroked_Rectangle_Is_Hit_On_Border_And_Missed_In_Hollow_Center()
        {
            var pen = new ImmutablePen(Brushes.Black, 4);

            using var stream = new RenderDataStream();
            stream.DrawRectangle(null, pen, pen,
                new RoundedRect(new Rect(0, 0, 100, 100)), default);

            Assert.True(stream.HitTest(new Point(0, 50)));
            Assert.False(stream.HitTest(new Point(50, 50)));
        }

        [Fact]
        public void Line_Is_Hit_Along_Its_Length()
        {
            var pen = new ImmutablePen(Brushes.Black, 4);

            using var stream = new RenderDataStream();
            stream.DrawLine(pen, pen, new Point(0, 0), new Point(100, 0));

            Assert.True(stream.HitTest(new Point(50, 1)));
            Assert.False(stream.HitTest(new Point(50, 50)));
        }

        [Fact]
        public void Filled_Ellipse_Is_Hit_At_Center_And_Missed_At_Corner()
        {
            using var stream = new RenderDataStream();
            stream.DrawEllipse(Brushes.Black, null, null, new Rect(0, 0, 100, 100));

            Assert.True(stream.HitTest(new Point(50, 50)));
            Assert.False(stream.HitTest(new Point(2, 2)));
        }

        [Fact]
        public void Geometry_Is_Hit_Via_FillContains()
        {
            var geometry = new Mock<IGeometryImpl>();
            geometry.Setup(x => x.FillContains(new Point(5, 5))).Returns(true);
            geometry.Setup(x => x.FillContains(new Point(50, 50))).Returns(false);

            using var stream = new RenderDataStream();
            stream.DrawGeometry(Brushes.Black, null, null, geometry.Object);

            Assert.True(stream.HitTest(new Point(5, 5)));
            Assert.False(stream.HitTest(new Point(50, 50)));
        }

        [Fact]
        public void Bitmap_Is_Hit_Within_Its_Destination_Rect()
        {
            using var bitmap = RefCountable.Create(Mock.Of<IBitmapImpl>());

            using var stream = new RenderDataStream();
            stream.DrawBitmap(bitmap, 1, new Rect(0, 0, 10, 10), new Rect(20, 20, 30, 30));

            Assert.True(stream.HitTest(new Point(25, 25)));
            Assert.False(stream.HitTest(new Point(5, 5)));
        }

        [Fact]
        public void Custom_Operation_Hit_Test_Is_Delegated()
        {
            var operation = new Mock<ICustomDrawOperation>();
            operation.Setup(x => x.HitTest(new Point(5, 5))).Returns(true);

            using var stream = new RenderDataStream();
            stream.DrawCustom(operation.Object);

            Assert.True(stream.HitTest(new Point(5, 5)));
            Assert.False(stream.HitTest(new Point(99, 99)));
        }

        [Fact]
        public void Clip_Restricts_The_Hit_Region()
        {
            using var stream = new RenderDataStream();
            stream.PushClip(new RoundedRect(new Rect(0, 0, 10, 10)));
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 100, 100)), default);
            stream.Pop();

            Assert.True(stream.HitTest(new Point(5, 5)));
            Assert.False(stream.HitTest(new Point(50, 50)));
        }

        [Fact]
        public void Geometry_Clip_Restricts_The_Hit_Region()
        {
            var geometry = new Mock<IGeometryImpl>();
            geometry.Setup(x => x.FillContains(new Point(5, 5))).Returns(true);
            geometry.Setup(x => x.FillContains(new Point(50, 50))).Returns(false);

            using var stream = new RenderDataStream();
            stream.PushGeometryClip(geometry.Object);
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 100, 100)), default);
            stream.Pop();

            Assert.True(stream.HitTest(new Point(5, 5)));
            Assert.False(stream.HitTest(new Point(50, 50)));
        }

        [Fact]
        public void Transform_Maps_Hit_Test_Coordinates()
        {
            using var stream = new RenderDataStream();
            stream.PushTransform(Matrix.CreateTranslation(50, 50));
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 10, 10)), default);
            stream.Pop();

            Assert.True(stream.HitTest(new Point(55, 55)));
            Assert.False(stream.HitTest(new Point(5, 5)));
        }

        [Fact]
        public void Nested_Transforms_Compose()
        {
            using var stream = new RenderDataStream();
            stream.PushTransform(Matrix.CreateTranslation(20, 20));
            stream.PushTransform(Matrix.CreateScale(2, 2));
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 10, 10)), default);
            stream.Pop();
            stream.Pop();

            Assert.True(stream.HitTest(new Point(30, 30)));
            Assert.False(stream.HitTest(new Point(5, 5)));
        }

        [Fact]
        public void Singular_Transform_Excludes_Its_Scope()
        {
            using var stream = new RenderDataStream();
            stream.PushTransform(new Matrix());
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 100, 100)), default);
            stream.Pop();

            Assert.False(stream.HitTest(new Point(50, 50)));
        }

        [Fact]
        public void Opacity_Push_Is_Transparent_To_Hit_Testing()
        {
            using var stream = new RenderDataStream();
            stream.PushOpacity(0.5);
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 100, 100)), default);
            stream.Pop();

            Assert.True(stream.HitTest(new Point(50, 50)));
        }

        [Fact]
        public void Scope_State_Is_Restored_After_Pop()
        {
            using var stream = new RenderDataStream();
            stream.PushTransform(Matrix.CreateTranslation(1000, 1000));
            stream.Pop();
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 10, 10)), default);

            Assert.True(stream.HitTest(new Point(5, 5)));
        }

        [Fact]
        public void Hit_Test_Handles_Deeply_Nested_Scopes()
        {
            using var stream = new RenderDataStream();
            for (var i = 0; i < 100; i++)
                stream.PushOpacity(0.5);
            stream.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 10, 10)), default);
            for (var i = 0; i < 100; i++)
                stream.Pop();

            Assert.True(stream.HitTest(new Point(5, 5)));
            Assert.False(stream.HitTest(new Point(50, 50)));
        }
    }
}
