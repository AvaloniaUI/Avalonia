// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class NameScopeTests
    {
        [Fact]
        public void Register_Registers_Element()
        {
            var target = new NameScope();
            var element = new object();

            target.Register("foo", element);

            Assert.Same(element, target.Find("foo"));
        }

        [Fact]
        public void Cannot_Register_New_Element_With_Existing_Name()
        {
            var target = new NameScope();

            target.Register("foo", new object());
            Assert.Throws<ArgumentException>(() => target.Register("foo", new object()));
        }

        [Fact]
        public void Can_Register_Same_Element_More_Than_Once()
        {
            var target = new NameScope();
            var element = new object();

            target.Register("foo", element);
            target.Register("foo", element);

            Assert.Same(element, target.Find("foo"));
        }

        [Fact]
        public void Cannot_Register_New_Element_For_Completed_Scope()
        {
            var target = new NameScope();
            var element = new object();

            target.Register("foo", element);
            target.Complete();
            Assert.Throws<InvalidOperationException>(() => target.Register("bar", element));
        }

        
        /*
         `async void` here is intentional since we expect the continuation to be
         executed *synchronously* and behave more like an event handler to make sure that
         that the object graph is completely ready to use after it's built
         rather than have pending continuations queued by SynchronizationContext.
        */
        object _found = null;
        async void FindAsync(INameScope scope, string name)
        {
            _found = await scope.FindAsync(name);
        }
        
        [Fact]
        public void FindAsync_Should_Find_Controls_Added_Earlier()
        {
            var scope = new NameScope();
            var element = new object();
            scope.Register("foo", element);
            FindAsync(scope, "foo");
            Assert.Same(_found, element);
        }
        
        [Fact]
        public void FindAsync_Should_Find_Controls_Added_Later()
        {
            var scope = new NameScope();
            var element = new object();
            
            FindAsync(scope, "foo");
            Assert.Null(_found);
            scope.Register("foo", element);
            Assert.Same(_found, element);
        }
        
        [Fact]
        public void FindAsync_Should_Return_Null_After_Scope_Completion()
        {
            var scope = new NameScope();
            var element = new object();
            bool finished = false;
            async void Find(string name)
            {
                Assert.Null(await scope.FindAsync(name));
                finished = true;
            }
            Find("foo");
            Assert.False(finished);
            scope.Register("bar", element);
            Assert.False(finished);
            scope.Complete();
            Assert.True(finished);
        }

        [Fact]
        public void Child_Scope_Should_Not_Find_Control_In_Parent_Scope_Unless_Completed()
        {
            var scope = new NameScope();
            var childScope = new ChildNameScope(scope);
            var element = new object();
            scope.Register("foo", element);
            Assert.Null(childScope.Find("foo"));
            childScope.Complete();
            Assert.Same(element, childScope.Find("foo"));
        }
        
        [Fact]
        public void Child_Scope_Should_Prefer_Own_Elements()
        {
            var scope = new NameScope();
            var childScope = new ChildNameScope(scope);
            var element = new object();
            var childElement = new object();
            scope.Register("foo", element);
            childScope.Register("foo", childElement);
            childScope.Complete();
            Assert.Same(childElement, childScope.Find("foo"));
        }

        [Fact]
        public void Child_Scope_FindAsync_Should_Find_Elements_In_Parent_Scope_When_Child_Is_Completed()
        {
            var scope = new NameScope();
            var childScope = new ChildNameScope(scope);
            var element = new object();
            scope.Register("foo", element);
            FindAsync(childScope, "foo");
            Assert.Null(_found);
            childScope.Complete();
            Assert.Same(element, _found);
        }
        
        
        [Fact]
        public void Child_Scope_FindAsync_Should_Prefer_Own_Elements()
        {
            var scope = new NameScope();
            var childScope = new ChildNameScope(scope);
            var element = new object();
            var childElement = new object();
            FindAsync(childScope, "foo");
            scope.Register("foo", element);
            Assert.Null(_found);
            childScope.Register("foo", childElement);
            Assert.Same(childElement, childScope.Find("foo"));
            childScope.Complete();
            FindAsync(childScope, "foo");
            Assert.Same(childElement, childScope.Find("foo"));
        }

    }
}
