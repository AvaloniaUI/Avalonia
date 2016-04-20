// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Data;
using Perspex.Markup.Data.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Perspex.Markup.UnitTests.Data
{
    public class ExceptionValidatorTests
    {
        public class Data : INotifyPropertyChanged
        {
            private int nonValidated;

            public int NonValidated
            {
                get { return nonValidated; }
                set { nonValidated = value; NotifyPropertyChanged(); }
            }

            private int mustBePositive;

            public int MustBePositive
            {
                get { return mustBePositive; }
                set
                {
                    if (value <= 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }
                    mustBePositive = value;
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [Fact]
        public void Setting_Non_Validating_Triggers_Validation()
        {
            var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
            var validatorPlugin = new ExceptionValidationPlugin();
            var data = new Data();
            var accessor = inpcAccessorPlugin.Start(new WeakReference(data), nameof(data.NonValidated), _ => { });
            IValidationStatus status = null;
            var validator = validatorPlugin.Start(new WeakReference(data), nameof(data.NonValidated), accessor, s => status = s);

            validator.SetValue(5, BindingPriority.LocalValue);

            Assert.NotNull(status);
        }

        [Fact]
        public void Setting_Validating_Property_To_Valid_Value_Returns_Successful_ValidationStatus()
        {
            var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
            var validatorPlugin = new ExceptionValidationPlugin();
            var data = new Data();
            var accessor = inpcAccessorPlugin.Start(new WeakReference(data), nameof(data.MustBePositive), _ => { });
            IValidationStatus status = null;
            var validator = validatorPlugin.Start(new WeakReference(data), nameof(data.MustBePositive), accessor, s => status = s);

            validator.SetValue(5, BindingPriority.LocalValue);

            Assert.True(status.IsValid);
        }



        [Fact]
        public void Setting_Validating_Property_To_Invalid_Value_Returns_Failed_ValidationStatus()
        {
            var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
            var validatorPlugin = new ExceptionValidationPlugin();
            var data = new Data();
            var accessor = inpcAccessorPlugin.Start(new WeakReference(data), nameof(data.MustBePositive), _ => { });
            IValidationStatus status = null;
            var validator = validatorPlugin.Start(new WeakReference(data), nameof(data.MustBePositive), accessor, s => status = s);

            validator.SetValue(-5, BindingPriority.LocalValue);

            Assert.False(status.IsValid);
        }
    }
}
