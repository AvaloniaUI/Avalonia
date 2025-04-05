using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Avalonia;

namespace ControlCatalog.Pages.Gamepad.Game
{
    /// <summary>
    /// Used for efficient collision-detection 
    /// </summary>
    public class QuadTreeNode
    {
        private readonly int _maxObjectsPerNode = 20;
        private readonly int _maxDepth = 20;
        private readonly Rect _bounds;

        private int _depth;
        private List<GameObjectBase> _objects = new();
        private QuadTreeNode[]? nodes; // null if not split yet
        private QuadTreeNode? parent;

        public QuadTreeNode(Rect bounds, int maxObjectsPerNode, int maxDepth, int depth)
        {
            _bounds = bounds;
            _maxObjectsPerNode = maxObjectsPerNode;
            _maxDepth = maxDepth;
            _depth = depth;
        }

        public int Depth => _depth;

        // Insert an object into the QuadTree
        public void Insert(GameObjectBase obj)
        {
            if (nodes != null)
            {
                int index = GetQuadrantIndex(obj.Hitbox);
                if (index != -1)
                {
                    nodes[index].Insert(obj);
                    return;
                }
            }

            _objects.Add(obj);
            obj.CurrentNode = this;

            if (_objects.Count > _maxObjectsPerNode && _depth < _maxDepth)
            {
                if (nodes == null)
                    Split();
                Debug.Assert(nodes is not null);
                int i = 0;
                while (i < _objects.Count)
                {
                    int index = GetQuadrantIndex(_objects[i].Hitbox);
                    if (index != -1)
                    {
                        // nodes is not null
                        nodes![index].Insert(_objects[i]);
                        _objects.RemoveAt(i);
                    }
                    else
                        i++;
                }
            }
        }

        // Remove an object (for dynamic objects when they move)
        public bool Remove(GameObjectBase obj)
        {
            obj.CurrentNode = null;
            if (_objects.Remove(obj))
                return true;

            if (nodes != null)
            {
                int index = GetQuadrantIndex(obj.Hitbox);
                if (index != -1)
                    return nodes[index].Remove(obj);
                else
                    for (int i = 0; i < 4; i++)
                        if (nodes[i].Remove(obj))
                            return true;
            }
            return false;
        }

        public void Upsert(GameObjectBase obj)
        {
            if (obj.CurrentNode == null)
            {
                Insert(obj); // Fallback to insert if not in tree yet
                return;
            }

            QuadTreeNode current = obj.CurrentNode;

            // Check if object still fits in its current node
            if (current._bounds.Contains(obj.Hitbox))
            {
                return; // No change needed
            }

            // Remove from current node
            current._objects.Remove(obj);

            // Move up to the first ancestor that contains the new hitbox
            QuadTreeNode node = current;
            while (node != null && !node._bounds.Contains(obj.Hitbox))
            {
                node = node?.parent ?? throw new Exception("Object hitbox is outside of global bounds!");
            }

            if (node == null)
            {
                // Object is outside the root bounds (unlikely in your case), re-insert from root
                obj.CurrentNode = null;
                Insert(obj);
                return;
            }

            // Move down to the correct leaf
            while (true)
            {
                if (node.nodes != null)
                {
                    int index = node.GetQuadrantIndex(obj.Hitbox);
                    if (index != -1)
                    {
                        node = node.nodes[index];
                        continue;
                    }
                }
                node._objects.Add(obj);
                obj.CurrentNode = node;
                break;
            }

            // Check if the new node needs splitting
            if (node._objects.Count > _maxObjectsPerNode && node._depth < _maxDepth)
            {
                node.Split();
                int i = 0;
                while (i < node._objects.Count)
                {
                    int index = node.GetQuadrantIndex(node._objects[i].Hitbox);
                    if (index != -1)
                    {
                        // nodes is never null after a split!
                        node.nodes![index].Insert(node._objects[i]);
                        node._objects.RemoveAt(i);
                    }
                    else
                        i++;
                }
            }
        }

        public IEnumerable<GameObjectBase> Retrieve(Rect area)
        {
            if (nodes != null)
            {
                int index = GetQuadrantIndex(area);
                if (index != -1)
                {
                    foreach (var obj in nodes[index].Retrieve(area))
                    {
                        yield return obj;
                    }
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        foreach (var obj in nodes[i].Retrieve(area))
                        {
                            yield return obj;
                        }
                    }
                }
            }

            foreach (var obj in _objects)
            {
                yield return obj;
            }
        }

        // Clear the QuadTree
        public void Clear()
        {
            _objects.Clear();
            if (nodes != null)
            {
                for (int i = 0; i < 4; i++)
                    nodes[i].Clear();
                nodes = null;
            }
        }

        // Split the node into four quadrants
        private void Split()
        {
            double subWidth = _bounds.Width / 2;
            double subHeight = _bounds.Height / 2;
            double x = _bounds.X;
            double y = _bounds.Y;

            nodes = new QuadTreeNode[4];
            nodes[0] = new QuadTreeNode(new Rect(x + subWidth, y, subWidth, subHeight), _maxObjectsPerNode, _maxDepth, _depth + 1); // NE
            nodes[1] = new QuadTreeNode(new Rect(x, y, subWidth, subHeight), _maxObjectsPerNode, _maxDepth, _depth + 1);             // NW
            nodes[2] = new QuadTreeNode(new Rect(x, y + subHeight, subWidth, subHeight), _maxObjectsPerNode, _maxDepth, _depth + 1); // SW
            nodes[3] = new QuadTreeNode(new Rect(x + subWidth, y + subHeight, subWidth, subHeight), _maxObjectsPerNode, _maxDepth, _depth + 1); // SE

            foreach (var node in nodes)
            {
                node.parent = this;
            }
        }

        // Determine which quadrant an object belongs to (-1 if it spans multiple)
        private int GetQuadrantIndex(Rect hitbox)
        {
            double midX = _bounds.X + _bounds.Width / 2;
            double midY = _bounds.Y + _bounds.Height / 2;

            bool topQuadrant = hitbox.Y + hitbox.Height <= midY;
            bool bottomQuadrant = hitbox.Y >= midY;
            bool leftQuadrant = hitbox.X + hitbox.Width <= midX;
            bool rightQuadrant = hitbox.X >= midX;

            if (leftQuadrant && topQuadrant)
                return 1; // NW
            if (rightQuadrant && topQuadrant)
                return 0; // NE
            if (leftQuadrant && bottomQuadrant)
                return 2; // SW
            if (rightQuadrant && bottomQuadrant)
                return 3; // SE
            return -1; // Object spans multiple quadrants
        }
    }
}
