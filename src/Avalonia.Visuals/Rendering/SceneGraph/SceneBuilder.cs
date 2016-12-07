// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    public class SceneBuilder : ISceneBuilder
    {
        public void UpdateAll(Scene scene, LayerDirtyRects dirty)
        {
            Contract.Requires<ArgumentNullException>(scene != null);
            Dispatcher.UIThread.VerifyAccess();

            using (var impl = new DeferredDrawingContextImpl(dirty))
            using (var context = new DrawingContext(impl))
            {
                Update(context, scene, (VisualNode)scene.Root, scene.Root.Visual.Bounds, true);
            }
        }

        public bool Update(Scene scene, IVisual visual, LayerDirtyRects dirty)
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
                            var clip = scene.Root.Visual.Bounds;

                            if (node.Parent != null)
                            {
                                context.PushPostTransform(node.Parent.Transform);
                                clip = node.Parent.ClipBounds;
                            }

                            Update(context, scene, node, clip, recurse);
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
                        ((VisualNode)node.Parent)?.RemoveChild(node);
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
                ((VisualNode)trim.Parent).RemoveChild(trim);
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

        private static void Update(DrawingContext context, Scene scene, VisualNode node, Rect clip, bool forceRecurse)
        {
            var visual = node.Visual;
            var opacity = visual.Opacity;
            var clipToBounds = visual.ClipToBounds;
            var bounds = new Rect(visual.Bounds.Size);
            var contextImpl = (DeferredDrawingContextImpl)context.PlatformImpl;

            contextImpl.Dirty.Add(node.LayerRoot, node.Bounds);

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

                using (contextImpl.BeginUpdate(node))
                using (context.PushPostTransform(m))
                using (context.PushTransformContainer())
                {
                    forceRecurse = forceRecurse || node.Transform != contextImpl.Transform;

                    node.Transform = contextImpl.Transform;
                    node.ClipBounds = (bounds * node.Transform).Intersect(clip);
                    node.ClipToBounds = clipToBounds;
                    node.GeometryClip = visual.Clip;
                    node.Opacity = opacity;
                    node.OpacityMask = visual.OpacityMask;

                    if (opacity < 1 && node.LayerRoot != visual)
                    {
                        SetLayer(node, node.Visual, contextImpl.Dirty);
                    }
                    else if (opacity >= 1 && node.LayerRoot == node.Visual && node.Parent != null)
                    {
                        ClearLayer(node, contextImpl.Dirty);
                    }

                    if (node.ClipToBounds)
                    {
                        clip = clip.Intersect(node.ClipBounds);
                    }

                    try
                    {
                        visual.Render(context);
                    }
                    catch { }

                    if (forceRecurse)
                    {
                        foreach (var child in visual.VisualChildren.OrderBy(x => x, ZIndexComparer.Instance))
                        {
                            var childNode = scene.FindNode(child) ?? CreateNode(scene, child, node);
                            Update(context, scene, (VisualNode)childNode, clip, forceRecurse);
                        }

                        node.SubTreeUpdated = true;
                        contextImpl.TrimChildren();
                    }
                }
            }
        }

        private static VisualNode CreateNode(Scene scene, IVisual visual, VisualNode parent)
        {
            var node = new VisualNode(visual, parent);
            node.LayerRoot = parent.LayerRoot;
            scene.Add(node);
            return node;
        }

        private static void Deindex(Scene scene, VisualNode node, LayerDirtyRects dirty)
        {
            scene.Remove(node);
            node.SubTreeUpdated = true;

            foreach (VisualNode child in node.Children)
            {
                var geometry = child as IDrawOperation;
                var visual = child as VisualNode;

                if (geometry != null)
                {
                    dirty.Add(child.LayerRoot, geometry.Bounds);
                }

                if (visual != null)
                {
                    Deindex(scene, visual, dirty);
                }
            }
        }

        private static void AddSubtreeBounds(VisualNode node, LayerDirtyRects dirty)
        {
            dirty.Add(node.LayerRoot, node.Bounds);

            foreach (VisualNode child in node.Children)
            {
                if (child.LayerRoot == node.LayerRoot)
                {
                    AddSubtreeBounds(child, dirty);
                }
            }
        }

        private static void ClearLayer(VisualNode node, LayerDirtyRects dirty)
        {
            var parent = (VisualNode)node.Parent;
            var newLayerRoot = parent.LayerRoot;
            var existingDirtyRects = dirty[node.LayerRoot];

            existingDirtyRects.Coalesce();

            foreach (var r in existingDirtyRects)
            {
                dirty.Add(newLayerRoot, r);
            }

            dirty.Remove(node.LayerRoot);

            SetLayer(node, newLayerRoot, dirty);
        }

        private static void SetLayer(VisualNode node, IVisual layerRoot, LayerDirtyRects dirty)
        {
            if (node.LayerRoot == layerRoot)
            {
                throw new AvaloniaInternalException("Called SetLayer with unchanged LayerRoot.");
            }

            var oldLayerRoot = node.LayerRoot;

            node.LayerRoot = layerRoot;
            dirty.Add(oldLayerRoot, node.Bounds);
            dirty.Add(layerRoot, node.Bounds);

            foreach (VisualNode child in node.Children)
            {
                // If the child is not the start of a new layer, recurse.
                if (child.LayerRoot != child.Visual)
                {
                    SetLayer(child, layerRoot, dirty);
                }
            }
        }
    }
}
