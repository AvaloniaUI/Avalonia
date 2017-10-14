using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Rendering
{
    public class DeferredRendererTests
    {
        [Fact]
        public void First_Frame_Calls_UpdateScene_On_Dispatcher()
        {
            var loop = new Mock<IRenderLoop>();
            var root = new TestRoot();

            var dispatcher = new Mock<IDispatcher>();
            dispatcher.Setup(x => x.InvokeAsync(It.IsAny<Action>(), DispatcherPriority.Render))
                .Callback<Action, DispatcherPriority>((a, p) => a());

            var target = new DeferredRenderer(
                root,
                loop.Object,
                sceneBuilder: MockSceneBuilder(root).Object,
                dispatcher: dispatcher.Object);

            target.Start();
            RunFrame(loop);

#if !NETCOREAPP1_1 // Delegate.Method is not available in netcoreapp1.1
            dispatcher.Verify(x => 
                x.InvokeAsync(
                    It.Is<Action>(a => a.Method.Name == "UpdateScene"),
                    DispatcherPriority.Render));
#endif
        }

        [Fact]
        public void First_Frame_Calls_SceneBuilder_UpdateAll()
        {
            var loop = new Mock<IRenderLoop>();
            var root = new TestRoot();
            var sceneBuilder = MockSceneBuilder(root);
            var dispatcher = new ImmediateDispatcher();
            var target = new DeferredRenderer(
                root,
                loop.Object,
                sceneBuilder: sceneBuilder.Object,
                dispatcher: dispatcher);

            target.Start();
            RunFrame(loop);

            sceneBuilder.Verify(x => x.UpdateAll(It.IsAny<Scene>()));
        }

        [Fact]
        public void Frame_Does_Not_Call_SceneBuilder_If_No_Dirty_Controls()
        {
            var loop = new Mock<IRenderLoop>();
            var root = new TestRoot();
            var sceneBuilder = MockSceneBuilder(root);
            var dispatcher = new ImmediateDispatcher();
            var target = new DeferredRenderer(
                root,
                loop.Object,
                sceneBuilder: sceneBuilder.Object,
                dispatcher: dispatcher);

            target.Start();
            IgnoreFirstFrame(loop, sceneBuilder);
            RunFrame(loop);

            sceneBuilder.Verify(x => x.UpdateAll(It.IsAny<Scene>()), Times.Never);
            sceneBuilder.Verify(x => x.Update(It.IsAny<Scene>(), It.IsAny<Visual>()), Times.Never);
        }

        [Fact]
        public void Should_Update_Dirty_Controls_In_Order()
        {
            var loop = new Mock<IRenderLoop>();
            var dispatcher = new ImmediateDispatcher();

            Border border;
            Decorator decorator;
            Canvas canvas;
            var root = new TestRoot
            {
                Child = decorator = new Decorator
                {
                    Child = border = new Border
                    {
                        Child = canvas = new Canvas()
                    }
                }
            };

            var sceneBuilder = MockSceneBuilder(root);
            var target = new DeferredRenderer(
                root,
                loop.Object,
                sceneBuilder: sceneBuilder.Object,
                dispatcher: dispatcher);

            target.Start();
            IgnoreFirstFrame(loop, sceneBuilder);
            target.AddDirty(border);
            target.AddDirty(canvas);
            target.AddDirty(root);
            target.AddDirty(decorator);

            var result = new List<IVisual>();
            sceneBuilder.Setup(x => x.Update(It.IsAny<Scene>(), It.IsAny<IVisual>()))
                .Callback<Scene, IVisual>((_, v) => result.Add(v));

            RunFrame(loop);

            Assert.Equal(new List<IVisual> { root, decorator, border, canvas }, result);
        }

        [Fact]
        public void Frame_Should_Create_Layer_For_Root()
        {
            var loop = new Mock<IRenderLoop>();
            var root = new TestRoot();
            var rootLayer = new Mock<IRenderTargetBitmapImpl>();
            var dispatcher = new ImmediateDispatcher();

            var sceneBuilder = new Mock<ISceneBuilder>();
            sceneBuilder.Setup(x => x.UpdateAll(It.IsAny<Scene>()))
                .Callback<Scene>(scene =>
                {
                    scene.Size = root.ClientSize;
                    scene.Layers.Add(root).Dirty.Add(new Rect(root.ClientSize));
                });

            var renderInterface = new Mock<IPlatformRenderInterface>();

            var target = new DeferredRenderer(
                root,
                loop.Object,
                sceneBuilder: sceneBuilder.Object,
                //layerFactory: layers.Object,
                dispatcher: dispatcher);

            target.Start();
            RunFrame(loop);

            var context = Mock.Get(root.CreateRenderTarget().CreateDrawingContext(null));
            context.Verify(x => x.CreateLayer(root.ClientSize));
        }

        [Fact]
        public void Should_Create_And_Delete_Layers_For_Transparent_Controls()
        {
            Border border;
            var root = new TestRoot
            {
                Width = 100,
                Height = 100,
                Child = new Border
                {
                    Background = Brushes.Red,
                    Child = border = new Border
                    {
                        Background = Brushes.Green,
                    }
                }
            };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            var rootLayer = CreateLayer();
            var borderLayer = CreateLayer();
            var renderTargetContext = Mock.Get(root.CreateRenderTarget().CreateDrawingContext(null));
            renderTargetContext.SetupSequence(x => x.CreateLayer(It.IsAny<Size>()))
                .Returns(rootLayer)
                .Returns(borderLayer);

            var loop = new Mock<IRenderLoop>();
            var target = new DeferredRenderer(
                root,
                loop.Object,
                dispatcher: new ImmediateDispatcher());
            root.Renderer = target;

            target.Start();
            RunFrame(loop);

            var rootContext = Mock.Get(rootLayer.CreateDrawingContext(null));
            var borderContext = Mock.Get(borderLayer.CreateDrawingContext(null));

            rootContext.Verify(x => x.FillRectangle(Brushes.Red, new Rect(0, 0, 100, 100), 0), Times.Once);
            rootContext.Verify(x => x.FillRectangle(Brushes.Green, new Rect(0, 0, 100, 100), 0), Times.Once);
            borderContext.Verify(x => x.FillRectangle(It.IsAny<IBrush>(), It.IsAny<Rect>(), It.IsAny<float>()), Times.Never);

            rootContext.ResetCalls();
            borderContext.ResetCalls();
            border.Opacity = 0.5;
            RunFrame(loop);

            rootContext.Verify(x => x.FillRectangle(Brushes.Red, new Rect(0, 0, 100, 100), 0), Times.Once);
            rootContext.Verify(x => x.FillRectangle(Brushes.Green, new Rect(0, 0, 100, 100), 0), Times.Never);
            borderContext.Verify(x => x.FillRectangle(Brushes.Green, new Rect(0, 0, 100, 100), 0), Times.Once);

            rootContext.ResetCalls();
            borderContext.ResetCalls();
            border.Opacity = 1;
            RunFrame(loop);

            Mock.Get(borderLayer).Verify(x => x.Dispose());
            rootContext.Verify(x => x.FillRectangle(Brushes.Red, new Rect(0, 0, 100, 100), 0), Times.Once);
            rootContext.Verify(x => x.FillRectangle(Brushes.Green, new Rect(0, 0, 100, 100), 0), Times.Once);
            borderContext.Verify(x => x.FillRectangle(It.IsAny<IBrush>(), It.IsAny<Rect>(), It.IsAny<float>()), Times.Never);
        }

        private void IgnoreFirstFrame(Mock<IRenderLoop> loop, Mock<ISceneBuilder> sceneBuilder)
        {
            RunFrame(loop);
            sceneBuilder.ResetCalls();
        }

        private void RunFrame(Mock<IRenderLoop> loop)
        {
            loop.Raise(x => x.Tick += null, EventArgs.Empty);
        }

        private IRenderTargetBitmapImpl CreateLayer()
        {
            return Mock.Of<IRenderTargetBitmapImpl>(x =>
                x.CreateDrawingContext(It.IsAny<IVisualBrushRenderer>()) == Mock.Of<IDrawingContextImpl>());
        }

        private Mock<ISceneBuilder> MockSceneBuilder(IRenderRoot root)
        {
            var result = new Mock<ISceneBuilder>();
            result.Setup(x => x.UpdateAll(It.IsAny<Scene>()))
                .Callback<Scene>(x => x.Layers.Add(root).Dirty.Add(new Rect(root.ClientSize)));
            return result;
        }
    }
}
