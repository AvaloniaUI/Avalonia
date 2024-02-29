using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Avalonia.Data;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core;

public partial class BindingExpressionTests
{
    [Theory]
    [MemberData(nameof(GetIsDataBound_Data))]
    public void BindingOperations_IsDataBound(AvaloniaObject target, AvaloniaProperty property, bool expected)
    {
        Assert.Equal(expected, target.IsDataBound(property));
    }

    public static IEnumerable<object[]> GetIsDataBound_Data()
    {
        yield return new object[]
        {
           new TargetClass()
           {
               [~TargetClass.StringProperty] = new Binding(nameof(ViewModel.StringValue), BindingMode.TwoWay)
               {
                   Source = new ViewModel()
                   {
                       StringValue = "1",
                   },
               }
           },
           TargetClass.StringProperty,
           true,
        };
        yield return new object[]
        {
           new TargetClass()
           {
               [~TargetClass.StringProperty] = new Binding(nameof(ViewModel.StringValue), BindingMode.TwoWay)
               {
                   Source = new ViewModel()
                   {
                       StringValue = "1",
                   },
               }
           },
           TargetClass.ReadOnlyStringProperty,
           false,
        };
        yield return new object[]
        {
           new TargetClass()
           {
               [~AttachedProperties.AttachedStringProperty] = new Binding(nameof(ViewModel.StringValue), BindingMode.TwoWay)
               {
                   Source = new ViewModel()
                   {
                       StringValue = "1",
                   },
               }
           },
           AttachedProperties.AttachedStringProperty,
           true,
        };
        yield return new object[]
        {
            new TargetClass()
            {
                [~TargetClass.StringProperty] = (new  BehaviorSubject<string>("foo")) .ToBinding(),
            },
            TargetClass.StringProperty,
            true,
        };
        yield return new object[]
        {
           new TargetClass()
           {
               [~TargetClass.StringProperty] = Task.FromResult("foo").ToObservable().ToBinding(),
           },
           TargetClass.StringProperty,
           true,
        };
    }

}
