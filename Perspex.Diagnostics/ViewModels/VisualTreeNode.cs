using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace Perspex.Diagnostics.ViewModels
{
    internal class VisualTreeNode : ReactiveObject
    {
        public VisualTreeNode(IVisual visual)
        {
            this.Children = visual.VisualChildren.CreateDerivedCollection(x => new VisualTreeNode(x));
            this.Type = visual.GetType().Name;
        }

        public IReactiveDerivedList<VisualTreeNode> Children { get; private set; }

        public string Type { get; private set; }
    }
}
