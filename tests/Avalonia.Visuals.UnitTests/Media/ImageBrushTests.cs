using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Moq;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class ImageBrushTests
    {
        [Fact]
        public void Changing_Source_Raises_Invalidated()
        {
            var bitmap1 = Mock.Of<IBitmap>();
            var bitmap2 = Mock.Of<IBitmap>();
            var target = new ImageBrush(bitmap1);
            var raised = false;

            target.Invalidated += (s, e) => raised = true;
            target.Source = bitmap2;

            Assert.True(raised);
        }
    }
}
