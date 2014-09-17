// -----------------------------------------------------------------------
// <copyright file="VisualTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Perspex.SceneGraph.UnitTests
{
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class VisualTests
    {
        [TestMethod]
        public void Initial_Children_Should_Be_Created()
        {
            var target = new TestVisual
            {
                InitialChildren = new[] { new Visual(), new Visual() }
            };

            var result = target.GetVisualChildren().ToList();

            CollectionAssert.AreEqual(target.InitialChildren.ToList(), result);
        }

        [TestMethod]
        public void Initial_Children_Should_Have_VisualParent_Set()
        {
            var target = new TestVisual
            {
                InitialChildren = new[] { new Visual(), new Visual() }
            };

            var result = target.GetVisualChildren().Select(x => x.GetVisualParent()).ToList();

            CollectionAssert.AreEqual(new[] { target, target }, result);
        }

        [TestMethod]
        public void Added_Child_Should_Have_VisualParent_Set()
        {
            var target = new TestVisual();
            var child = new Visual();

            target.AddChild(child);

            Assert.AreEqual(target, child.GetVisualParent());
        }

        [TestMethod]
        public void Removed_Child_Should_Have_VisualParent_Cleared()
        {
            var target = new TestVisual();
            var child = new Visual();

            target.AddChild(child);
            target.RemoveChild(child);

            Assert.IsNull(child.GetVisualParent());
        }

        [TestMethod]
        public void Clearing_Children_Should_Clear_VisualParent()
        {
            var target = new TestVisual
            {
                InitialChildren = new[] { new Visual(), new Visual() }
            };

            target.ClearChildren();

            var result = target.InitialChildren.Select(x => x.GetVisualParent()).ToList();

            CollectionAssert.AreEqual(new Visual[] { null, null }, result);
        }

        [TestMethod]
        public void ParentChanged_Attached_Methods_Should_Be_Called_In_Right_Order()
        {
            var target = new TestRoot();
            var child = new TestVisual();
            int changed = 0;
            int attched = 0;
            int i = 1;

            target.InitialChildren = new[] { child };
            child.VisualParentChangedCalled += (s, e) => changed = i++;
            child.AttachedToVisualTreeCalled += (s, e) => attched = i++;

            target.GetVisualChildren().First();

            Assert.AreEqual(1, changed);
            Assert.AreEqual(2, attched);
        }

        [TestMethod]
        public void ParentChanged_Detached_Methods_Should_Be_Called_In_Right_Order()
        {
            var target = new TestRoot();
            var child = new TestVisual();
            int changed = 0;
            int detached = 0;
            int i = 1;

            target.InitialChildren = new[] { child };
            target.GetVisualChildren().First();

            child.VisualParentChangedCalled += (s, e) => changed = i++;
            child.DetachedFromVisualTreeCalled += (s, e) => detached = i++;

            target.ClearChildren();

            Assert.AreEqual(1, changed);
            Assert.AreEqual(2, detached);
        }
    }
}
