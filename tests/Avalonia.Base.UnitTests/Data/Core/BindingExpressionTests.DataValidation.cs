using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core;

public partial class BindingExpressionTests
{
    [Fact]
    public void Root_Null_Should_Update_Data_Validation()
    {
        var target = CreateTargetWithSource<ViewModel?, string?>(
            null,
            o => o!.StringValue,
            enableDataValidation: true);

        AssertBindingError(
            target,
            TargetClass.StringProperty,
            new BindingChainException("Binding Source is null.", "StringValue", "(source)"),
            BindingErrorType.Error);
    }

    [Fact]
    public void Null_Value_In_Path_Should_Update_Data_Validation()
    {
        var data = new { Foo = default(ViewModel) };
        var target = CreateTargetWithSource(
            data,
            o => o.Foo!.StringValue!.Length, 
            enableDataValidation: true);

        AssertBindingError(
            target,
            TargetClass.IntProperty,
            new BindingChainException("Value is null.", "Foo.StringValue.Length", "Foo"),
            BindingErrorType.Error);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Invalid_Double_String_Should_Update_Data_Validation()
    {
        var data = new ViewModel { StringValue = "foo" };
        var target = CreateTargetWithSource(
            data, 
            o => o.StringValue, 
            enableDataValidation: true,
            targetProperty: TargetClass.DoubleProperty);

        AssertBindingError(
            target,
            TargetClass.DoubleProperty,
            new InvalidCastException("Could not convert 'foo' (System.String) to 'System.Double'."),
            BindingErrorType.Error);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Invalid_Double_String_Should_Revert_To_FallbackValue()
    {
        var data = new ViewModel { StringValue = "foo" };
        var target = CreateTargetWithSource(
            data,
            o => o.StringValue,
            enableDataValidation: true,
            fallbackValue: 42.0,
            targetProperty: TargetClass.DoubleProperty);

        Assert.Equal(42.0, target.Double);
        AssertBindingError(
            target,
            TargetClass.DoubleProperty,
            new InvalidCastException("Could not convert 'foo' (System.String) to 'System.Double'."),
            BindingErrorType.Error);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Setter_Exception_Does_Not_Cause_DataValidationError_When_Data_Validation_Not_Enabled()
    {
        var data = new ExceptionViewModel { MustBePositive = 5 };

        var target = CreateTargetWithSource(
            data,
            o => o.MustBePositive,
            enableDataValidation: false,
            mode: BindingMode.TwoWay);

        target.Int = -5;

        // TODO: Should this be 5?
        Assert.Equal(-5, target.Int);

        Assert.Equal(5, data.MustBePositive);
        AssertNoError(target, TargetClass.IntProperty);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Setter_Exception_Updates_Data_Validation()
    {
        var data = new ExceptionViewModel { MustBePositive = 5 };

        var target = CreateTargetWithSource(
            data,
            o => o.MustBePositive,
            enableDataValidation: true,
            mode: BindingMode.TwoWay);

        target.Int = -5;

        // TODO: Should this be 5?
        Assert.Equal(-5, target.Int);

        Assert.Equal(5, data.MustBePositive);
        AssertBindingError(
            target,
            TargetClass.IntProperty,
            new ArgumentOutOfRangeException("value"),
            BindingErrorType.DataValidationError);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Indei_Validation_Does_Not_Subscribe_When_DataValidation_Not_Enabled()
    {
        var data = new IndeiViewModel { MustBePositive = 5 };
        var target = CreateTargetWithSource(
            data,
            o => o.MustBePositive,
            enableDataValidation: false,
            mode: BindingMode.TwoWay);

        Assert.Equal(0, data.ErrorsChangedSubscriptionCount);
    }

    [Fact]
    public void Indei_Validation_Subscribes_And_Unsubscribes()
    {
        var data = new IndeiViewModel { MustBePositive = 5 };
        var (target, expression) = CreateTargetAndExpression<IndeiViewModel, int>(
            o => o.MustBePositive,
            enableDataValidation: true,
            mode: BindingMode.TwoWay,
            source: data);

        Assert.Equal(1, data.ErrorsChangedSubscriptionCount);

        expression.Dispose();

        Assert.Equal(0, data.ErrorsChangedSubscriptionCount);
    }

    [Fact]
    public void Conversion_Errors_Update_Data_Validation_When_Writing_To_Source()
    {
        var data = new ViewModel { DoubleValue = 5.6 };
        var target = CreateTargetWithSource(
            data,
            o => o.DoubleValue,
            enableDataValidation: true,
            mode: BindingMode.TwoWay,
            targetProperty: TargetClass.TagProperty);

        // Can write a double value.
        target.Tag = 1.2;

        Assert.Equal(1.2, data.DoubleValue);
        AssertNoError(target, TargetClass.StringProperty);

        // Can write a string value and it gets converted to double.
        target.Tag = "3.4";

        Assert.Equal(3.4, data.DoubleValue);
        AssertNoError(target, TargetClass.StringProperty);

        // An invalid string value should result in an error. Not sure why this is considered
        // a data validation error rather than a binding error, but preserving semantics.
        target.Tag = "bar";

        Assert.Equal(3.4, data.DoubleValue);
        AssertBindingError(
            target,
            TargetClass.TagProperty,
            new InvalidCastException("Could not convert 'bar' (System.String) to System.Double."),
            BindingErrorType.DataValidationError);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Indei_Validation_Updates_Data_Validation_When_Writing_To_Source()
    {
        var data = new IndeiViewModel();
        var target = CreateTargetWithSource(
            data,
            o => o.MustBePositive,
            enableDataValidation: true,
            mode: BindingMode.TwoWay);

        Assert.Equal(0, target.Int);
        Assert.Equal(0, data.MustBePositive);
        AssertNoError(target, TargetClass.IntProperty);

        target.Int = 5;

        Assert.Equal(5, target.Int);
        Assert.Equal(5, data.MustBePositive);
        AssertNoError(target, TargetClass.IntProperty);

        target.Int = -5;

        Assert.Equal(-5, target.Int);
        Assert.Equal(-5, data.MustBePositive);
        AssertBindingError(target, TargetClass.IntProperty, new DataValidationException("Must be positive"), BindingErrorType.DataValidationError);

        target.Int = 5;

        Assert.Equal(5, target.Int);
        Assert.Equal(5, data.MustBePositive);
        AssertNoError(target, TargetClass.IntProperty);

        GC.KeepAlive(data);
    }

    [Fact]
    public void Does_Not_Subscribe_To_Indei_Of_Intermediate_Object_In_Chain()
    {
        var data = new IndeiContainerViewModel { Inner = new() };

        var target = CreateTargetWithSource(
            data,
            o => o.Inner!.MustBePositive,
            enableDataValidation: true,
            mode: BindingMode.TwoWay);

        // We may want to change this but I've never seen an example of data validation on an
        // intermediate object in a chain so for the moment I'm not sure what the result of 
        // validating such a thing should look like.
        Assert.Equal(0, data.ErrorsChangedSubscriptionCount);
        Assert.Equal(1, data.Inner.ErrorsChangedSubscriptionCount);
    }

    [Fact]
    public void Updates_Data_Validation_For_Null_Value_In_Property_Chain()
    {
        var data = new IndeiContainerViewModel();
        var target = CreateTargetWithSource(
            data,
            o => o.Inner!.MustBePositive,
            enableDataValidation: true,
            mode: BindingMode.TwoWay);

        AssertBindingError(
            target, 
            TargetClass.IntProperty,
            new BindingChainException("Value is null.", "Inner.MustBePositive", "Inner"),
            BindingErrorType.Error);

        GC.KeepAlive(data);
    }

    public class ExceptionViewModel : NotifyingBase
    {
        private int _mustBePositive;

        public int MustBePositive
        {
            get { return _mustBePositive; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _mustBePositive = value;
                RaisePropertyChanged();
            }
        }
    }

    private class IndeiViewModel : IndeiBase
    {
        private int _mustBePositive;
        private Dictionary<string, IList<string>> _errors = new Dictionary<string, IList<string>>();

        public int MustBePositive
        {
            get { return _mustBePositive; }
            set
            {
                _mustBePositive = value;
                RaisePropertyChanged();

                if (value >= 0)
                {
                    _errors.Remove(nameof(MustBePositive));
                    RaiseErrorsChanged(nameof(MustBePositive));
                }
                else
                {
                    _errors[nameof(MustBePositive)] = new[] { "Must be positive" };
                    RaiseErrorsChanged(nameof(MustBePositive));
                }
            }
        }

        public override bool HasErrors => _mustBePositive >= 0;

        public override IEnumerable? GetErrors(string propertyName)
        {
            IList<string>? result;
            _errors.TryGetValue(propertyName, out result);
            return result;
        }
    }

    private class IndeiContainerViewModel : IndeiBase
    {
        private IndeiViewModel? _inner;

        public IndeiViewModel? Inner
        {
            get { return _inner; }
            set { _inner = value; RaisePropertyChanged(); }
        }

        public override bool HasErrors => false;
        public override IEnumerable? GetErrors(string propertyName) => null;
    }

    private static void AssertNoError(TargetClass target, AvaloniaProperty property)
    {
        Assert.False(target.BindingNotifications.TryGetValue(property, out var notification));
    }

    private static void AssertBindingError(
        TargetClass target,
        AvaloniaProperty property,
        Exception expectedException,
        BindingErrorType errorType)
    {
        Assert.True(target.BindingNotifications.TryGetValue(property, out var notification));
        Assert.Equal(errorType, notification.ErrorType);
        Assert.NotNull(notification.Error);
        Assert.IsType(expectedException.GetType(), notification.Error);
        Assert.Equal(expectedException.Message, notification.Error.Message);
    }
}
