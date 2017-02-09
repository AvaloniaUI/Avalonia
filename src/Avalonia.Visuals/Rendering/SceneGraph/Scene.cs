// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    public class Scene
    {
        private Dictionary<IVisual, IVisualNode> _index;

        public Scene(IVisual rootVisual)
            : this(
                new VisualNode(rootVisual, null),
                new Dictionary<IVisual, IVisualNode>(),
                new SceneLayers(rootVisual),
                0)
        {
            _index.Add(rootVisual, Root);
        }

        internal Scene(VisualNode root, Dictionary<IVisual, IVisualNode> index, SceneLayers layers, int id)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            var renderRoot = root.Visual as IRenderRoot;

            _index = index;
            Root = root;
            Layers = layers;
            Id = id;
            root.LayerRoot = root.Visual;
        }

        public int Id { get; }
        public SceneLayers Layers { get; }
        public IVisualNode Root { get; }
        public Size Size { get; set; }
        public double Scaling { get; set; } = 1;

        public void Add(IVisualNode node)
        {
            Contract.Requires<ArgumentNullException>(node != null);

            _index.Add(node.Visual, node);
        }

        public Scene Clone()
        {
            var index = new Dictionary<IVisual, IVisualNode>();
            var root = Clone((VisualNode)Root, null, index);

            var result = new Scene(root, index, Layers.Clone(), Id + 1)
            {
                Size = Size,
                Scaling = Scaling,
            };

            return result;
        }

        public IVisualNode FindNode(IVisual visual)
        {
            IVisualNode node;
            _index.TryGetValue(visual, out node);
            return node;
        }

        public IEnumerable<IVisual> HitTest(Point p, Func<IVisual, bool> filter)
        {
            return HitTest(Root, p, null, filter);
        }

        public void Remove(IVisualNode node)
        {
            Contract.Requires<ArgumentNullException>(node != null);

            _index.Remove(node.Visual);
        }

        private VisualNode Clone(VisualNode source, IVisualNode parent, Dictionary<IVisual, IVisualNode> index)
        {
            var result = source.Clone(parent);

            index.Add(result.Visual, result);

            foreach (var child in source.Children)
            {
                result.AddChild(Clone((VisualNode)child, result, index));
            }

            return result;
        }

        private IEnumerable<IVisual> HitTest(IVisualNode node, Point p, Rect? clip, Func<IVisual, bool> filter)
        {
            if (filter?.Invoke(node.Visual) != false)
            {
                var clipped = false;

                if (node.ClipToBounds)
                {
                    clip = clip == null ? node.ClipBounds : clip.Value.Intersect(node.ClipBounds);
                    clipped = !clip.Value.Contains(p);
                }

                if (node.GeometryClip != null)
                {
                    var controlPoint = Root.Visual.TranslatePoint(p, node.Visual);
                    clipped = !node.GeometryClip.FillContains(p);
                }

                if (!clipped)
                {
                    for (var i = node.Children.Count - 1; i >= 0; --i)
                    {
                        foreach (var h in HitTest(node.Children[i], p, clip, filter))
                        {
                            yield return h;
                        }
                    }

                    if (node.HitTest(p))
                    {
                        yield return node.Visual;
                    }
                }
            }
        }
    }
}
