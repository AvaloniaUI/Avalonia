/// Ported from https://github.com/HandyOrg/HandyControl/blob/master/src/Shared/HandyControl_Shared/Controls/Panel/RelativePanel.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Layout;

#nullable enable

namespace Avalonia.Controls
{
    public partial class RelativePanel : Panel
    {
        private readonly Graph _childGraph;

        public RelativePanel() => _childGraph = new Graph();

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var child in Children)
            {
                child?.Measure(availableSize);
            }

            return availableSize;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            _childGraph.Reset(arrangeSize);

            foreach (var child in Children.OfType<Layoutable>())
            {
                if (child == null)
                    continue;

                var node = _childGraph.AddNode(child);

                node.AlignLeftWithNode = _childGraph.AddLink(node, GetDependencyElement(AlignLeftWithProperty, child));
                node.AlignTopWithNode = _childGraph.AddLink(node, GetDependencyElement(AlignTopWithProperty, child));
                node.AlignRightWithNode = _childGraph.AddLink(node, GetDependencyElement(AlignRightWithProperty, child));
                node.AlignBottomWithNode = _childGraph.AddLink(node, GetDependencyElement(AlignBottomWithProperty, child));

                node.LeftOfNode = _childGraph.AddLink(node, GetDependencyElement(LeftOfProperty, child));
                node.AboveNode = _childGraph.AddLink(node, GetDependencyElement(AboveProperty, child));
                node.RightOfNode = _childGraph.AddLink(node, GetDependencyElement(RightOfProperty, child));
                node.BelowNode = _childGraph.AddLink(node, GetDependencyElement(BelowProperty, child));

                node.AlignHorizontalCenterWith = _childGraph.AddLink(node, GetDependencyElement(AlignHorizontalCenterWithProperty, child));
                node.AlignVerticalCenterWith = _childGraph.AddLink(node, GetDependencyElement(AlignVerticalCenterWithProperty, child));
            }

            if (_childGraph.CheckCyclic())
            {
                throw new Exception("RelativePanel error: Circular dependency detected. Layout could not complete.");
            }

            var size = new Size();

            foreach (var child in Children)
            {
                if (child.Bounds.Bottom > size.Height)
                {
                    size = size.WithHeight(child.Bounds.Bottom);
                }

                if (child.Bounds.Right > size.Width)
                {
                    size = size.WithWidth(child.Bounds.Right);
                }
            }

            if (VerticalAlignment == VerticalAlignment.Stretch)
            {
                size = size.WithHeight(arrangeSize.Height);
            }

            if (HorizontalAlignment == HorizontalAlignment.Stretch)
            {
                size = size.WithWidth(arrangeSize.Width);
            }

