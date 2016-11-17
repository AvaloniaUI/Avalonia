// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections.Generic;

namespace Avalonia.Rendering.SceneGraph
{
    public static class SceneBuilder
    {
        public static void UpdateAll(Scene scene)
        {
            Contract.Requires<ArgumentNullException>(scene != null);
            Dispatcher.UIThread.VerifyAccess();

            using (var impl = new DeferredDrawingContextImpl())
            using (var context = new DrawingContext(impl))
            {
                Update(context, scene, (VisualNode)scene.Root, true);
            }
        }

        public static bool Update(Scene scene, IVisual visual)
        {
            var dirty = new DirtyRects();
            return Update(scene, visual, dirty);
        }

        public static bool Update(Scene scene, IVisual visual, DirtyRects dirty)
        {
            Contract.Requires<ArgumentNullException>(scene != null);
            Contract.Requires<ArgumentNullException>(visual != null);
            Contract.Requires<ArgumentNullException>(dirty != null);
            Dispatcher.UIThread.VerifyAccess();

            var node = (VisualNode)scene.FindNode(visual);

            if (visual.VisualRoot != null)
            {
                if (visual.IsVisible)
                {
                    // If the node isn't yet part of the scene, find the nearest ancestor that is.
                    node = node ?? FindExistingAncestor(scene, visual);

                    // We don't need to do anything if this part of the tree has already been fully
                    // updated.
                    if (node != null && !node.SubTreeUpdated)
                    {
                        // If the control we've been asked to update isn't part of the scene then
                        // we're carrying out an add operation, so recurse and add all the
                        // descendents too.
                        var recurse = node.Visual != visual;

                        using (var impl = new DeferredDrawingContextImpl(dirty))
                        using (var context = new DrawingContext(impl))
                        {
                            if (node.Parent != null)
                            {
                                context.PushPostTransform(node.Parent.Transform);
                            }

                            Update(context, scene, (VisualNode)node, recurse);
                        }

                        return true;
                    }
                }
                else
                {
                    if (node != null)
                    {
                        // The control has been removed so remove it from its parent and deindex the
                        // node and its descendents.
                        ((VisualNode)node.Parent)?.Children.Remove(node);
                        Deindex(scene, node, dirty);
                        return true;
                    }
                }
            }
            else if (node != null)
            {
                // The control has been removed so remove it from its parent and deindex the
                // node and its descendents.
                var trim = FindFirstDeadAncestor(scene, node);
                ((VisualNode)trim.Parent).Children.Remove(trim);
                Deindex(scene, trim, dirty);
                return true;
            }

            return false;
        }

        private static VisualNode FindExistingAncestor(Scene scene, IVisual visual)
        {
            var node = scene.FindNode(visual);

            while (node == null && visual.IsVisible)
            {
                visual = visual.VisualParent;
                node = scene.FindNode(visual);
            }

            return visual.IsVisible ? (VisualNode)node : null;
        }

        private static VisualNode FindFirstDeadAncestor(Scene scene, IVisualNode node)
        {
            var parent = node.Parent;

            while (parent.Visual.VisualRoot == null)
            {
                node = parent;
                parent = node.Parent;
            }

            return (VisualNode)node;
        }

        private static void Update(DrawingContext context, Scene scene, VisualNode node, bool forceRecurse)
        {
            var visual = node.Visual;
            var opacity = visual.Opacity;
            var clipToBounds = visual.ClipToBounds;
            var bounds = new Rect(visual.Bounds.Size);
            var contextImpl = (DeferredDrawingContextImpl)context.PlatformImpl;

            contextImpl.Dirty.Add(node.Bounds);

            if (visual.IsVisible)
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
                    forceRecurse = forceRecurse ||
                        node.Transform != contextImpl.Transform;

                    node.Transform = contextImpl.Transform;
                    node.ClipBounds = bounds * node.Transform;
                    node.ClipToBounds = clipToBounds;
                    node.GeometryClip = visual.Clip;
                    node.Opacity = opacity;
                    node.OpacityMask = visual.OpacityMask;

                    visual.Render(context);

                    if (forceRecurse)
                    {
                        foreach (var child in visual.VisualChildren.OrderBy(x => x, ZIndexComparer.Instance))
                        {
                            var childNode = scene.FindNode(child) ?? CreateNode(scene, child, node);
                            Update(context, scene, (VisualNode)childNode, forceRecurse);
                        }

                        node.SubTreeUpdated = true;
                        contextImpl.TrimNodes();
                    }
                }
            }
        }

        private static VisualNode CreateNode(Scene scene, IVisual visual, IVisualNode parent)
        {
            var node = new VisualNode(visual, parent);
            scene.Add(node);
            return node;
        }

        private static IList<Rect> Deindex(Scene scene, VisualNode node)
        {
            var dirty = new DirtyRects();
            Deindex(scene, node, dirty);
            return dirty.Coalesce();
        }

        private static void Deindex(Scene scene, VisualNode node, DirtyRects dirty)
        {
            scene.Remove(node);
            node.SubTreeUpdated = true;

            foreach (var child in node.Children)
            {
                var geometry = child as IGeometryNode;
                var visual = child as VisualNode;

                if (geometry != null)
                {
                    dirty.Add(geometry.Bounds);
                }

                if (visual != null)
                {
                    Deindex(scene, visual, dirty);
                }
            }
        }
    }
}
