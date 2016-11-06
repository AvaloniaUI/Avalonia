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
            : this(new VisualNode(rootVisual), new Dictionary<IVisual, IVisualNode>())
        {
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

        private VisualNode Clone(VisualNode source, ISceneNode parent, Dictionary<IVisual, IVisualNode> index)
        {
            var result = source.Clone();

            index.Add(result.Visual, result);

            foreach (var child in source.Children)
            {
                var visualNode = child as VisualNode;

                if (visualNode != null)
                {
                    result.Children.Add(Clone(visualNode, result, index));
                }
                else
                {
                    result.Children.Add(child);
                }
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
                        var visualChild = node.Children[i] as IVisualNode;

                        if (visualChild != null)
                        {
                            foreach (var h in HitTest(visualChild, p, clip, filter))
                            {
                                yield return h;
                            }
                        }
                    }

                    dynamic d = node.Visual;

                    if (node.HitTest(p))
                    {
                        yield return node.Visual;
                    }
                }
            }
        }
    }
}
