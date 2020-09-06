using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Xunit;

namespace Avalonia.Styling.UnitTests
{
    public class SelectorTests_Child
    {
        [Fact]
        public void Child_Matches_Control_When_It_Is_Child_OfType()
        {
            var parent = new TestLogical1();
            var child = new TestLogical2();

            child.LogicalParent = parent;

            var selector = default(Selector).OfType<TestLogical1>().Child().OfType<TestLogical2>();

            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, selector.Match(child).Result);
        }

        [Fact]
        public void Child_Doesnt_Match_Control_When_It_Is_Grandchild_OfType()
        {
            var grandparent = new TestLogical1();
            var parent = new TestLogical2();
            var child = new TestLogical3();

            parent.LogicalParent = grandparent;
            child.LogicalParent = parent;

            var selector = default(Selector).OfType<TestLogical1>().Child().OfType<TestLogical3>();

            Assert.Equal(SelectorMatchResult.NeverThisInstance, selector.Match(child).Result);
        }

        [Fact]
        public async Task Child_Matches_Control_When_It_Is_Child_OfType_And_Class()
        {
            var parent = new TestLogical1();
            var child = new TestLogical2();

            child.LogicalParent = parent;

            var selector = default(Selector).OfType<TestLogical1>().Class("foo").Child().OfType<TestLogical2>();
            var activator = selector.Match(child).Activator;
            var result = new List<bool>();

            Assert.False(await activator.Take(1));
            parent.Classes.Add("foo");
            Assert.True(await activator.Take(1));
            parent.Classes.Remove("foo");
            Assert.False(await activator.Take(1));
        }

        [Fact]
        public void Child_Doesnt_Match_Control_When_It_Has_No_Parent()
        {
            var control = new TestLogical3();
            var selector = default(Selector).OfType<TestLogical1>().Child().OfType<TestLogical3>();

            Assert.Equal(SelectorMatchResult.NeverThisInstance, selector.Match(control).Result);
        }

        [Fact]
        public void Child_Selector_Should_Have_Correct_String_Representation()
        {
            var selector = default(Selector).OfType<TestLogical1>().Child().OfType<TestLogical3>();

            Assert.Equal("TestLogical1 > TestLogical3", selector.ToString());
        }

        public abstract class TestLogical : Control
        {
            public ILogical LogicalParent
            {
                get => Parent;
                set => ((ISetLogicalParent)this).SetParent(value);
            }

            public void ClearValue(AvaloniaProperty property)
            {
                throw new NotImplementedException();
            }

            public void ClearValue<T>(AvaloniaProperty<T> property)
            {
                throw new NotImplementedException();
            }

            public void AddInheritanceChild(IAvaloniaObject child)
            {
                throw new NotImplementedException();
            }

            public void RemoveInheritanceChild(IAvaloniaObject child)
            {
                throw new NotImplementedException();
            }

            public void InheritanceParentChanged<T>(StyledPropertyBase<T> property, IAvaloniaObject oldParent, IAvaloniaObject newParent)
            {
                throw new NotImplementedException();
            }

            public void InheritedPropertyChanged<T>(AvaloniaProperty<T> property, Optional<T> oldValue, Optional<T> newValue)
            {
                throw new NotImplementedException();
            }

            public void ClearValue<T>(StyledPropertyBase<T> property)
            {
                throw new NotImplementedException();
            }

            public void ClearValue<T>(DirectPropertyBase<T> property)
            {
                throw new NotImplementedException();
            }

            public T GetValue<T>(StyledPropertyBase<T> property)
            {
                throw new NotImplementedException();
            }

            public T GetValue<T>(DirectPropertyBase<T> property)
            {
                throw new NotImplementedException();
            }

            public void SetValue<T>(StyledPropertyBase<T> property, T value, BindingPriority priority = BindingPriority.LocalValue)
            {
                throw new NotImplementedException();
            }

            public void SetValue<T>(DirectPropertyBase<T> property, T value)
            {
                throw new NotImplementedException();
            }

            public IDisposable Bind<T>(StyledPropertyBase<T> property, IObservable<BindingValue<T>> source, BindingPriority priority = BindingPriority.LocalValue)
            {
                throw new NotImplementedException();
            }

            public IDisposable Bind<T>(DirectPropertyBase<T> property, IObservable<BindingValue<T>> source)
            {
                throw new NotImplementedException();
            }
        }

        public class TestLogical1 : TestLogical
        {
        }

        public class TestLogical2 : TestLogical
        {
        }

        public class TestLogical3 : TestLogical
        {
        }
    }
}
