using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    [InvariantCulture]
    public class BindingExpressionTests_UpdateSourceTrigger
    {
        [Fact]
        public void TwoWay_PropertyChanged_Should_Update_Source_On_Property_Changed()
        {
            using var app = Start();
            var data = new ViewModel();
            var binding = new Binding
            {
                Path = "Foo",
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };

            var target = new TextBox { DataContext = data };
            target.Bind(TextBox.TextProperty, binding);

            var root = new TestRoot(target);

            Assert.Equal("foo", target.Text);
            Assert.Equal("foo", data.Foo);

            target.Text = "bar";

            Assert.Equal("bar", target.Text);
            Assert.Equal("bar", data.Foo);
        }

        [Fact]
        public void TwoWay_LostFocus_Should_Update_Source_On_Lost_Focus()
        {
            using var app = Start();
            var data = new ViewModel();
            var binding = new Binding
            {
                Path = "Foo",
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
            };

            var target = new TextBox { DataContext = data };
            target.Bind(TextBox.TextProperty, binding);

            var root = new TestRoot(target);
            root.Focusable = true;
            target.Focus();

            Assert.Equal("foo", target.Text);
            Assert.Equal("foo", data.Foo);

            target.Text = "bar";

            Assert.Equal("bar", target.Text);
            Assert.Equal("foo", data.Foo);

            root.Focus();

            Assert.Equal("bar", target.Text);
            Assert.Equal("bar", data.Foo);
        }

        [Fact]
        public void OneWayToSource_LostFocus_Should_Update_Source_On_Lost_Focus()
        {
            using var app = Start();
            var data = new ViewModel();
            var binding = new Binding
            {
                Path = "Foo",
                Mode = BindingMode.OneWayToSource,
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
            };

            var target = new TextBox { DataContext = data };
            target.Bind(TextBox.TextProperty, binding);

            var root = new TestRoot(target);
            root.Focusable = true;
            target.Focus();

            Assert.Null(target.Text);
            Assert.Null(data.Foo);

            target.Text = "bar";

            Assert.Equal("bar", target.Text);
            Assert.Null(data.Foo);

            root.Focus();

            Assert.Equal("bar", target.Text);
            Assert.Equal("bar", data.Foo);
        }

        [Fact]
        public void TwoWay_Explicit_Should_Update_Source_On_Call_To_UpdateSource()
        {
            using var app = Start();
            var data = new ViewModel();
            var binding = new Binding
            {
                Path = "Foo",
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit,
            };

            var target = new TextBox { DataContext = data };
            var expression = target.Bind(TextBox.TextProperty, binding);

            var root = new TestRoot(target);
            root.Focusable = true;
            target.Focus();

            Assert.Equal("foo", target.Text);
            Assert.Equal("foo", data.Foo);

            target.Text = "bar";

            Assert.Equal("bar", target.Text);
            Assert.Equal("foo", data.Foo);

            root.Focus();

            Assert.Equal("bar", target.Text);
            Assert.Equal("foo", data.Foo);

            expression.UpdateSource();

            Assert.Equal("bar", target.Text);
            Assert.Equal("bar", data.Foo);
        }

        private static IDisposable Start()
        {
            return UnitTestApplication.Start(TestServices.RealFocus);
        }

        private class ViewModel
        {
            public ViewModel(string foo = "foo") => Foo = foo;
            public string Foo { get; set; }
        }
    }
}
