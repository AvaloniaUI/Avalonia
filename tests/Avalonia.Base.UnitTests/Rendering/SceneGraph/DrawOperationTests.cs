using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.SceneGraph
{  
    public class DrawOperationTests : ScopedTestBase
    {

        class TestContext
        {
            private readonly Compositor _compositor;
            public RenderDataDrawingContext Context { get; }

            public TestContext(CompositorTestServices services)
            {
                _compositor = services.Compositor;
                Context = new RenderDataDrawingContext(_compositor);
            }

            public void ForceRender()
            {
                _compositor.Commit();
                _compositor.Server.Render();
            }
            
            public Rect? GetBounds()
            {
                var renderData = Context.GetRenderResults();
                if (renderData == null)
                    return null;
                ForceRender();
                return renderData.Server.Bounds;
            }
        }

        private CompositorTestServices _services = new();
        public override void Dispose()
        {
            _services.Dispose();
            base.Dispose();
        }

        [Fact]
        public void Empty_Bounds_Remain_Empty()
        {
            var ctx = new TestContext(_services);
            ctx.Context.DrawRectangle(Brushes.Black, null, default);
            
            Assert.Null(ctx.GetBounds());
        }

        [Theory]
        [InlineData(10, 10, 10, 10, 1, 1, 1, 9, 9, 12, 12)]
        [InlineData(10, 10, 10, 10, 1, 1, 2, 9, 9, 12, 12)]
        [InlineData(10, 10, 10, 10, 1.5, 1.5, 1, 14, 14, 17, 17)]
        public void Rectangle_Bounds_Are_Snapped_To_Pixels(
            double x,
            double y,
            double width,
            double height,
            double scaleX,
            double scaleY,
            double penThickness,
            double expectedX,
            double expectedY,
            double expectedWidth,
            double expectedHeight)
        {
            var ctx = new TestContext(_services);
            using (ctx.Context.PushTransform(Matrix.CreateScale(scaleX, scaleY)))
                ctx.Context.DrawRectangle(null, new ImmutablePen(Brushes.Black, penThickness), new Rect(x, y, width, height));

            Assert.Equal(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), ctx.GetBounds().Value);
        }

        [Theory, InlineData(false), InlineData(true)]
        public void Image_Node_Releases_Reference_To_Bitmap_On_Dispose(bool disposeBeforeCommit)
        {
            var bitmap = RefCountable.Create(Mock.Of<IBitmapImpl>());

            var ctx = new TestContext(_services);
            ctx.Context.DrawBitmap(bitmap, 1, new Rect(1, 1, 1, 1), new Rect(1, 1, 1, 1));
            var renderData = ctx.Context.GetRenderResults()!;
            Assert.Equal(2, bitmap.RefCount);
            if (disposeBeforeCommit)
            {
                renderData.Dispose();
                Assert.Equal(1, bitmap.RefCount);
                ctx.ForceRender();
                Assert.Equal(1, bitmap.RefCount);
            }
            else
            {
                ctx.ForceRender();
                Assert.Equal(2, bitmap.RefCount);
                
                // Refs ownership is transferred to server-side render data 
                renderData.Dispose();
                Assert.Equal(2, bitmap.RefCount);
                
                ctx.ForceRender();
                Assert.Equal(1, bitmap.RefCount);
            }
        }

        [Fact]
        public void HitTest_On_Geometry_Node_With_Zero_Transform_Does_Not_Throw()
        {
            var ctx = new TestContext(_services);
            using (ctx.Context.PushTransform(new Matrix()))
                ctx.Context.DrawGeometry(Brushes.Black, null, Mock.Of<IGeometryImpl>());
            Assert.False(ctx.Context.GetRenderResults()!.HitTest(default));
        }

        [Fact]
        public void HitTest_RectangleNode_With_Transform_Hits()
        {
            var ctx = new TestContext(_services);
            using (ctx.Context.PushTransform(Matrix.CreateTranslation(20, 20)))
                ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));

            Assert.True(ctx.Context.GetRenderResults()!.HitTest(new Point(25, 25)));
        }
        
        [Fact]
        public void Empty_Push_Pop_Sequence_Produces_No_Results()
        {
            var ctx = new TestContext(_services);
            using (ctx.Context.PushTransform(Matrix.CreateTranslation(20, 20)))
            using (ctx.Context.PushOpacity(1))
            {

            }

            Assert.Null(ctx.Context.GetRenderResults());
        }
    }
}
