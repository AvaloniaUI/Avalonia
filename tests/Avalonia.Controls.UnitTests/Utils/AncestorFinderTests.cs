using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Utils;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests.Utils
{
    public class AncestorFinderTests
    {
        [Fact]
        public void SanityCheck()
        {
            var child = new Control();
            var parent = new Decorator();
            var grandParent = new Border();
            var grandParent2 = new Border();

            StyledElement currentParent = null;
            var subscription = AncestorFinder.Create(child, typeof (Border)).Subscribe(s => currentParent = s);

            Assert.Null(currentParent);
            parent.Child = child;
            Assert.Null(currentParent);
            grandParent.Child = parent;
            Assert.Equal(grandParent, currentParent);
            grandParent.Child = null;
            grandParent2.Child = parent;
            Assert.Equal(grandParent2, currentParent);

            subscription.Dispose();
            parent.Child = null;
            Assert.Equal(grandParent2, currentParent);
        }


    }
}
