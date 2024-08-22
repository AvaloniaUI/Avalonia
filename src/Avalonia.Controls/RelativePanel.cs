// Ported from https://github.com/OrgEleCho/EleCho.WpfSuite/blob/master/EleCho.WpfSuite/Panels/RelativePanel.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines an area within which you can position and align child objects in relation to each other or the parent panel.
    /// </summary>
    public partial class RelativePanel : Panel
    {
        private readonly Graph _graph = new();

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            GenerateGraph();

            _graph.MeasureNodes(availableSize);

            var desiredSizeOfChildren = _graph.CalculateDesiredSize();

            return desiredSizeOfChildren;
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            _graph.ArrangeNodes(new Rect(0, 0, finalSize.Width, finalSize.Height));

            return finalSize;
        }

        private void GenerateGraph()
        {
            _graph.Nodes.Clear();

            foreach (Control child in Children)
            {
                _graph.Nodes.AddLast(new GraphNode(child));
            }

            _graph.ResolveConstraints();
        }
    }
}
