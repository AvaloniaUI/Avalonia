using System;
using Perspex.Styling;

namespace Perspex.Controls.UnitTests.Moonlight
{
    public class SilverlightTest
    {
        public Perspex.Controls.Panel TestPanel { get; } = new InternalPanel();

        public void CreateAsyncTest(IControl control, params Action[] actions)
        {
            var width = double.IsNaN(TestPanel.Width) ? double.PositiveInfinity : TestPanel.Width;
            var height = double.IsNaN(TestPanel.Height) ? double.PositiveInfinity : TestPanel.Height;

            foreach (var action in actions)
            {
                control.Measure(new Size(width, height));
                control.Arrange(new Rect(control.DesiredSize));
                action();
            }
        }

        private class InternalPanel : Panel, IStyleRoot
        {
        }
    }
}
