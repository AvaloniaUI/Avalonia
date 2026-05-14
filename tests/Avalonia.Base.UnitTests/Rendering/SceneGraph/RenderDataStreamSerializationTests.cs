using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Transport;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.SceneGraph
{
    public class RenderDataStreamSerializationTests
    {
        [Fact]
        public void Round_Trip_Preserves_Bounds()
        {
            using var source = new RenderDataStream();
            source.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 10, 10)), default);
            source.PushTransform(Matrix.CreateTranslation(40, 40));
            source.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 10, 10)), default);
            source.Pop();

            using var result = RoundTrip(source);

            Assert.Equal(source.CalculateBounds(), result.CalculateBounds());
            Assert.Equal(new Rect(0, 0, 50, 50), result.CalculateBounds());
        }

        [Fact]
        public void Round_Trip_Preserves_Hit_Testing()
        {
            using var source = new RenderDataStream();
            source.PushClip(new RoundedRect(new Rect(0, 0, 10, 10)));
            source.DrawRectangle(Brushes.Black, null, null,
                new RoundedRect(new Rect(0, 0, 100, 100)), default);
            source.Pop();

            using var result = RoundTrip(source);

            Assert.True(result.HitTest(new Point(5, 5)));
            Assert.False(result.HitTest(new Point(50, 50)));
        }

        [Fact]
        public void Round_Trip_Preserves_Resource_References()
        {
            var brush = Mock.Of<IBrush>();
            using var source = new RenderDataStream();
            source.DrawRectangle(brush, null, null,
                new RoundedRect(new Rect(0, 0, 10, 10)), default);

            using var result = RoundTrip(source);

            var context = new Mock<IDrawingContextImpl>();
            result.Replay(context.Object);
            context.Verify(x => x.DrawRectangle(brush, null,
                It.IsAny<RoundedRect>(), It.IsAny<BoxShadows>()), Times.Once);
        }

        [Fact]
        public void Round_Trip_Of_Empty_Stream_Produces_Empty_Stream()
        {
            using var source = new RenderDataStream();
            using var result = RoundTrip(source);

            Assert.Null(result.CalculateBounds());
        }

        [Fact]
        public void Round_Trip_Spanning_Multiple_Stream_Segments()
        {
            using var source = new RenderDataStream();
            for (var i = 0; i < 50; i++)
                source.DrawRectangle(Brushes.Black, null, null,
                    new RoundedRect(new Rect(i, i, 1, 1)), default);

            using var result = RoundTrip(source);

            Assert.Equal(source.CalculateBounds(), result.CalculateBounds());
        }

        private static RenderDataStream RoundTrip(RenderDataStream source)
        {
            var data = new BatchStreamData();
            var memoryPool = new BatchStreamMemoryPool(false, 64, _ => { });
            var objectPool = new BatchStreamObjectPool<object?>(false, 8, _ => { });

            using (var writer = new BatchStreamWriter(data, memoryPool, objectPool))
                source.SerializeTo(writer);

            var result = new RenderDataStream();
            using (var reader = new BatchStreamReader(data, memoryPool, objectPool))
                result.DeserializeFrom(reader);

            return result;
        }
    }
}
