using System;
using Avalonia.Controls;
using Avalonia.Data;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core
{
    public partial class BindingExpressionTests
    {
        [Fact]
        public void Should_Get_Simple_Property_Value()
        {
            var data = new { Foo = "foo" };
            var target = CreateTargetWithSource(data, o => o.Foo);

            Assert.Equal("foo", target.String);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Get_Simple_Property_Value_Null()
        {
            var data = new { Foo = (string?)null };
            var target = CreateTargetWithSource(data, o => o.Foo);

            Assert.Null(target.String);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Get_Simple_Property_From_Base_Class()
        {
            var data = new DerivedViewModel { StringValue = "foo" };
            var target = CreateTargetWithSource(data, o => o.StringValue);

            Assert.Equal("foo", target.String);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Get_Simple_Property_Chain()
        {
            var data = new { Foo = new { Bar = new { Baz = "baz" } } };
            var target = CreateTargetWithSource(data, o => o.Foo.Bar.Baz);

            Assert.Equal("baz", target.String);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Simple_Property_Value()
        {
            var data = new ViewModel { StringValue = "foo" };
            var target = CreateTargetWithSource(data, o => o.StringValue);

            Assert.Equal("foo", target.String);

            data.StringValue = "bar";

            Assert.Equal("bar", target.String);

            GC.KeepAlive(data);
        }

        [Fact]
        public void PropertyChangedEventArgs_With_Null_PropertyName_Should_Trigger_Update()
        {
            var data = new ViewModel { StringValue = "foo" };
            var target = CreateTargetWithSource(data, o => o.StringValue);

            Assert.Equal("foo", target.String);

            data.SetStringValueWithoutRaising("bar");

            Assert.Equal("foo", target.String);

            data.RaisePropertyChanged(null);

            Assert.Equal("bar", target.String);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_End_Of_Property_Chain_Changing()
        {
            var data = new ViewModel { Next = new() { StringValue = "bar" } };
            var target = CreateTargetWithSource(data, o => o.Next!.StringValue);

            Assert.Equal("bar", target.String);

            data.Next.StringValue = "baz";

            Assert.Equal("baz", target.String);

            data.Next.StringValue = null;

            Assert.Null(target.String);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Property_Chain_Changing()
        {
            var data = new ViewModel { Next = new() { StringValue = "bar" } };
            var target = CreateTargetWithSource(data, o => o.Next!.StringValue);
            var old = data.Next;

            Assert.Equal("bar", target.String);

            data.Next = new() { StringValue = "baz" };

            Assert.Equal("baz", target.String);

            data.Next = new() { StringValue = null };

            Assert.Null(target.String);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Property_Chain_Breaking_With_Null_Then_Mending()
        {
            var data = new ViewModel
            {
                Next = new()
                {
                    Next = new()
                    {
                        StringValue = "bar"
                    }
                }
            };

            var target = CreateTargetWithSource(data, o => o.Next!.Next!.StringValue);

            Assert.Equal("bar", target.String);

            var old = data.Next;
            data.Next = null;

            Assert.Null(target.String);

            data.Next = old;

            Assert.Equal("bar", target.String);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Track_Property_Chain_Breaking_With_Missing_Member_Then_Mending()
        {
            var data = new ViewModel { ObjectValue = new ViewModel { StringValue = "bar" } };
            var target = CreateTargetWithSource(data, o => (o.ObjectValue as ViewModel)!.StringValue);

            Assert.Equal("bar", target.String);

            var old = data.ObjectValue;
            data.ObjectValue = new { MissingMember = "Yes" };

            Assert.Null(target.String);

            data.ObjectValue = old;

            Assert.Equal("bar", target.String);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Should_Not_Keep_Source_Alive()
        {
            Func<(TargetClass, WeakReference<ViewModel>)> run = () =>
            {
                var source = new ViewModel { StringValue = "foo" };
                var target = CreateTargetWithSource(source, o => o.StringValue);
                return (target, new(source));
            };

            var result = run();

            // Mono trickery
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            GC.WaitForPendingFinalizers();
            GC.Collect(2);

            Assert.False(result.Item2.TryGetTarget(out _));
        }

        [Fact]
        public void Should_Not_Throw_Exception_On_Duplicate_Properties()
        {
            // Repro of https://github.com/AvaloniaUI/Avalonia/issues/4733.
            var source = new DerivedViewModelWithDuplicateProperty { StringValue = "NewName" };
            var target = CreateTargetWithSource(source, x => x.StringValue);

            Assert.Equal("NewName", target.String);
        }

        [Fact]
        public void Can_Convert_Int_To_Enum_Two_Way()
        {
            var data = new ViewModel { IntValue = 1 };
            var target = CreateTargetWithSource(
                data,
                o => o.IntValue,
                mode: BindingMode.TwoWay,
                targetProperty: DockPanel.DockProperty);

            Assert.Equal(Dock.Bottom, DockPanel.GetDock(target));

            DockPanel.SetDock(target, Dock.Right);

            Assert.Equal(2, data.IntValue);

            GC.KeepAlive(data);
        }

        private class DerivedViewModel : ViewModel
        {
        }

        private class DerivedViewModelWithDuplicateProperty : ViewModel
        {
            public new string? StringValue { get; set; }
        }
    }
}
