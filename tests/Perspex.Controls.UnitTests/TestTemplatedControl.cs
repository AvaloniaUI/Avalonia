





namespace Perspex.Controls.UnitTests
{
    using Perspex.Controls.Primitives;

    internal class TestTemplatedControl : TemplatedControl
    {
        public bool OnTemplateAppliedCalled { get; private set; }

        public new void AddVisualChild(IVisual visual)
        {
            base.AddVisualChild(visual);
        }

        protected override void OnTemplateApplied()
        {
            this.OnTemplateAppliedCalled = true;
        }
    }
}
