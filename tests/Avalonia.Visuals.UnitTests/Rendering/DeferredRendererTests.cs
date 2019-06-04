﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.Visuals.Media.Imaging;
using Avalonia.VisualTree;

using Moq;

using Xunit;

namespace Avalonia.Visuals.UnitTests.Rendering
{
    public class DeferredRendererTests
    {
        [Fact]
        public void First_Frame_Calls_SceneBuilder_UpdateAll()
        {
            var root = new TestRoot();
            var sceneBuilder = MockSceneBuilder(root);

            CreateTargetAndRunFrame(root, sceneBuilder: sceneBuilder.Object);

            sceneBuilder.Verify(x => x.UpdateAll(It.IsAny<Scene>()));
        }

        [Fact]
        public void Frame_Does_Not_Call_SceneBuilder_If_No_Dirty_Controls()
        {
            var dispatcher = new ImmediateDispatcher();
            var loop = new Mock<IRenderLoop>();
            var root = new TestRoot();
            var sceneBuilder = MockSceneBuilder(root);
            var target = new DeferredRenderer(
                root,
                loop.Object,
                sceneBuilder: sceneBuilder.Object);

            target.Start();
            IgnoreFirstFrame(target, sceneBuilder);
            RunFrame(target);

            sceneBuilder.Verify(x => x.UpdateAll(It.IsAny<Scene>()), Times.Never);
            sceneBuilder.Verify(x => x.Update(It.IsAny<Scene>(), It.IsAny<Visual>()), Times.Never);
        }

        [Fact]
        public void Should_Update_Dirty_Controls_In_Order()
        {
            var dispatcher = new ImmediateDispatcher();
            var loop = new Mock<IRenderLoop>();

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
            IgnoreFirstFrame(target, sceneBuilder);
            target.AddDirty(border);
            target.AddDirty(canvas);
            target.AddDirty(root);
            target.AddDirty(decorator);

            var result = new List<IVisual>();
            sceneBuilder.Setup(x => x.Update(It.IsAny<Scene>(), It.IsAny<IVisual>()))
                .Callback<Scene, IVisual>((_, v) => result.Add(v));

            RunFrame(target);

            Assert.Equal(new List<IVisual> { root, decorator, border, canvas }, result);
        }

        [Fact]
        public void Should_Push_Opacity_For_Controls_With_Less_Than_1_Opacity()
        {
            var root = new TestRoot
            {
                Width = 100,
                Height = 100,
                Child = new Border
                {
                    Background = Brushes.Red,
                    Opacity = 0.5,
                }
            };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            var target = CreateTargetAndRunFrame(root);
            var context = GetLayerContext(target, root);
            var animation = new BehaviorSubject<double>(0.5);

            context.Verify(x => x.PushOpacity(0.5), Times.Once);
            context.Verify(x => x.FillRectangle(Brushes.Red, new Rect(0, 0, 100, 100), 0), Times.Once);
            context.Verify(x => x.PopOpacity(), Times.Once);
        }

        [Fact]
        public void Should_Not_Draw_Controls_With_0_Opacity()
        {
            var root = new TestRoot
            {
                Width = 100,
                Height = 100,
                Child = new Border
                {
                    Background = Brushes.Red,
                    Opacity = 0,
                    Child = new Border
                    {
                        Background = Brushes.Green,
                    }
                }
            };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            var target = CreateTargetAndRunFrame(root);
            var context = GetLayerContext(target, root);
            var animation = new BehaviorSubject<double>(0.5);

            context.Verify(x => x.PushOpacity(0.5), Times.Never);
            context.Verify(x => x.FillRectangle(Brushes.Red, new Rect(0, 0, 100, 100), 0), Times.Never);
            context.Verify(x => x.PopOpacity(), Times.Never);
        }

        [Fact]
        public void Should_Push_Opacity_Mask()
        {
            var root = new TestRoot
            {
                Width = 100,
                Height = 100,
                Child = new Border
                {
                    Background = Brushes.Red,
                    OpacityMask = Brushes.Green,
                }
            };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            var target = CreateTargetAndRunFrame(root);
            var context = GetLayerContext(target, root);
            var animation = new BehaviorSubject<double>(0.5);

            context.Verify(x => x.PushOpacityMask(Brushes.Green, new Rect(0, 0, 100, 100)), Times.Once);
            context.Verify(x => x.FillRectangle(Brushes.Red, new Rect(0, 0, 100, 100), 0), Times.Once);
            context.Verify(x => x.PopOpacityMask(), Times.Once);
        }

        [Fact]
        public void Should_Create_Layer_For_Root()
        {
            var root = new TestRoot();
            var rootLayer = new Mock<IRenderTargetBitmapImpl>();

            var sceneBuilder = new Mock<ISceneBuilder>();
            sceneBuilder.Setup(x => x.UpdateAll(It.IsAny<Scene>()))
                .Callback<Scene>(scene =>
                {
                    scene.Size = root.ClientSize;
                    scene.Layers.Add(root).Dirty.Add(new Rect(root.ClientSize));
                });

            var renderInterface = new Mock<IPlatformRenderInterface>();
            var target = CreateTargetAndRunFrame(root, sceneBuilder: sceneBuilder.Object);

            Assert.Single(target.Layers);
        }

