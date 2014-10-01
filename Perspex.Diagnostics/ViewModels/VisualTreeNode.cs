using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Controls;
using ReactiveUI;

namespace Perspex.Diagnostics.ViewModels
{
    internal class VisualTreeNode : ReactiveObject
    {
        public VisualTreeNode(IVisual visual)
        {
            this.Children = visual.VisualChildren.CreateDerivedCollection(x => new VisualTreeNode(x));
            this.Type = visual.GetType().Name;

            Control control = visual as Control;

            if (control != null)
            {
                this.IsInTemplate = control.TemplatedParent != null;
            }
        }

        public IReactiveDerivedList<VisualTreeNode> Children { get; private set; }

        public bool IsInTemplate { get; private set; }

        public string Type { get; private set; }
    }
}
