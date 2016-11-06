// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    public static class SceneBuilder
    {
        public static Scene Update(Scene scene)
        {
            Dispatcher.UIThread.VerifyAccess();

            scene = scene.Clone();

            using (var impl = new DeferredDrawingContextImpl())
            using (var context = new DrawingContext(impl))
            {
                Update(context, scene, scene.Root.Visual, null);
            }

            return scene;
        }

        private static void Update(DrawingContext context, Scene scene, IVisual visual, VisualNode parent)
        {
            var opacity = visual.Opacity;
            var clipToBounds = visual.ClipToBounds;
            var bounds = new Rect(visual.Bounds.Size);
            var node = (VisualNode)scene.FindNode(visual) ?? CreateNode(visual, scene, parent);
            var contextImpl = (DeferredDrawingContextImpl)context.PlatformImpl;

            contextImpl.AddChild(node);

            if (visual.IsVisible && opacity > 0)
            {
                var m = Matrix.CreateTranslation(visual.Bounds.Position);

                var renderTransform = Matrix.Identity;

                if (visual.RenderTransform != null)
                {
                    var origin = visual.RenderTransformOrigin.ToPixels(new Size(visual.Bounds.Width, visual.Bounds.Height));
                    var offset = Matrix.CreateTranslation(origin);
                    renderTransform = (-offset) * visual.RenderTransform.Value * (offset);
                }

                m = renderTransform * m;

                using (contextImpl.Begin(node))
                using (context.PushPostTransform(m))
                using (context.PushTransformContainer())
                {
                    node.Transform = contextImpl.Transform;
                    node.ClipBounds = bounds * node.Transform;
                    node.ClipToBounds = clipToBounds;
                    node.GeometryClip = visual.Clip;
                    node.Opacity = opacity;
                    node.OpacityMask = visual.OpacityMask;

                    visual.Render(context);

                    foreach (var child in visual.VisualChildren.OrderBy(x => x, ZIndexComparer.Instance))
                    {
                        Update(context, scene, child, node);
                    }
                }
            }
        }

        private static VisualNode CreateNode(IVisual visual, Scene scene, VisualNode parent)
        {
            var node = new VisualNode(visual);
            scene.Add(node);
            return node;
        }
    }
}
