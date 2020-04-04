using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Avalonia.Controls.UnitTests
{
    internal class TestTemplatedControl : TemplatedControl
    {
        public bool OnTemplateAppliedCalled { get; private set; }

        public void AddVisualChild(IVisual visual)
        {
            VisualChildren.Add(visual);
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);
            OnTemplateAppliedCalled = true;
        }
    }
}
