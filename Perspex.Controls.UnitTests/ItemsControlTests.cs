// -----------------------------------------------------------------------
// <copyright file="ItemsControlTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Perspex.Collections;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Platform;
    using Perspex.Styling;
    using Perspex.VisualTree;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Splat;

    [TestClass]
    public class ItemsControlTests
    {
        [TestMethod]
        public void Panel_Should_Have_TemplatedParent_Set_To_ItemsPresenter()
        {
            var target = new ItemsControl();

            target.Template = this.GetTemplate();
            target.Items = new[] { "Foo" };
            target.ApplyTemplate();

            var presenter = target.GetTemplateControls().OfType<ItemsPresenter>().Single();
            var panel = presenter.GetTemplateControls().OfType<StackPanel>().Single();

            Assert.AreEqual(presenter, panel.TemplatedParent);
        }

        [TestMethod]
        public void Item_Should_Have_TemplatedParent_Set_To_Null()
        {
            var target = new ItemsControl();

            target.Template = this.GetTemplate();
            target.Items = new[] { "Foo" };
            target.ApplyTemplate();

            var presenter = target.GetTemplateControls().OfType<ItemsPresenter>().Single();
            var panel = presenter.GetTemplateControls().OfType<StackPanel>().Single();
            var item = (TextBlock)panel.GetVisualChildren().First();

            Assert.IsNull(item.TemplatedParent);
        }

        [TestMethod]
        public void Control_Item_Should_Have_Parent_Set()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = this.GetTemplate();
            target.Items = new[] { child };
            target.ApplyTemplate();

            Assert.AreEqual(target, child.Parent);
            Assert.AreEqual(target, ((ILogical)child).LogicalParent);
        }

        [TestMethod]
        public void Clearing_Control_Item_Should_Clear_Child_Controls_Parent()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = this.GetTemplate();
            target.Items = new[] { child };
            target.ApplyTemplate();
            target.Items = null;

            Assert.IsNull(child.Parent);
            Assert.IsNull(((ILogical)child).LogicalParent);
        }

        [TestMethod]
        public void Adding_Control_Item_Should_Make_Control_Appear_In_LogicalChildren()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = this.GetTemplate();
            target.Items = new[] { child };
            target.ApplyTemplate();

            CollectionAssert.AreEqual(new[] { child }, ((ILogical)target).LogicalChildren.ToList());
        }

        [TestMethod]
        public void Adding_String_Item_Should_Make_TextBlock_Appear_In_LogicalChildren()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = this.GetTemplate();
            target.Items = new[] { "Foo" };
            target.ApplyTemplate();

            var logical = (ILogical)target;
            Assert.AreEqual(1, logical.LogicalChildren.Count);
            Assert.IsInstanceOfType(logical.LogicalChildren[0], typeof(TextBlock));
        }

        [TestMethod]
        public void Setting_Items_To_Null_Should_Remove_LogicalChildren()
        {
            var target = new ItemsControl();
            var child = new Control();

            target.Template = this.GetTemplate();
            target.Items = new[] { "Foo" };
            target.ApplyTemplate();
            target.Items = null;

            CollectionAssert.AreEqual(new ILogical[0], ((ILogical)target).LogicalChildren.ToList());
        }

        [TestMethod]
        public void Setting_Items_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ItemsControl();
            var child = new Control();
            var called = false;

            target.Template = this.GetTemplate();
            target.ApplyTemplate();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            target.Items = new[] { child };

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Setting_Items_To_Null_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ItemsControl();
            var child = new Control();
            var called = false;

            target.Template = this.GetTemplate();
            target.Items = new[] { child };
            target.ApplyTemplate();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            target.Items = null;

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Changing_Items_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ItemsControl();
            var child = new Control();
            var called = false;

            target.Template = this.GetTemplate();
            target.Items = new[] { child };
            target.ApplyTemplate();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) => called = true;

            target.Items = new[] { "Foo" };

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Adding_Items_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ItemsControl();
            var items = new PerspexList<string> { "Foo" };
            var called = false;

            target.Template = this.GetTemplate();
            target.Items = items;
            target.ApplyTemplate();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Add;

            items.Add("Bar");

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Removing_Items_Should_Fire_LogicalChildren_CollectionChanged()
        {
            var target = new ItemsControl();
            var items = new PerspexList<string> { "Foo", "Bar" };
            var called = false;

            target.Template = this.GetTemplate();
            target.Items = items;
            target.ApplyTemplate();

            ((ILogical)target).LogicalChildren.CollectionChanged += (s, e) =>
                called = e.Action == NotifyCollectionChangedAction.Remove;

            items.Remove("Bar");

            Assert.IsTrue(called);
        }

        private ControlTemplate GetTemplate()
        {
            return ControlTemplate.Create<ItemsControl>(parent =>
            {
                return new Border
                {
                    Background = new Perspex.Media.SolidColorBrush(0xffffffff),
                    Content = new ItemsPresenter
                    {
                        Id = "itemsPresenter",
                        [~ItemsPresenter.ItemsProperty] = parent[~ItemsControl.ItemsProperty],
                    }
                };
            });
        }

        private IDisposable RegisterServices()
        {
            var result = Locator.CurrentMutable.WithResolver();
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var renderInterface = fixture.Create<IPlatformRenderInterface>();
            Locator.CurrentMutable.RegisterConstant(renderInterface, typeof(IPlatformRenderInterface));
            return result;
        }
    }
}
