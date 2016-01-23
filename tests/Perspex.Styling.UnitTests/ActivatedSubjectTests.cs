// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Xunit;

namespace Perspex.Styling.UnitTests
{
    public class ActivatedSubjectTests
    {
        [Fact]
        public void Should_Set_Values()
        {
            var activator = new BehaviorSubject<bool>(false);
            var source = new TestSubject();
            var target = new ActivatedSubject(activator, source, string.Empty);

            target.OnNext("bar");
            Assert.Equal(PerspexProperty.UnsetValue, source.Value);
            activator.OnNext(true);
            target.OnNext("baz");
            Assert.Equal("baz", source.Value);
            activator.OnNext(false);
            Assert.Equal(PerspexProperty.UnsetValue, source.Value);
            target.OnNext("bax");
            activator.OnNext(true);
            Assert.Equal("bax", source.Value);
        }

        [Fact]
        public void Should_Invoke_OnCompleted_On_Activator_Completed()
        {
            var activator = new BehaviorSubject<bool>(false);
            var source = new TestSubject();
            var target = new ActivatedSubject(activator, source, string.Empty);

            activator.OnCompleted();

            Assert.True(source.Completed);
        }

        [Fact]
        public void Should_Invoke_OnError_On_Activator_Error()
        {
            var activator = new BehaviorSubject<bool>(false);
            var source = new TestSubject();
            var target = new ActivatedSubject(activator, source, string.Empty);

            activator.OnError(new Exception());

            Assert.NotNull(source.Error);
        }

        private class Class1 : PerspexObject
        {
            public static readonly PerspexProperty<string> FooProperty =
                PerspexProperty.Register<Class1, string>("Foo", "foodefault");

            public string Foo
            {
                get { return GetValue(FooProperty); }
                set { SetValue(FooProperty, value); }
            }
        }

        private class TestSubject : ISubject<object>
        {
            private IObserver<object> _observer;

            public bool Completed { get; set; }
            public Exception Error { get; set; }
            public object Value { get; set; } = PerspexProperty.UnsetValue;

            public void OnCompleted()
            {
                Completed = true;
            }

            public void OnError(Exception error)
            {
                Error = error;
            }

            public void OnNext(object value)
            {
                Value = value;
            }

            public IDisposable Subscribe(IObserver<object> observer)
            {
                _observer = observer;
                return Disposable.Empty;
            }
        }
    }
}
