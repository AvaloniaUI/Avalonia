using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Harfbuzz;
using Avalonia.Headless;
using Avalonia.Markup.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBoxTests_DataValidation : ScopedTestBase
    {
        [Fact]
        public void Setter_Exceptions_Should_Set_Error_Pseudoclass()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    DataContext = new ExceptionTest(),
                    [!TextBox.TextProperty] = new Binding(nameof(ExceptionTest.LessThan10), BindingMode.TwoWay),
                    Template = CreateTemplate(),
                };

                target.ApplyTemplate();

                Assert.DoesNotContain(":error", target.Classes);
                target.Text = "20";
                Assert.Contains(":error", target.Classes);
                target.Text = "1";
                Assert.DoesNotContain(":error", target.Classes);
            }
        }

        [Fact]
        public void Setter_Exceptions_Should_Set_DataValidationErrors_Errors()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    DataContext = new ExceptionTest(),
                    [!TextBox.TextProperty] = new Binding(nameof(ExceptionTest.LessThan10), BindingMode.TwoWay),
                    Template = CreateTemplate(),
                };

                target.ApplyTemplate();

                Assert.Null(DataValidationErrors.GetErrors(target));
                target.Text = "20";

                var errors = DataValidationErrors.GetErrors(target);
                Assert.NotNull(errors);
                var error = Assert.Single(errors);
                Assert.IsType<InvalidOperationException>(error);
                target.Text = "1";
                Assert.Null(DataValidationErrors.GetErrors(target));
            }
        }

        [Fact]
        public void Setter_Exceptions_Should_Be_Converter_If_Error_Converter_Set()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    DataContext = new ExceptionTest(),
                    [!TextBox.TextProperty] = new Binding(nameof(ExceptionTest.LessThan10), BindingMode.TwoWay),
                    Template = CreateTemplate()  
                };
                DataValidationErrors.SetErrorConverter(target, err => "Error: " + err);

                target.ApplyTemplate();

                target.Text = "20";

                var errors = DataValidationErrors.GetErrors(target);
                Assert.NotNull(errors);
                var error = Assert.IsType<string>(Assert.Single(errors));
                Assert.StartsWith("Error: ", error);
            }
        }
        
        [Fact]
        public void Setter_Exceptions_Should_Set_DataValidationErrors_HasErrors()
        {
            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    DataContext = new ExceptionTest(),
                    [!TextBox.TextProperty] = new Binding(nameof(ExceptionTest.LessThan10), BindingMode.TwoWay),
                    Template = CreateTemplate(),
                };

                target.ApplyTemplate();

                Assert.False(DataValidationErrors.GetHasErrors(target));
                target.Text = "20";
                Assert.True(DataValidationErrors.GetHasErrors(target));
                target.Text = "1";
                Assert.False(DataValidationErrors.GetHasErrors(target));
            }
        }

        [Fact]
        public void CompiledBindings_TypeConverter_Exceptions_Should_Set_DataValidationErrors_HasErrors()
        {
            var path = new CompiledBindingPathBuilder()
            .Property(
                new ClrPropertyInfo(
                    nameof(ExceptionTest.LessThan10),
                    target => ((ExceptionTest)target).LessThan10,
                    (target, value) => ((ExceptionTest)target).LessThan10 = (int)value!,
                    typeof(int)),
                PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
            .Build();

            using (UnitTestApplication.Start(Services))
            {
                var target = new TextBox
                {
                    DataContext = new ExceptionTest(),
                    [!TextBox.TextProperty] = new CompiledBindingExtension
                    {
                        Source = new ExceptionTest(),
                        Path = path,
                        Mode = BindingMode.TwoWay
                    },
                    Template = CreateTemplate(),
                };

                target.ApplyTemplate();

                target.Text = "a";
                Assert.True(DataValidationErrors.GetHasErrors(target));
            }
        }

        private static TestServices Services => TestServices.MockThreadingInterface.With(
            standardCursorFactory: Mock.Of<ICursorFactory>(),
            textShaperImpl: new HarfBuzzTextShaper(),
            fontManagerImpl: new HeadlessFontManagerStub());

        private static IControlTemplate CreateTemplate()
        {
            return new FuncControlTemplate<TextBox>((control, scope) =>
                new TextPresenter
                {
                    Name = "PART_TextPresenter",
                    [!!TextPresenter.TextProperty] = new Binding
                    {
                        Path = "Text",
                        Mode = BindingMode.TwoWay,
                        Priority = BindingPriority.Template,
                        RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                    },
                }.RegisterInNameScope(scope));
        }

        private class ExceptionTest
        {
            private int _lessThan10;

            public int LessThan10
            {
                get { return _lessThan10; }
                set
                {
                    if (value < 10)
                    {
                        _lessThan10 = value;
                    }
                    else
                    {
                        throw new InvalidOperationException("More than 10.");
                    }
                }
            }
        }

        private class IndeiTest : INotifyDataErrorInfo
        {
            private int _lessThan10;
            private Dictionary<string, IList<string>> _errors = new Dictionary<string, IList<string>>();

            public int LessThan10
            {
                get { return _lessThan10; }
                set
                {
                    if (value < 10)
                    {
                        _lessThan10 = value;
                        _errors.Remove(nameof(LessThan10));
                        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(LessThan10)));
                    }
                    else
                    {
                        _errors[nameof(LessThan10)] = new[] { "More than 10" };
                        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(LessThan10)));
                    }
                }
            }

            public bool HasErrors => _lessThan10 >= 10;

            public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

            public IEnumerable GetErrors(string? propertyName)
            {
                if (propertyName is not null && _errors.TryGetValue(propertyName, out var result))
                    return result;
                return Array.Empty<string?>();
            }
        }
    }
}
