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
                _compositor.Server.Render(false);
            }
            
            public Rect? GetBounds()
            {
                var renderData = Context.GetRenderResults();
                if (renderData == null)
                    return null;
                ForceRender();
                return renderData.Server.Bounds?.ToRect();
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

            var bounds = Assert.NotNull(ctx.GetBounds());
            Assert.Equal(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), bounds);
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

        [Fact]
        public void HitTest_Through_PushTransform_Maps_Coordinates()
        {
            var ctx = new TestContext(_services);
            using (ctx.Context.PushTransform(Matrix.CreateTranslation(50, 50)))
                ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));

            var rd = ctx.Context.GetRenderResults()!;
            Assert.True(rd.HitTest(new Point(55, 55)));
            Assert.False(rd.HitTest(new Point(5, 5)));
        }

        [Fact]
        public void HitTest_Through_PushClip_Restricts_To_Clip_Region()
        {
            var ctx = new TestContext(_services);
            using (ctx.Context.PushClip(new Rect(0, 0, 10, 10)))
                ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 100, 100)));

            var rd = ctx.Context.GetRenderResults()!;
            Assert.True(rd.HitTest(new Point(5, 5)));
            Assert.False(rd.HitTest(new Point(50, 50)));
        }

        [Fact]
        public void HitTest_Through_Nested_Transforms_Composes_Outer_To_Inner()
        {
            var ctx = new TestContext(_services);
            using (ctx.Context.PushTransform(Matrix.CreateTranslation(20, 20)))
            using (ctx.Context.PushTransform(Matrix.CreateScale(2, 2)))
                ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));

            var rd = ctx.Context.GetRenderResults()!;
            Assert.True(rd.HitTest(new Point(30, 30)));
            Assert.False(rd.HitTest(new Point(5, 5)));
            Assert.False(rd.HitTest(new Point(45, 45)));
        }

        [Fact]
        public void Bounds_Union_Across_Multiple_Top_Level_Draws()
        {
            var ctx = new TestContext(_services);
            ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));
            ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(20, 20, 10, 10)));

            Assert.Equal(new Rect(0, 0, 30, 30), ctx.GetBounds());
        }

        [Fact]
        public void Bounds_Reflect_PushTransform_Translation()
        {
            var ctx = new TestContext(_services);
            using (ctx.Context.PushTransform(Matrix.CreateTranslation(50, 50)))
                ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));

            Assert.Equal(new Rect(50, 50, 10, 10), ctx.GetBounds());
        }

        [Fact]
        public void Identity_PushTransform_Does_Not_Wrap_Children()
        {
            var ctx = new TestContext(_services);
            using (ctx.Context.PushTransform(Matrix.Identity))
                ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));

            var rd = ctx.Context.GetRenderResults()!;
            ctx.ForceRender();
            Assert.Equal(new Rect(0, 0, 10, 10), rd.Server.Bounds?.ToRect());
            Assert.True(rd.HitTest(new Point(5, 5)));
        }

        [Fact]
        public void Opacity_One_Push_Does_Not_Wrap_Children()
        {
            var ctx = new TestContext(_services);
            using (ctx.Context.PushOpacity(1))
                ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));

            var rd = ctx.Context.GetRenderResults()!;
            ctx.ForceRender();
            Assert.Equal(new Rect(0, 0, 10, 10), rd.Server.Bounds?.ToRect());
            Assert.True(rd.HitTest(new Point(5, 5)));
        }

        [Fact]
        public void Empty_Push_Between_Real_Draws_Does_Not_Affect_Siblings()
        {
            var ctx = new TestContext(_services);
            ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));
            using (ctx.Context.PushTransform(Matrix.CreateTranslation(100, 100)))
            {
            }
            ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(40, 40, 10, 10)));

            var rd = ctx.Context.GetRenderResults()!;
            ctx.ForceRender();
            Assert.Equal(new Rect(0, 0, 50, 50), rd.Server.Bounds?.ToRect());
            Assert.True(rd.HitTest(new Point(5, 5)));
            Assert.True(rd.HitTest(new Point(45, 45)));
            Assert.False(rd.HitTest(new Point(25, 25)));
        }

        [Fact]
        public void NoOp_Inner_Push_Inside_Real_Outer_Push_Is_Stripped()
        {
            var ctx = new TestContext(_services);
            using (ctx.Context.PushTransform(Matrix.CreateTranslation(20, 20)))
            {
                using (ctx.Context.PushOpacity(1))
                {
                }
                ctx.Context.DrawRectangle(Brushes.Black, null, new RoundedRect(new Rect(0, 0, 10, 10)));
            }

            var rd = ctx.Context.GetRenderResults()!;
            ctx.ForceRender();
            Assert.Equal(new Rect(20, 20, 10, 10), rd.Server.Bounds?.ToRect());
            Assert.True(rd.HitTest(new Point(25, 25)));
            Assert.False(rd.HitTest(new Point(5, 5)));
        }

        [Fact]
        public void Non_Immutable_Brush_Is_AddRefed_Once_And_Released_On_Dispose()
        {
            var brush = new TrackingBrush();
            var ctx = new TestContext(_services);
            ctx.Context.DrawRectangle(brush, null, new RoundedRect(new Rect(0, 0, 10, 10)));
            var rd = ctx.Context.GetRenderResults()!;

            Assert.Equal(1, brush.AddRefCount);
            Assert.Equal(0, brush.ReleaseCount);

            rd.Dispose();
            Assert.Equal(1, brush.AddRefCount);
            Assert.Equal(1, brush.ReleaseCount);
        }

        [Fact]
        public void Non_Immutable_Brush_Used_Multiple_Times_Is_AddRefed_Once()
        {
            var brush = new TrackingBrush();
            var ctx = new TestContext(_services);
            ctx.Context.DrawRectangle(brush, null, new RoundedRect(new Rect(0, 0, 10, 10)));
            ctx.Context.DrawRectangle(brush, null, new RoundedRect(new Rect(20, 20, 10, 10)));
            ctx.Context.DrawEllipse(brush, null, new Rect(40, 40, 10, 10));
            var rd = ctx.Context.GetRenderResults()!;

            Assert.Equal(1, brush.AddRefCount);

            rd.Dispose();
            Assert.Equal(1, brush.ReleaseCount);
        }

        [Fact]
        public void Non_Immutable_Pen_Is_AddRefed_Once_And_Released_On_Dispose()
        {
            var pen = new TrackingPen();
            var ctx = new TestContext(_services);
            ctx.Context.DrawLine(pen, new Point(0, 0), new Point(10, 10));
            var rd = ctx.Context.GetRenderResults()!;

            Assert.Equal(1, pen.AddRefCount);
            Assert.Equal(0, pen.ReleaseCount);

            rd.Dispose();
            Assert.Equal(1, pen.ReleaseCount);
        }

        [Fact]
        public void Immutable_Brush_Is_Not_Registered_As_Server_Resource()
        {
            var brush = new ImmutableTrackingBrush();
            var ctx = new TestContext(_services);
            ctx.Context.DrawRectangle(brush, null, new RoundedRect(new Rect(0, 0, 10, 10)));
            var rd = ctx.Context.GetRenderResults()!;

            Assert.Equal(0, brush.AddRefCount);

            rd.Dispose();
        }

        [Fact]
        public void Immutable_Pen_Is_Not_Registered_As_Server_Resource()
        {
            var pen = new ImmutableTrackingPen();
            var ctx = new TestContext(_services);
            ctx.Context.DrawLine(pen, new Point(0, 0), new Point(10, 10));
            var rd = ctx.Context.GetRenderResults()!;

            Assert.Equal(0, pen.AddRefCount);

            rd.Dispose();
        }

        [Fact]
        public void Custom_Draw_Operation_Disposed_When_RenderData_Disposed_Before_Commit()
        {
            var op = new TrackingCustomOp();
            var ctx = new TestContext(_services);
            ctx.Context.Custom(op);
            var rd = ctx.Context.GetRenderResults()!;

            Assert.Equal(0, op.DisposeCount);

            rd.Dispose();
            Assert.Equal(1, op.DisposeCount);
        }

        sealed class TrackingBrush : IBrush, ICompositionRenderResource<IBrush>
        {
            public int AddRefCount;
            public int ReleaseCount;
            public double Opacity => 1;
            public ITransform? Transform => null;
            public RelativePoint TransformOrigin => default;
            public void AddRefOnCompositor(Compositor c) => AddRefCount++;
            public void ReleaseOnCompositor(Compositor c) => ReleaseCount++;
            public IBrush GetForCompositor(Compositor c) => this;
        }

        sealed class TrackingPen : IPen, ICompositionRenderResource<IPen>
        {
            public int AddRefCount;
            public int ReleaseCount;
            public IBrush? Brush => Brushes.Black;
            public IDashStyle? DashStyle => null;
            public PenLineCap LineCap => PenLineCap.Flat;
            public PenLineJoin LineJoin => PenLineJoin.Miter;
            public double MiterLimit => 10;
            public double Thickness => 1;
            public void AddRefOnCompositor(Compositor c) => AddRefCount++;
            public void ReleaseOnCompositor(Compositor c) => ReleaseCount++;
            public IPen GetForCompositor(Compositor c) => this;
        }

        sealed class ImmutableTrackingBrush : IImmutableBrush, ICompositionRenderResource<IBrush>
        {
            public int AddRefCount;
            public double Opacity => 1;
            public ITransform? Transform => null;
            public RelativePoint TransformOrigin => default;
            public void AddRefOnCompositor(Compositor c) => AddRefCount++;
            public void ReleaseOnCompositor(Compositor c) { }
            public IBrush GetForCompositor(Compositor c) => this;
        }

        sealed class ImmutableTrackingPen : ImmutablePen, ICompositionRenderResource<IPen>
        {
            public int AddRefCount;
            public ImmutableTrackingPen() : base(Brushes.Black, 1) { }
            public void AddRefOnCompositor(Compositor c) => AddRefCount++;
            public void ReleaseOnCompositor(Compositor c) { }
            public IPen GetForCompositor(Compositor c) => this;
        }

        sealed class TrackingCustomOp : ICustomDrawOperation
        {
            public int DisposeCount;
            public Rect Bounds => new Rect(0, 0, 10, 10);
            public bool HitTest(Point p) => false;
            public void Render(ImmediateDrawingContext context) { }
            public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
            public void Dispose() => DisposeCount++;
        }
    }
}
