// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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

        public abstract class TestLogical : ILogical, IStyleable
        {
            public TestLogical()
            {
                Classes = new Classes();
            }

            public event EventHandler<AvaloniaPropertyChangedEventArgs> PropertyChanged;
            public event EventHandler<AvaloniaPropertyChangedEventArgs> InheritablePropertyChanged;
            public event EventHandler<LogicalTreeAttachmentEventArgs> AttachedToLogicalTree;
            public event EventHandler<LogicalTreeAttachmentEventArgs> DetachedFromLogicalTree;

            public Classes Classes { get; }

            public string Name { get; set; }

            public bool IsAttachedToLogicalTree { get; }

            public IAvaloniaReadOnlyList<ILogical> LogicalChildren { get; set; }

            public ILogical LogicalParent { get; set; }

            public Type StyleKey { get; }

            public ITemplatedControl TemplatedParent { get; }

            IObservable<IStyleable> IStyleable.StyleDetach { get; }

            IAvaloniaReadOnlyList<string> IStyleable.Classes => Classes;

            public object GetValue(AvaloniaProperty property)
            {
                throw new NotImplementedException();
            }

            public T GetValue<T>(AvaloniaProperty<T> property)
            {
                throw new NotImplementedException();
            }

            public void SetValue(AvaloniaProperty property, object value, BindingPriority priority)
            {
                throw new NotImplementedException();
            }

            public void SetValue<T>(AvaloniaProperty<T> property, T value, BindingPriority priority = BindingPriority.LocalValue)
            {
                throw new NotImplementedException();
            }

            public IDisposable Bind(AvaloniaProperty property, IObservable<object> source, BindingPriority priority)
            {
                throw new NotImplementedException();
            }

            public bool IsAnimating(AvaloniaProperty property)
            {
                throw new NotImplementedException();
            }

            public bool IsSet(AvaloniaProperty property)
            {
                throw new NotImplementedException();
            }

            public IDisposable Bind<T>(AvaloniaProperty<T> property, IObservable<T> source, BindingPriority priority = BindingPriority.LocalValue)
            {
                throw new NotImplementedException();
            }

            public void NotifyAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
            {
                throw new NotImplementedException();
            }

            public void NotifyDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
            {
                throw new NotImplementedException();
            }

            public void NotifyResourcesChanged(ResourcesChangedEventArgs e)
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
