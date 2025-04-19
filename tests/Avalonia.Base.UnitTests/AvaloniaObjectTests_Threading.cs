using System;
using System.Reactive.Subjects;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_Threading : ScopedTestBase
    {
        void AssertThrowsOnDifferentThread(Action cb)
        {
            Assert.Throws<InvalidOperationException>(() =>
                ThreadRunHelper.RunOnDedicatedThread(cb).GetAwaiter().GetResult());
        }
        
        [Fact]
        public void StyledProperty_GetValue_Should_Throw()
        {
            using (UnitTestApplication.Start())
            {
                var target = new Class1();
                target.GetValue(Class1.StyledProperty);
                
                AssertThrowsOnDifferentThread(() => target.GetValue(Class1.StyledProperty));
            }
        }

        [Fact]
        public void StyledProperty_SetValue_Should_Throw()
        {
            using (UnitTestApplication.Start())
            {
                var target = new Class1();
                AssertThrowsOnDifferentThread(() => target.SetValue(Class1.StyledProperty, "foo"));
            }
        }

        [Fact]
        public void Setting_StyledProperty_Binding_Should_Throw()
        {
            using (UnitTestApplication.Start())
            {
                var target = new Class1();
                AssertThrowsOnDifferentThread(() => 
                    target.Bind(
                        Class1.StyledProperty,
                        new BehaviorSubject<string>("foo")));
            }
        }

        [Fact]
        public void StyledProperty_ClearValue_Should_Throw()
        {
            using (UnitTestApplication.Start())
            {
                var target = new Class1();
                AssertThrowsOnDifferentThread(() => target.ClearValue(Class1.StyledProperty));
            }
        }

        [Fact]
        public void StyledProperty_IsSet_Should_Throw()
        {
            using (UnitTestApplication.Start())
            {
                var target = new Class1();
                AssertThrowsOnDifferentThread(() => target.IsSet(Class1.StyledProperty));
            }
        }

        [Fact]
        public void DirectProperty_GetValue_Should_Throw()
        {
            using (UnitTestApplication.Start())
            {
                var target = new Class1();
                AssertThrowsOnDifferentThread(() => target.GetValue(Class1.DirectProperty));
            }
        }

        [Fact]
        public void DirectProperty_SetValue_Should_Throw()
        {
            using (UnitTestApplication.Start())
            {
                var target = new Class1();
                AssertThrowsOnDifferentThread(() => target.SetValue(Class1.DirectProperty, "foo"));
            }
        }

        [Fact]
        public void Setting_DirectProperty_Binding_Should_Throw()
        {
            using (UnitTestApplication.Start())
            {
                var target = new Class1();
                AssertThrowsOnDifferentThread(() =>
                    target.Bind(
                        Class1.DirectProperty,
                        new BehaviorSubject<string>("foo")));
            }
        }

        [Fact]
        public void DirectProperty_ClearValue_Should_Throw()
        {
            using (UnitTestApplication.Start())
            {
                var target = new Class1();
                AssertThrowsOnDifferentThread(() => target.ClearValue(Class1.DirectProperty));
            }
        }

        [Fact]
        public void DirectProperty_IsSet_Should_Throw()
        {
            using (UnitTestApplication.Start())
            {
                var target = new Class1();
                AssertThrowsOnDifferentThread(() => target.IsSet(Class1.DirectProperty));
            }
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> StyledProperty =
                AvaloniaProperty.Register<Class1, string>("Foo", "foodefault");

            public static readonly DirectProperty<Class1, string> DirectProperty =
                AvaloniaProperty.RegisterDirect<Class1, string>("Qux", _ => null, (o, v) => { });
        }
    }
}
