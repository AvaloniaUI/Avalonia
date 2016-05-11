// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Markup.Data;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
{
    public class ExpressionObserverTests_Observable
    {
        [Fact]
        public void Should_Get_Simple_Observable_Value()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var source = new BehaviorSubject<string>("foo");
                var data = new { Foo = source };
                var target = new ExpressionObserver(data, "Foo");
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));
                source.OnNext("bar");
                sync.ExecutePostedCallbacks();

                Assert.Equal(new[] { AvaloniaProperty.UnsetValue, "foo", "bar" }, result);
            }
        }

        [Fact]
        public void Should_Get_Property_Value_From_Observable()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var data = new Class1();
                var target = new ExpressionObserver(data, "Next.Foo");
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));
                data.Next.OnNext(new Class2("foo"));
                sync.ExecutePostedCallbacks();

                Assert.Equal(new[] { AvaloniaProperty.UnsetValue, "foo" }, result);

                sub.Dispose();
                Assert.Equal(0, data.SubscriptionCount);
            }
        }

        private class Class1 : NotifyingBase
        {
            public Subject<Class2> Next { get; } = new Subject<Class2>();
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
