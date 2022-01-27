﻿using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Moq;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Rendering.SceneGraph
{
    public class TextNodeTests
    {
        [Fact]
        public void Bounds_Should_Be_Offset_By_Origin()
        {
            var target = new TextNode(
                Matrix.Identity,
                Brushes.Black,
                new Point(10, 10),
                Mock.Of<IFormattedTextImpl>(x => x.Bounds == new Rect(5, 5, 50, 50)));

            Assert.Equal(new Rect(15, 15, 50, 50), target.Bounds);
        }
    }
}
