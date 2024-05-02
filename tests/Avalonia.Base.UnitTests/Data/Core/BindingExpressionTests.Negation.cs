using System;
using Avalonia.Data;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core
{
    public partial class BindingExpressionTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Should_Negate_Boolean_Value(bool value)
        {
            var data = new ViewModel { BoolValue = value };
            var target = CreateTargetWithSource(data, o => !o.BoolValue);

            //Assert.Equal(!value, target.Bool);

            GC.KeepAlive(data);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Should_Negate_Boolean_Value_In_Path(bool value)
        {
            var data = new ViewModel { Next = new() { BoolValue = value } };
            var target = CreateTargetWithSource(data, o => !o.Next!.BoolValue);

            Assert.Equal(!value, (bool)target.Bool);

            GC.KeepAlive(data);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Should_Double_Negate_Boolean_Value(bool value)
        {
            var data = new ViewModel { BoolValue = value };
            var target = CreateTargetWithSource(data, o => !!o.BoolValue);

            Assert.Equal(value, (bool)target.Bool);

            GC.KeepAlive(data);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Should_Double_Negate_Boolean_Value_In_Path(bool value)
        {
            var data = new ViewModel { Next = new() { BoolValue = value } };
            var target = CreateTargetWithSource(data, o => !!o.Next!.BoolValue);

            Assert.Equal(value, (bool)target.Bool);

            GC.KeepAlive(data);
        }

        [Fact]
        public void Can_Set_Negated_Value()
        {
            var data = new ViewModel { BoolValue = true };
            var target = CreateTargetWithSource(data, o => !o.BoolValue, mode: BindingMode.TwoWay);

            target.Bool = true;

            Assert.False(data.BoolValue);
        }

        [Fact]
        public void Can_Set_Negated_Value_In_Path()
        {
            var data = new ViewModel { Next = new() { BoolValue = true } };
            var target = CreateTargetWithSource(data, o => !o.Next!.BoolValue, mode: BindingMode.TwoWay);

            target.Bool = true;

            Assert.False(data.Next.BoolValue);
        }

        [Fact]
        public void Can_Set_Double_Negated_Value()
        {
            var data = new ViewModel { BoolValue = true };
            var target = CreateTargetWithSource(data, o => !!o.BoolValue, mode: BindingMode.TwoWay);

            target.Bool = false;

            Assert.False(data.BoolValue);
        }

        [Fact]
        public void Can_Set_Double_Negated_Value_In_Path()
        {
            var data = new ViewModel { Next = new() { BoolValue = true } };
            var target = CreateTargetWithSource(data, o => !!o.Next!.BoolValue, mode: BindingMode.TwoWay);

            target.Bool = false;

            Assert.False(data.Next.BoolValue);
        }
    }
}
