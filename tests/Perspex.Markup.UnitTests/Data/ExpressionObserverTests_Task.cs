// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Perspex.Markup.Data;
using Perspex.UnitTests;
using Xunit;

namespace Perspex.Markup.UnitTests.Data
{
    public class ExpressionObserverTests_Task
    {
        [Fact]
        public void Should_Get_Simple_Task_Value()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var tcs = new TaskCompletionSource<string>();
                var data = new { Foo = tcs.Task };
                var target = new ExpressionObserver(data, "Foo");
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));
                tcs.SetResult("foo");
                sync.ExecutePostedCallbacks();

                Assert.Equal(new object[] { PerspexProperty.UnsetValue, "foo" }, result.ToArray());
            }
        }

        [Fact]
        public void Should_Get_Completed_Task_Value()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var data = new { Foo = Task.FromResult("foo") };
                var target = new ExpressionObserver(data, "Foo");
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));

                Assert.Equal(new object[] { "foo" }, result.ToArray());
            }
        }

        [Fact]
        public void Should_Get_Property_Value_From_Task()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var tcs = new TaskCompletionSource<Class2>();
                var data = new Class1(tcs.Task);
                var target = new ExpressionObserver(data, "Next.Foo");
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));
                tcs.SetResult(new Class2("foo"));
                sync.ExecutePostedCallbacks();

                Assert.Equal(new object[] { PerspexProperty.UnsetValue, "foo" }, result.ToArray());
            }
        }

        private class Class1 : NotifyingBase
        {
            public Class1(Task<Class2> next)
            {
                Next = next;
            }

            public Task<Class2> Next { get; }
        }

        private class Class2 : NotifyingBase
        {
            public Class2(string foo)
            {
                Foo = foo;
            }

            public string Foo { get; }
        }
    }
}
