using System;
using System.Collections;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Markup.UnitTests.Data
{
    public class BindingTests_DataValidation
    {
        public abstract class TestBase<T>
            where T : AvaloniaProperty<int>
        {
            [Fact]
            public void Setter_Exception_Causes_DataValidation_Error()
            {
                var (target, property) = CreateTarget();
                var binding = new Binding(nameof(ExceptionValidatingModel.Value))
                {
                    Mode = BindingMode.TwoWay
                };

                target.DataContext = new ExceptionValidatingModel();
                target.Bind(property, binding);

                Assert.Equal(20, target.GetValue(property));

                target.SetValue(property, 200);

                Assert.Equal(200, target.GetValue(property));
                Assert.IsType<ArgumentOutOfRangeException>(target.DataValidationError);

                target.SetValue(property, 10);

                Assert.Equal(10, target.GetValue(property));
                Assert.Null(target.DataValidationError);
            }

            [Fact]
            public void Indei_Error_Causes_DataValidation_Error()
            {
                var (target, property) = CreateTarget();
                var binding = new Binding(nameof(IndeiValidatingModel.Value))
                {
                    Mode = BindingMode.TwoWay
                };

                target.DataContext = new IndeiValidatingModel();
                target.Bind(property, binding);

                Assert.Equal(20, target.GetValue(property));

                target.SetValue(property, 200);

                Assert.Equal(200, target.GetValue(property));
                Assert.IsType<DataValidationException>(target.DataValidationError);
                Assert.Equal("Invalid value: 200.", target.DataValidationError?.Message);

                target.SetValue(property, 10);

                Assert.Equal(10, target.GetValue(property));
                Assert.Null(target.DataValidationError);
            }

            [Fact]
            public void Disposing_Binding_Subscription_Clears_DataValidation()
            {
                var (target, property) = CreateTarget();
                var binding = new Binding(nameof(ExceptionValidatingModel.Value))
                {
                    Mode = BindingMode.TwoWay
                };

                target.DataContext = new IndeiValidatingModel
                {
                    Value = 200,
                };
                
                var sub = target.Bind(property, binding);

                Assert.Equal(200, target.GetValue(property));
                Assert.IsType<DataValidationException>(target.DataValidationError);

                sub.Dispose();

                Assert.Null(target.DataValidationError);
            }

            private protected abstract (DataValidationTestControl, T) CreateTarget();
        }

        public class DirectPropertyTests : TestBase<DirectPropertyBase<int>>
        {
            private protected override (DataValidationTestControl, DirectPropertyBase<int>) CreateTarget()
            {
                return (new ValidatedDirectPropertyClass(), ValidatedDirectPropertyClass.ValueProperty);
            }
        }

        public class StyledPropertyTests : TestBase<StyledProperty<int>>
        {
            [Fact]
            public void Style_Binding_Supports_Data_Validation()
            {
                var (target, property) = CreateTarget();
                var binding = new Binding(nameof(IndeiValidatingModel.Value))
                {
                    Mode = BindingMode.TwoWay
                };

                var model = new IndeiValidatingModel();
                var root = new TestRoot
                {
                    DataContext = model,
                    Styles =
                    {
                        new Style(x => x.Is<DataValidationTestControl>())
                        {
                            Setters =
                            {
                                new Setter(property, binding)
                            }
                        }
                    },
                    Child = target,
                };

                root.LayoutManager.ExecuteInitialLayoutPass();

                Assert.Equal(20, target.GetValue(property));

                model.Value = 200;

                Assert.Equal(200, target.GetValue(property));
                Assert.IsType<DataValidationException>(target.DataValidationError);
                Assert.Equal("Invalid value: 200.", target.DataValidationError?.Message);

                model.Value = 10;

                Assert.Equal(10, target.GetValue(property));
                Assert.Null(target.DataValidationError);
            }

            [Fact]
            public void Style_With_Activator_Binding_Supports_Data_Validation()
            {
                var (target, property) = CreateTarget();
                var binding = new Binding(nameof(IndeiValidatingModel.Value))
                {
                    Mode = BindingMode.TwoWay
                };

                var model = new IndeiValidatingModel
                {
                    Value = 200,
                };

                var root = new TestRoot
                {
                    DataContext = model,
                    Styles =
                    {
                        new Style(x => x.Is<DataValidationTestControl>().Class("foo"))
                        {
                            Setters =
                            {
                                new Setter(property, binding)
                            }
                        }
                    },
                    Child = target,
                };

                root.LayoutManager.ExecuteInitialLayoutPass();
                target.Classes.Add("foo");

                Assert.Equal(200, target.GetValue(property));
                Assert.IsType<DataValidationException>(target.DataValidationError);
                Assert.Equal("Invalid value: 200.", target.DataValidationError?.Message);

                target.Classes.Remove("foo");
                Assert.Equal(0, target.GetValue(property));
                Assert.Null(target.DataValidationError);

                target.Classes.Add("foo");
                Assert.IsType<DataValidationException>(target.DataValidationError);
                Assert.Equal("Invalid value: 200.", target.DataValidationError?.Message);

                model.Value = 10;

                Assert.Equal(10, target.GetValue(property));
                Assert.Null(target.DataValidationError);
            }

            [Fact]
            public void Data_Validation_Can_Switch_Between_Style_And_LocalValue_Binding()
            {
                var (target, property) = CreateTarget();
                var model1 = new IndeiValidatingModel { Value = 200 };
                var model2 = new IndeiValidatingModel { Value = 300 };
                var binding1 = new Binding(nameof(IndeiValidatingModel.Value));
                var binding2 = new Binding(nameof(IndeiValidatingModel.Value)) { Source = model2 };

                var root = new TestRoot
                {
                    DataContext = model1,
                    Styles =
                    {
                        new Style(x => x.Is<DataValidationTestControl>())
                        {
                            Setters =
                            {
                                new Setter(property, binding1)
                            }
                        }
                    },
                    Child = target,
                };

                root.LayoutManager.ExecuteInitialLayoutPass();

                Assert.Equal(200, target.GetValue(property));
                Assert.IsType<DataValidationException>(target.DataValidationError);
                Assert.Equal("Invalid value: 200.", target.DataValidationError?.Message);

                var sub = target.Bind(property, binding2);
                Assert.Equal(300, target.GetValue(property));
                Assert.Equal("Invalid value: 300.", target.DataValidationError?.Message);

                sub.Dispose();
                Assert.Equal(200, target.GetValue(property));
                Assert.IsType<DataValidationException>(target.DataValidationError);
                Assert.Equal("Invalid value: 200.", target.DataValidationError?.Message);
            }


            [Fact]
            public void Data_Validation_Can_Switch_Between_Style_And_StyleTrigger_Binding()
            {
                var (target, property) = CreateTarget();
                var model1 = new IndeiValidatingModel { Value = 200 };
                var model2 = new IndeiValidatingModel { Value = 300 };
                var binding1 = new Binding(nameof(IndeiValidatingModel.Value));
                var binding2 = new Binding(nameof(IndeiValidatingModel.Value)) { Source = model2 };

                var root = new TestRoot
                {
                    DataContext = model1,
                    Styles =
                    {
                        new Style(x => x.Is<DataValidationTestControl>())
                        {
                            Setters =
                            {
                                new Setter(property, binding1)
                            }
                        },
                        new Style(x => x.Is<DataValidationTestControl>().Class("foo"))
                        {
                            Setters =
                            {
                                new Setter(property, binding2)
                            }
                        },
                    },
                    Child = target,
                };

                root.LayoutManager.ExecuteInitialLayoutPass();

                Assert.Equal(200, target.GetValue(property));
                Assert.IsType<DataValidationException>(target.DataValidationError);
                Assert.Equal("Invalid value: 200.", target.DataValidationError?.Message);

                target.Classes.Add("foo");
                Assert.Equal(300, target.GetValue(property));
                Assert.Equal("Invalid value: 300.", target.DataValidationError?.Message);

                target.Classes.Remove("foo");
                Assert.Equal(200, target.GetValue(property));
                Assert.IsType<DataValidationException>(target.DataValidationError);
                Assert.Equal("Invalid value: 200.", target.DataValidationError?.Message);
            }

            private protected override (DataValidationTestControl, StyledProperty<int>) CreateTarget()
            {
                return (new ValidatedStyledPropertyClass(), ValidatedStyledPropertyClass.ValueProperty);
            }
        }

        internal class DataValidationTestControl : Control
        {
            public Exception? DataValidationError { get; protected set; }
        }

        private class ValidatedStyledPropertyClass : DataValidationTestControl
        {
            public static readonly StyledProperty<int> ValueProperty =
                AvaloniaProperty.Register<ValidatedStyledPropertyClass, int>(
                    "Value",
                    enableDataValidation: true);

            public int Value
            {
                get => GetValue(ValueProperty);
                set => SetValue(ValueProperty, value);
            }

            protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
            {
                if (property == ValueProperty)
                {
                    DataValidationError = state.HasAnyFlag(BindingValueType.DataValidationError) ? error : null;
                }
            }
        }

        private class ValidatedDirectPropertyClass : DataValidationTestControl
        {
            public static readonly DirectProperty<ValidatedDirectPropertyClass, int> ValueProperty =
                AvaloniaProperty.RegisterDirect<ValidatedDirectPropertyClass, int>(
                    "Value",
                    o => o.Value,
                    (o, v) => o.Value = v,
                    enableDataValidation: true);

            private int _value;

            public int Value
            {
                get => _value;
                set => SetAndRaise(ValueProperty, ref _value, value);
            }

            protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
            {
                if (property == ValueProperty)
                {
                    DataValidationError = state.HasAnyFlag(BindingValueType.DataValidationError) ? error : null;
                }
            }
        }

        private class ExceptionValidatingModel
        {
            public const int MaxValue = 100;
            private int _value = 20;

            public int Value
            {
                get => _value;
                set
                {
                    if (value > MaxValue)
                        throw new ArgumentOutOfRangeException(nameof(value));
                    _value = value;
                }
            }
        }

        private class IndeiValidatingModel : INotifyDataErrorInfo
        {
            public const int MaxValue = 100;
            private bool _hasErrors;
            private int _value = 20;

            public int Value
            {
                get => _value;
                set
                {
                    _value = value;
                    HasErrors = value > MaxValue;
                }
            }

            public bool HasErrors 
            {
                get => _hasErrors;
                private set
                {
                    if (_hasErrors != value)
                    {
                        _hasErrors = value;
                        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Value)));
                    }
                }
            }

            public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

            public IEnumerable GetErrors(string? propertyName)
            {
                if (propertyName == nameof(Value) && _value > MaxValue)
                    yield return $"Invalid value: {_value}.";
            }
        }
    }
}
