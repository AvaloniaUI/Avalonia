using Avalonia.Data;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core
{
    public partial class BindingExpressionTests
    {
        [Fact]
        public void TwoWay_PropertyChanged_Should_Update_Source_On_Property_Changed()
        {
            var data = new ViewModel { StringValue = "foo" };
            var target = CreateTargetWithSource(data, o => o.StringValue, mode: BindingMode.TwoWay);

            Assert.Equal("foo", target.String);
            Assert.Equal("foo", data.StringValue);

            target.String = "bar";

            Assert.Equal("bar", target.String);
            Assert.Equal("bar", data.StringValue);
        }

        [Fact]
        public void TwoWay_LostFocus_Should_Update_Source_On_Lost_Focus()
        {
            using var app = StartWithFocusSupport();
            var data = new ViewModel { StringValue = "foo" };
            var target = CreateTargetWithSource(
                data,
                o => o.StringValue,
                mode: BindingMode.TwoWay,
                updateSourceTrigger: UpdateSourceTrigger.LostFocus);
            var root = new TestRoot(target) { Focusable = true };

            target.Focus();

            Assert.Equal("foo", target.String);
            Assert.Equal("foo", data.StringValue);

            target.String = "bar";

            Assert.Equal("bar", target.String);
            Assert.Equal("foo", data.StringValue);

            root.Focus();

            Assert.Equal("bar", target.String);
            Assert.Equal("bar", data.StringValue);
        }

        [Fact]
        public void OneWayToSource_LostFocus_Should_Update_Source_On_Lost_Focus()
        {
            using var app = StartWithFocusSupport();
            var data = new ViewModel { StringValue = "foo" };
            var target = CreateTargetWithSource(
                data,
                o => o.StringValue,
                mode: BindingMode.OneWayToSource,
                updateSourceTrigger: UpdateSourceTrigger.LostFocus);
            var root = new TestRoot(target) { Focusable = true };

            target.Focus();

            Assert.Null(target.String);
            Assert.Equal("foo", data.StringValue);

            target.String = "bar";

            Assert.Equal("bar", target.String);
            Assert.Equal("foo", data.StringValue);

            root.Focus();

            Assert.Equal("bar", target.String);
            Assert.Equal("bar", data.StringValue);
        }

        [Fact]
        public void TwoWay_Explicit_Should_Update_Source_On_Call_To_UpdateSource()
        {
            using var app = StartWithFocusSupport();
            var data = new ViewModel { StringValue = "foo" };
            var (target, expression) = CreateTargetAndExpression<ViewModel, string?>(
                o => o.StringValue,
                mode: BindingMode.TwoWay,
                source: data,
                updateSourceTrigger: UpdateSourceTrigger.Explicit);
            var root = new TestRoot(target) { Focusable = true };

            target.Focus();

            Assert.Equal("foo", target.String);
            Assert.Equal("foo", data.StringValue);

            target.String = "bar";

            Assert.Equal("bar", target.String);
            Assert.Equal("foo", data.StringValue);

            root.Focus();

            Assert.Equal("bar", target.String);
            Assert.Equal("foo", data.StringValue);

            expression.UpdateSource();

            Assert.Equal("bar", target.String);
            Assert.Equal("bar", data.StringValue);
        }
    }
}
