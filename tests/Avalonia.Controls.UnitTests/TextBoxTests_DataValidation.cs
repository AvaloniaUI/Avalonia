// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Data;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBoxTests_DataValidation
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
        public void Setter_Exceptions_Should_Set_DataValidationErrors()
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

                Assert.Null(target.DataValidationErrors);
                target.Text = "20";
                Assert.Single(target.DataValidationErrors);
                Assert.IsType<InvalidOperationException>(target.DataValidationErrors.Single());
                target.Text = "1";
                Assert.Null(target.DataValidationErrors);
            }
        }

        private static TestServices Services => TestServices.MockThreadingInterface.With(
            standardCursorFactory: Mock.Of<IStandardCursorFactory>());

        private IControlTemplate CreateTemplate()
        {
            return new FuncControlTemplate<TextBox>(control =>
                new TextPresenter
                {
                    Name = "PART_TextPresenter",
                    [!!TextPresenter.TextProperty] = new Binding
                    {
                        Path = "Text",
                        Mode = BindingMode.TwoWay,
                        Priority = BindingPriority.TemplatedParent,
                        RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                    },
                });
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

            public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

            public IEnumerable GetErrors(string propertyName)
            {
                IList<string> result;
                _errors.TryGetValue(propertyName, out result);
                return result;
            }
        }
    }
}
