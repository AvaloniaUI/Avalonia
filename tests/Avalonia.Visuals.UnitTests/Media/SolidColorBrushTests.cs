using System;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class SolidColorBrushTests
    {
        [Fact]
        public void Changing_Color_Raises_Invalidated()
        {
            var target = new SolidColorBrush(Colors.Red);
            var raised = false;

            target.Invalidated += (s, e) => raised = true;
            target.Color = Colors.Green;

            Assert.True(raised);
        }
    }
}
