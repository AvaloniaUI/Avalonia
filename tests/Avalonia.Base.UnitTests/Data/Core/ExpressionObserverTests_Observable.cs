// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Parsers;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class ExpressionObserverTests_Observable
    {
        [Fact]
        public void Should_Not_Get_Observable_Value_Without_Streaming()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var source = new BehaviorSubject<string>("foo");
                var data = new { Foo = source };
                var target = ExpressionObserver.Create(data, o => o.Foo);
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));
                source.OnNext("bar");
                sync.ExecutePostedCallbacks();

                Assert.Equal(new[] { source }, result);

                GC.KeepAlive(data);
            }
        }

        [Fact]
        public void Should_Get_Simple_Observable_Value()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var source = new BehaviorSubject<string>("foo");
                var data = new { Foo = source };
                var target = ExpressionObserver.Create(data, o => o.Foo.StreamBinding());
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));
                source.OnNext("bar");
                sync.ExecutePostedCallbacks();

                Assert.Equal(new[] { "foo", "bar" }, result);

                GC.KeepAlive(data);
            }
        }

        [Fact]
        public void Should_Get_Property_Value_From_Observable()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var data = new Class1();
                var target = ExpressionObserver.Create(data, o => o.Next.StreamBinding().Foo);
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));
                data.Next.OnNext(new Class2("foo"));
                sync.ExecutePostedCallbacks();

                Assert.Equal(new[] { "foo" }, result);

                sub.Dispose();
                Assert.Equal(0, data.PropertyChangedSubscriptionCount);

                GC.KeepAlive(data);
            }
        }

        [Fact]
        public void Should_Get_Simple_Observable_Value_With_DataValidation_Enabled()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var source = new BehaviorSubject<string>("foo");
                var data = new { Foo = source };
                var target = ExpressionObserver.Create(data, o => o.Foo.StreamBinding(), true);
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));
                source.OnNext("bar");
                sync.ExecutePostedCallbacks();

                // What does it mean to have data validation on an observable? Without a use-case
                // it's hard to know what to do here so for the moment the value is returned.
                Assert.Equal(new[] { "foo", "bar" }, result);

                GC.KeepAlive(data);
            }
        }

        [Fact]
        public void Should_Get_Property_Value_From_Observable_With_DataValidation_Enabled()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var data1 = new Class1();
                var data2 = new Class2("foo");
                var target = ExpressionObserver.Create(data1, o => o.Next.StreamBinding().Foo, true);
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));
                data1.Next.OnNext(data2);
                sync.ExecutePostedCallbacks();

                Assert.Equal(new[] { new BindingNotification("foo") }, result);

                sub.Dispose();
                Assert.Equal(0, data1.PropertyChangedSubscriptionCount);

                GC.KeepAlive(data1);
                GC.KeepAlive(data2);
            }
        }

        [Fact]
        public void Should_Return_BindingNotification_If_Stream_Operator_Applied_To_Not_Supported_Type()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var data = new NotStreamable();
                var target = ExpressionObserver.Create(data, o => o.StreamBinding());
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));
                sync.ExecutePostedCallbacks();

                Assert.Equal(
                    new[]
                    {
                        new BindingNotification(
                            new MarkupBindingChainException("Stream operator applied to unsupported type", "o => o.StreamBinding()", "^"),
                            BindingErrorType.Error)
                    },
                    result);

                sub.Dispose();

                GC.KeepAlive(data);
            }
        }

        [Fact]
        public void Should_Work_With_Value_Type()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var source = new BehaviorSubject<int>(1);
                var data = new { Foo = source };
                var target = ExpressionObserver.Create(data, o => o.Foo.StreamBinding());
                var result = new List<int>();

                var sub = target.Subscribe(x => result.Add((int)x));
                source.OnNext(42);
                sync.ExecutePostedCallbacks();

                Assert.Equal(new[] { 1, 42 }, result);

                GC.KeepAlive(data);
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

        private class NotStreamable
        {
            public object StreamBinding() { throw new InvalidOperationException(); }
        }
    }
}
