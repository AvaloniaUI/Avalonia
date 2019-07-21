// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Represents a scene graph used by the <see cref="DeferredRenderer"/>.
    /// </summary>
    public class Scene : IDisposable
    {
        private Dictionary<IVisual, IVisualNode> _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scene"/> class.
        /// </summary>
        /// <param name="rootVisual">The root visual to draw.</param>
        public Scene(IVisual rootVisual)
            : this(
                new VisualNode(rootVisual, null),
                new Dictionary<IVisual, IVisualNode>(),
                new SceneLayers(rootVisual),
                0)
        {
            _index.Add(rootVisual, Root);
        }

        private Scene(VisualNode root, Dictionary<IVisual, IVisualNode> index, SceneLayers layers, int generation)
        {
            Contract.Requires<ArgumentNullException>(root != null);

            var renderRoot = root.Visual as IRenderRoot;

            _index = index;
            Root = root;
            Layers = layers;
            Generation = generation;
            root.LayerRoot = root.Visual;
        }

        /// <summary>
        /// Gets a value identifying the scene's generation. This is incremented each time the scene is cloned.
        /// </summary>
        public int Generation { get; }

        /// <summary>
        /// Gets the layers for the scene.
        /// </summary>
        public SceneLayers Layers { get; }

        /// <summary>
        /// Gets the root node of the scene graph.
        /// </summary>
        public IVisualNode Root { get; }

        /// <summary>
        /// Gets or sets the size of the scene in device independent pixels.
        /// </summary>
        public Size Size { get; set; }

        /// <summary>
        /// Gets or sets the scene scaling.
        /// </summary>
        public double Scaling { get; set; } = 1;

        /// <summary>
        /// Adds a node to the scene index.
        /// </summary>
        /// <param name="node">The node.</param>
        public void Add(IVisualNode node)
        {
            Contract.Requires<ArgumentNullException>(node != null);

            _index.Add(node.Visual, node);
        }

        /// <summary>
        /// Clones the scene.
        /// </summary>
        /// <returns>The cloned scene.</returns>
        public Scene CloneScene()
        {
            var index = new Dictionary<IVisual, IVisualNode>();
            var root = Clone((VisualNode)Root, null, index);

            var result = new Scene(root, index, Layers.Clone(), Generation + 1)
            {
                Size = Size,
                Scaling = Scaling,
            };

            return result;
        }

        public void Dispose()
        {
            foreach (var node in _index.Values)
            {
                node.Dispose();
            }
        }

        /// <summary>
        /// Tries to find a node in the scene graph representing the specified visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>
        /// The node representing the visual or null if it could not be found.
        /// </returns>
        public IVisualNode FindNode(IVisual visual)
        {
            IVisualNode node;
            _index.TryGetValue(visual, out node);
            return node;
        }

        /// <summary>
        /// Gets the visuals at a point in the scene.
        /// </summary>
        /// <param name="p">The point.</param>
        /// <param name="root">The root of the subtree to search.</param>
        /// <param name="filter">A filter. May be null.</param>
        /// <returns>The visuals at the specified point.</returns>
        public IEnumerable<IVisual> HitTest(Point p, IVisual root, Func<IVisual, bool> filter)
        {
            var node = FindNode(root);
            return (node != null) ? HitTest(node, p, null, filter) : Enumerable.Empty<IVisual>();
        }

        /// <summary>
        /// Removes a node from the scene index.
        /// </summary>
        /// <param name="node">The node.</param>
        public void Remove(IVisualNode node)
        {
            Contract.Requires<ArgumentNullException>(node != null);

            _index.Remove(node.Visual);

            node.Dispose();
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
            if (filter?.Invoke(node.Visual) != false && node.Visual.IsAttachedToVisualTree)
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
                    clipped = !node.GeometryClip.FillContains(controlPoint.Value);
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