        [Fact]
        public void Should_Create_And_Delete_Layers_For_Controls_With_Animated_Opacity()
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
                        Child = new Canvas(),
                        Opacity = 0.9,
                    }
                }
            };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            var timer = new Mock<IRenderTimer>();
            var target = CreateTargetAndRunFrame(root, timer);

            Assert.Equal(new[] { root }, target.Layers.Select(x => x.LayerRoot));

            var animation = new BehaviorSubject<double>(0.5);
            border.Bind(Border.OpacityProperty, animation, BindingPriority.Animation);
            RunFrame(target);

            Assert.Equal(new IVisual[] { root, border }, target.Layers.Select(x => x.LayerRoot));

            animation.OnCompleted();
            RunFrame(target);

            Assert.Equal(new[] { root }, target.Layers.Select(x => x.LayerRoot));
        }

        [Fact]
        public void Should_Not_Create_Layer_For_Childless_Control_With_Animated_Opacity()
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

            var animation = new BehaviorSubject<double>(0.5);
            border.Bind(Border.OpacityProperty, animation, BindingPriority.Animation);

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            var timer = new Mock<IRenderTimer>();
            var target = CreateTargetAndRunFrame(root, timer);

            Assert.Single(target.Layers);
        }

        [Fact]
        public void Should_Not_Push_Opacity_For_Transparent_Layer_Root_Control()
        {
            Border border;
            var root = new TestRoot
            {
                Width = 100,
                Height = 100,
                Child = border = new Border
                {
                    Background = Brushes.Red,
                    Child = new Canvas(),
                }
            };

            var animation = new BehaviorSubject<double>(0.5);
            border.Bind(Border.OpacityProperty, animation, BindingPriority.Animation);

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            var target = CreateTargetAndRunFrame(root);
            var context = GetLayerContext(target, border);

            context.Verify(x => x.PushOpacity(0.5), Times.Never);
            context.Verify(x => x.FillRectangle(Brushes.Red, new Rect(0, 0, 100, 100), 0), Times.Once);
            context.Verify(x => x.PopOpacity(), Times.Never);
        }

        [Fact]
        public void Should_Draw_Transparent_Layer_With_Correct_Opacity()
        {
            Border border;
            var root = new TestRoot
            {
                Width = 100,
                Height = 100,
                Child = border = new Border
                {
                    Background = Brushes.Red,
                    Child = new Canvas(),
                }
            };

            var animation = new BehaviorSubject<double>(0.5);
            border.Bind(Border.OpacityProperty, animation, BindingPriority.Animation);

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            var target = CreateTargetAndRunFrame(root);
            var context = Mock.Get(target.RenderTarget.CreateDrawingContext(null));
            var borderLayer = target.Layers[border].Bitmap;

            context.Verify(x => x.DrawImage(borderLayer, 0.5, It.IsAny<Rect>(), It.IsAny<Rect>(), BitmapInterpolationMode.Default));
        }

        [Fact]
        public void Can_Dirty_Control_In_SceneInvalidated()
        {
            Border border1;
            Border border2;
            var root = new TestRoot
            {
                Width = 100,
                Height = 100,
                Child = new StackPanel
                {
                    Children =
                    {
                        (border1 = new Border
                        {
                            Background = Brushes.Red,
                            Child = new Canvas(),
                        }),
                        (border2 = new Border
                        {
                            Background = Brushes.Red,
                            Child = new Canvas(),
                        }),
                    }
                }
            };

            root.Measure(Size.Infinity);
            root.Arrange(new Rect(root.DesiredSize));

            var target = CreateTargetAndRunFrame(root);
            var invalidated = false;

            target.SceneInvalidated += (s, e) =>
            {
                invalidated = true;
                target.AddDirty(border2);
            };

            target.AddDirty(border1);
            target.Paint(new Rect(root.DesiredSize));

            Assert.True(invalidated);
            Assert.True(((IRenderLoopTask)target).NeedsUpdate);
        }

        private DeferredRenderer CreateTargetAndRunFrame(
            TestRoot root,
            Mock<IRenderTimer> timer = null,
            ISceneBuilder sceneBuilder = null,
            IDispatcher dispatcher = null)
        {
            timer = timer ?? new Mock<IRenderTimer>();
            dispatcher = dispatcher ?? new ImmediateDispatcher();
            var target = new DeferredRenderer(
                root,
                new RenderLoop(timer.Object, dispatcher),
                sceneBuilder: sceneBuilder,
                dispatcher: dispatcher);
            root.Renderer = target;
            target.Start();
            RunFrame(target);
            return target;
        }

        private Mock<IDrawingContextImpl> GetLayerContext(DeferredRenderer renderer, IControl layerRoot)
        {
            return Mock.Get(renderer.Layers[layerRoot].Bitmap.Item.CreateDrawingContext(null));
        }

        private void IgnoreFirstFrame(IRenderLoopTask task, Mock<ISceneBuilder> sceneBuilder)
        {
            RunFrame(task);
            sceneBuilder.ResetCalls();
        }

        private void RunFrame(IRenderLoopTask task)
        {
            task.Update(TimeSpan.Zero);
            task.Render();
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
