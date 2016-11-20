// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    public class Scene
    {
        private Dictionary<IVisual, IVisualNode> _index;

        public Scene(IVisual rootVisual)
            : this(new VisualNode(rootVisual, null), new Dictionary<IVisual, IVisualNode>())
        {
            _index.Add(rootVisual, Root);
        }

        internal Scene(VisualNode root, Dictionary<IVisual, IVisualNode> index)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            _index = index;
            Root = root;
        }

        public IVisualNode Root { get; }

        public void Add(IVisualNode node)
        {
            Contract.Requires<ArgumentNullException>(node != null);

            _index.Add(node.Visual, node);
        }

        public Scene Clone()
        {
            var index = new Dictionary<IVisual, IVisualNode>();
            var root = (VisualNode)Clone((VisualNode)Root, null, index);
            var result = new Scene(root, index);
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
                if (node.ClipToBounds)
                {
                    // TODO: Handle geometry clip.
                    clip = clip == null ? node.ClipBounds : clip.Value.Intersect(node.ClipBounds);
                }

                if (!clip.HasValue || clip.Value.Contains(p))
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
