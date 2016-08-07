// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Markup.Xaml.Data;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBoxTests_ValidationState
    {
        [Fact]
        public void Setter_Exceptions_Should_Set_ValidationState()
        {
            using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
            {
                var target = new TextBox();
                var binding = new Binding(nameof(ExceptionTest.LessThan10));
                binding.Source = new ExceptionTest();
                binding.EnableValidation = true;
                target.Bind(TextBox.TextProperty, binding);

                Assert.True(false);
                //Assert.True(target.ValidationStatus.IsValid);
                //target.Text = "20";
                //Assert.False(target.ValidationStatus.IsValid);
                //target.Text = "1";
                //Assert.True(target.ValidationStatus.IsValid);
            }
        }

        [Fact(Skip = "TODO: Not yet passing")]
        public void Unconvertable_Value_Should_Set_ValidationState()
        {
            using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
            {
                var target = new TextBox();
                var binding = new Binding(nameof(ExceptionTest.LessThan10));
                binding.Source = new ExceptionTest();
                binding.EnableValidation = true;
                target.Bind(TextBox.TextProperty, binding);

                Assert.True(false);
                //Assert.True(target.ValidationStatus.IsValid);
                //target.Text = "foo";
                //Assert.False(target.ValidationStatus.IsValid);
                //target.Text = "1";
                //Assert.True(target.ValidationStatus.IsValid);
            }
        }

        [Fact]
        public void Indei_Should_Set_ValidationState()
        {
            using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
            {
                var target = new TextBox();
                var binding = new Binding(nameof(ExceptionTest.LessThan10));
                binding.Source = new IndeiTest();
                binding.EnableValidation = true;
                target.Bind(TextBox.TextProperty, binding);

                Assert.True(false);
                //Assert.True(target.ValidationStatus.IsValid);
                //target.Text = "20";
                //Assert.False(target.ValidationStatus.IsValid);
                //target.Text = "1";
                //Assert.True(target.ValidationStatus.IsValid);
            }
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
