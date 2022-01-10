using System.Collections.Generic;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Avalonia.Controls.UnitTests
{
    internal class TestTemplatedControl : TemplatedControl
    {
        private List<IVisual> _visualChildren;

        public bool OnTemplateAppliedCalled { get; private set; }

        protected override int VisualChildrenCount => _visualChildren?.Count ?? base.VisualChildrenCount;

        public new void AddVisualChild(IVisual visual)
        {
            _visualChildren ??= new List<IVisual>(this.GetVisualChildren());
            _visualChildren.Add(visual);
            base.AddVisualChild(visual);
        }

        protected override IVisual GetVisualChild(int index)
        {
            return _visualChildren?[index] ?? base.GetVisualChild(index);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            OnTemplateAppliedCalled = true;
        }
    }
}
