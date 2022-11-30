using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace Avalonia.Controls.UnitTests
{
    internal class TestTemplatedControl : TemplatedControl
    {
        public bool OnTemplateAppliedCalled { get; private set; }

        public void AddVisualChild(Visual visual)
        {
            VisualChildren.Add(visual);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            OnTemplateAppliedCalled = true;
        }
    }
}
