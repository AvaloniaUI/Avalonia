using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Parsers;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public class ExpressionObserverTests_Task
    {
        [Fact]
        public void Should_Not_Get_Task_Result_Without_StreamBinding()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var tcs = new TaskCompletionSource<string>();
                var data = new { Foo = tcs.Task };
                var target = ExpressionObserver.Create(data, o => o.Foo);
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));
                tcs.SetResult("foo");
                sync.ExecutePostedCallbacks();

                Assert.Single(result);
                Assert.IsType<Task<string>>(result[0]);

                GC.KeepAlive(data);
            }
        }

        [Fact]
        public void Should_Get_Completed_Task_Value()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var data = new { Foo = Task.FromResult("foo") };
                var target = ExpressionObserver.Create(data, o => o.Foo.StreamBinding());
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));

                Assert.Equal(new[] { "foo" }, result);

                GC.KeepAlive(data);
            }
        }

        [Fact]
        public void Should_Get_Property_Value_From_Task()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var tcs = new TaskCompletionSource<Class2>();
                var data = new Class1(tcs.Task);
                var target = ExpressionObserver.Create(data, o => o.Next.StreamBinding().Foo);
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));
                tcs.SetResult(new Class2("foo"));
                sync.ExecutePostedCallbacks();

                Assert.Equal(new[] { "foo" }, result);

                GC.KeepAlive(data);
            }
        }

        [Fact]
        public void Should_Return_BindingNotification_Error_On_Task_Exception()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var tcs = new TaskCompletionSource<string>();
                var data = new { Foo = tcs.Task };
                var target = ExpressionObserver.Create(data, o => o.Foo.StreamBinding());
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));
                tcs.SetException(new NotSupportedException());
                sync.ExecutePostedCallbacks();

                Assert.Equal(
                    new[] 
                    {
                        new BindingNotification(
                            new AggregateException(new NotSupportedException()),
                            BindingErrorType.Error)
                    }, 
                    result);

                GC.KeepAlive(data);
            }
        }

        [Fact]
        public void Should_Return_BindingNotification_Error_For_Faulted_Task()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var data = new { Foo = TaskFromException(new NotSupportedException()) };
                var target = ExpressionObserver.Create(data, o => o.Foo.StreamBinding());
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));

                Assert.Equal(
                    new[]
                    {
                        new BindingNotification(
                            new AggregateException(new NotSupportedException()),
                            BindingErrorType.Error)
                    },
                    result);

                GC.KeepAlive(data);
            }
        }

        [Fact]
        public void Should_Get_Simple_Task_Value_With_Data_DataValidation_Enabled()
        {
            using (var sync = UnitTestSynchronizationContext.Begin())
            {
                var tcs = new TaskCompletionSource<string>();
                var data = new { Foo = tcs.Task };
                var target = ExpressionObserver.Create(data, o => o.Foo.StreamBinding(), true);
                var result = new List<object>();

                var sub = target.Subscribe(x => result.Add(x));
                tcs.SetResult("foo");
                sync.ExecutePostedCallbacks();

                // What does it mean to have data validation on a Task? Without a use-case it's
                // hard to know what to do here so for the moment the value is returned.
                Assert.Equal(new [] { "foo" }, result);

                GC.KeepAlive(data);
            }
        }

        private static Task TaskFromException(Exception e)
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetException(e);
            return tcs.Task;
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