            return size;
        }

        private Layoutable? GetDependencyElement(AvaloniaProperty property, AvaloniaObject child)
        {
            var dependency = child.GetValue(property);

            if (dependency is Layoutable layoutable)
            {
                if (Children.Contains((ILayoutable)layoutable))
                    return layoutable;

                throw new ArgumentException($"RelativePanel error: Element does not exist in the current context: {property.Name}");
            }

            return null;
        }

        private class GraphNode
        {
            public Point Position { get; set; }

            public bool Arranged { get; set; }

            public Layoutable Element { get; }

            public HashSet<GraphNode> OutgoingNodes { get; }

            public GraphNode? AlignLeftWithNode { get; set; }

            public GraphNode? AlignTopWithNode { get; set; }

            public GraphNode? AlignRightWithNode { get; set; }

            public GraphNode? AlignBottomWithNode { get; set; }

            public GraphNode? LeftOfNode { get; set; }

            public GraphNode? AboveNode { get; set; }

            public GraphNode? RightOfNode { get; set; }

            public GraphNode? BelowNode { get; set; }

            public GraphNode? AlignHorizontalCenterWith { get; set; }

            public GraphNode? AlignVerticalCenterWith { get; set; }

            public GraphNode(Layoutable element)
            {
                OutgoingNodes = new HashSet<GraphNode>();
                Element = element;
            }
        }

        private class Graph
        {
            private readonly Dictionary<AvaloniaObject, GraphNode> _nodeDic;

            private Size _arrangeSize;

            public Graph()
            {
                _nodeDic = new Dictionary<AvaloniaObject, GraphNode>();
            }

            public GraphNode? AddLink(GraphNode from, Layoutable? to)
            {
                if (to == null)
                    return null;

                GraphNode nodeTo;
                if (_nodeDic.ContainsKey(to))
                {
                    nodeTo = _nodeDic[to];
                }
                else
                {
                    nodeTo = new GraphNode(to);
                    _nodeDic[to] = nodeTo;
                }

                from.OutgoingNodes.Add(nodeTo);
                return nodeTo;
            }

            public GraphNode AddNode(Layoutable value)
            {
                if (!_nodeDic.ContainsKey(value))
                {
                    var node = new GraphNode(value);
                    _nodeDic.Add(value, node);
                    return node;
                }

                return _nodeDic[value];
            }

            public void Reset(Size arrangeSize)
            {
                _arrangeSize = arrangeSize;
                _nodeDic.Clear();
            }

            public bool CheckCyclic() => CheckCyclic(_nodeDic.Values, null);

            private bool CheckCyclic(IEnumerable<GraphNode> nodes, HashSet<Layoutable>? set)
            {
                set ??= new HashSet<Layoutable>();

                foreach (var node in nodes)
                {
                    if (!node.Arranged && node.OutgoingNodes.Count == 0)
                    {
                        ArrangeChild(node, true);
                        continue;
                    }

                    if (node.OutgoingNodes.All(item => item.Arranged))
                    {
                        ArrangeChild(node);
                        continue;
                    }

                    if (!set.Add(node.Element))
                        return true;

                    return CheckCyclic(node.OutgoingNodes, set);
                }

                return false;
            }

            private void ArrangeChild(GraphNode node, bool ignoneSibling = false)
            {
                var child = node.Element;
                var childSize = child.DesiredSize;
                var childPos = new Point();

                if (GetAlignHorizontalCenterWithPanel(child))
                {
                    childPos = childPos.WithX((_arrangeSize.Width - childSize.Width) / 2);
                }

                if (GetAlignVerticalCenterWithPanel(child))
                {
                    childPos = childPos.WithY((_arrangeSize.Height - childSize.Height) / 2);
                }

                var alignLeftWithPanel = GetAlignLeftWithPanel(child);
                var alignTopWithPanel = GetAlignTopWithPanel(child);
                var alignRightWithPanel = GetAlignRightWithPanel(child);
                var alignBottomWithPanel = GetAlignBottomWithPanel(child);

                if (!ignoneSibling)
                {
                    if (node.LeftOfNode != null)
                    {
                        childPos = childPos.WithX(node.LeftOfNode.Position.X - childSize.Width);
                    }

                    if (node.AboveNode != null)
                    {
                        childPos = childPos.WithY(node.AboveNode.Position.Y - childSize.Height);
                    }

                    if (node.RightOfNode != null)
                    {
                        childPos = childPos.WithX(node.RightOfNode.Position.X + node.RightOfNode.Element.DesiredSize.Width);
                    }

                    if (node.BelowNode != null)
                    {
                        childPos = childPos.WithY(node.BelowNode.Position.Y + node.BelowNode.Element.DesiredSize.Height);
                    }

                    if (node.AlignHorizontalCenterWith != null)
                    {
                        childPos = childPos.WithX(node.AlignHorizontalCenterWith.Position.X +
                                     (node.AlignHorizontalCenterWith.Element.DesiredSize.Width - childSize.Width) / 2);
                    }

                    if (node.AlignVerticalCenterWith != null)
                    {
                        childPos = childPos.WithY(node.AlignVerticalCenterWith.Position.Y +
                                     (node.AlignVerticalCenterWith.Element.DesiredSize.Height - childSize.Height) / 2);
                    }

                    if (node.AlignLeftWithNode != null)
                    {
                        childPos = childPos.WithX(node.AlignLeftWithNode.Position.X);
                    }

                    if (node.AlignTopWithNode != null)
                    {
                        childPos = childPos.WithY(node.AlignTopWithNode.Position.Y);
                    }

                    if (node.AlignRightWithNode != null)
                    {
                        childPos = childPos.WithX(node.AlignRightWithNode.Element.DesiredSize.Width + node.AlignRightWithNode.Position.X - childSize.Width);
                    }

                    if (node.AlignBottomWithNode != null)
                    {
                        childPos = childPos.WithY(node.AlignBottomWithNode.Element.DesiredSize.Height + node.AlignBottomWithNode.Position.Y - childSize.Height);
                    }
                }

                if (alignLeftWithPanel)
                {
                    if (node.AlignRightWithNode != null)
                    {
                        childPos = childPos.WithX((node.AlignRightWithNode.Element.DesiredSize.Width + node.AlignRightWithNode.Position.X - childSize.Width) / 2);
                    }
                    else
                    {
                        childPos = childPos.WithX(0);
                    }
                }

                if (alignTopWithPanel)
                {
                    if (node.AlignBottomWithNode != null)
                    {
                        childPos = childPos.WithY((node.AlignBottomWithNode.Element.DesiredSize.Height + node.AlignBottomWithNode.Position.Y - childSize.Height) / 2);
                    }
                    else
                    {
                        childPos = childPos.WithY(0);
                    }
                }

                if (alignRightWithPanel)
                {
                    if (alignLeftWithPanel)
                    {
                        childPos = childPos.WithX((_arrangeSize.Width - childSize.Width) / 2);
                    }
                    else if (node.AlignLeftWithNode == null)
                    {
                        childPos = childPos.WithX(_arrangeSize.Width - childSize.Width);
                    }
                    else
                    {
                        childPos = childPos.WithX((_arrangeSize.Width + node.AlignLeftWithNode.Position.X - childSize.Width) / 2);
                    }
                }

                if (alignBottomWithPanel)
                {
                    if (alignTopWithPanel)
                    {
                        childPos = childPos.WithY((_arrangeSize.Height - childSize.Height) / 2);
                    }
                    else if (node.AlignTopWithNode == null)
                    {
                        childPos = childPos.WithY(_arrangeSize.Height - childSize.Height);
                    }
                    else
                    {
                        childPos = childPos.WithY((_arrangeSize.Height + node.AlignTopWithNode.Position.Y - childSize.Height) / 2);
                    }
                }

                child.Arrange(new Rect(childPos.X, childPos.Y, childSize.Width, childSize.Height));
                node.Position = childPos;
                node.Arranged = true;
            }
        }
    }
}
