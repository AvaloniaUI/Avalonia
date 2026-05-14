using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.SceneGraph
{
    public class RenderDataStreamTests
    {
        [Fact]
        public void Replay_Forwards_Line()
        {
            var pen = Mock.Of<IPen>();
            var context = new Mock<IDrawingContextImpl>();

            using var stream = new RenderDataStream();
            stream.DrawLine(pen, pen, new Point(1, 2), new Point(3, 4));
            stream.Replay(context.Object);

            context.Verify(x => x.DrawLine(pen, new Point(1, 2), new Point(3, 4)), Times.Once);
        }

        [Fact]
        public void Replay_Forwards_Rectangle_With_Box_Shadows()
        {
            var brush = Mock.Of<IBrush>();
            var pen = Mock.Of<IPen>();
            var rect = new RoundedRect(new Rect(0, 0, 10, 20));
            var shadows = new BoxShadows(
                new BoxShadow { Blur = 1 },
                new[] { new BoxShadow { Blur = 2 } });
            var context = new Mock<IDrawingContextImpl>();

            using var stream = new RenderDataStream();
            stream.DrawRectangle(brush, pen, pen, rect, shadows);
            stream.Replay(context.Object);

            context.Verify(x => x.DrawRectangle(brush, pen, rect,
                It.Is<BoxShadows>(s => s.Count == 2)), Times.Once);
        }

        [Fact]
        public void Replay_Forwards_Custom_Operation()
        {
            var operation = new Mock<ICustomDrawOperation>();
            var context = new Mock<IDrawingContextImpl>();

            using var stream = new RenderDataStream();
            stream.DrawCustom(operation.Object);
            stream.Replay(context.Object);

            operation.Verify(x => x.Render(It.IsAny<ImmediateDrawingContext>()), Times.Once);
        }

        [Fact]
        public void Replay_Pop_Dispatches_To_Matching_Pop_In_Lifo_Order()
        {
            var calls = new List<string>();
            var context = RecordingContext(calls);

            using var stream = new RenderDataStream();
            stream.PushClip(new RoundedRect(new Rect(0, 0, 10, 10)));
            stream.PushOpacity(0.5);
            stream.Pop();
            stream.Pop();
            stream.Replay(context.Object);

            Assert.Equal(new[] { "PushClip", "PushOpacity", "PopOpacity", "PopClip" }, calls);
        }

        [Fact]
        public void Replay_Applies_And_Restores_Transform()
        {
            var context = new Mock<IDrawingContextImpl>();
            context.SetupProperty(x => x.Transform);
            context.Object.Transform = Matrix.Identity;
            var matrix = Matrix.CreateTranslation(5, 7);

            using var stream = new RenderDataStream();
            stream.PushTransform(matrix);
            stream.Pop();

            stream.Replay(context.Object);

            Assert.Equal(Matrix.Identity, context.Object.Transform);
        }

        [Fact]
        public void Replay_Skips_Opacity_One_Push()
        {
            var calls = new List<string>();
            var context = RecordingContext(calls);

            using var stream = new RenderDataStream();
            stream.PushOpacity(1);
            stream.Pop();
            stream.Replay(context.Object);

            Assert.Empty(calls);
        }

        [Fact]
        public void Replay_Skips_Null_Geometry_Clip()
        {
            var calls = new List<string>();
            var context = RecordingContext(calls);

            using var stream = new RenderDataStream();
            stream.PushGeometryClip(null);
            stream.Pop();
            stream.Replay(context.Object);

            Assert.Empty(calls);
        }

        [Fact]
        public void Replay_Walks_Nested_Pushes_In_Order()
        {
            var calls = new List<string>();
            var context = RecordingContext(calls);

            using var stream = new RenderDataStream();
            stream.PushClip(new RoundedRect(new Rect(0, 0, 10, 10)));
            stream.PushGeometryClip(Mock.Of<IGeometryImpl>());
            stream.Pop();
            stream.Pop();
            stream.Replay(context.Object);

            Assert.Equal(
                new[] { "PushClip", "PushGeometryClip", "PopGeometryClip", "PopClip" },
                calls);
        }

        private static Mock<IDrawingContextImpl> RecordingContext(List<string> calls)
        {
            var context = new Mock<IDrawingContextImpl>();
            context.Setup(x => x.PushClip(It.IsAny<RoundedRect>())).Callback(() => calls.Add("PushClip"));
            context.Setup(x => x.PopClip()).Callback(() => calls.Add("PopClip"));
            context.Setup(x => x.PushGeometryClip(It.IsAny<IGeometryImpl>()))
                .Callback(() => calls.Add("PushGeometryClip"));
            context.Setup(x => x.PopGeometryClip()).Callback(() => calls.Add("PopGeometryClip"));
            context.Setup(x => x.PushOpacity(It.IsAny<double>(), It.IsAny<Rect?>()))
                .Callback(() => calls.Add("PushOpacity"));
            context.Setup(x => x.PopOpacity()).Callback(() => calls.Add("PopOpacity"));
            return context;
        }
    }
}
