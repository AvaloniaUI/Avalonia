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

        public IVisualNode FindNode(IVisual visual)
        {
            IVisualNode node;
            _index.TryGetValue(visual, out node);
            return node;
        }

        public Scene Clone()
        {
            var index = new Dictionary<IVisual, IVisualNode>();
            var root = (VisualNode)Clone((VisualNode)Root, null, index);
            var result = new Scene(root, index);
            return result;
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
    }
}
