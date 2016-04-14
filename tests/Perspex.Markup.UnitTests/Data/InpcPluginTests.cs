// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Markup.Data.Plugins;
using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xunit;

namespace Perspex.Markup.UnitTests.Data
{
    public class InpcPluginTests
    {
        private class InpcTest : INotifyPropertyChanged, INotifyDataErrorInfo
        {
            private int noValidationTest;

            public int NoValidationTest
            {
                get { return noValidationTest; }
                set
                {
                    noValidationTest = value;
                    NotifyPropertyChanged();
                }
            }

            public bool HasErrors
            {
                get
                {
                    return NonNegative < 0;
                }
            }

            private int nonNegative;

            public int NonNegative
            {
                get { return nonNegative; }
                set
                {
                    var old = nonNegative;
                    nonNegative = value;
                    NotifyPropertyChanged();
                    if (old * value < 0) // If signs are different
                    {
                        NotifyErrorsChanged();
                    }
                }
            }


            public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
            public event PropertyChangedEventHandler PropertyChanged;

            public IEnumerable GetErrors(string propertyName)
            {
                if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(NonNegative))
                {
                    if (NonNegative < 0)
                    {
                        yield return "Invalid Value";
                    }
                }
            }

            private void NotifyPropertyChanged([CallerMemberName] string property = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
            }

            private void NotifyErrorsChanged([CallerMemberName] string property = "")
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
            }
        }

        [Fact]
        public void Calls_Change_Callback_When_Value_Changes()
        {
            var plugin = new InpcPropertyAccessorPlugin();
            var source = new InpcTest { NoValidationTest = 0 };
            var changeFired = false;
            var accessor = plugin.Start(new WeakReference(source), nameof(InpcTest.NoValidationTest), _ => changeFired = true, _ => { });
            source.NoValidationTest = 1;

            Assert.True(changeFired);
        }

        [Fact]
        public void ValidationChanged_Does_Not_Fire_When_NonValidated_Value_Changes()
        {
            var plugin = new InpcPropertyAccessorPlugin();
            var source = new InpcTest { NoValidationTest = 0 };
            var validationFired = false;
            plugin.Start(new WeakReference(source), nameof(InpcTest.NoValidationTest), _ => { }, _ => validationFired = true);
            source.NoValidationTest = 1;

            Assert.False(validationFired);
        }
        
        [Fact]
        public void ValidationChanged_Does_Not_Fire_When_Validation_Does_Not_Change()
        {
            var plugin = new InpcPropertyAccessorPlugin();
            var source = new InpcTest { NonNegative = 3 };
            var validationFired = false;
            plugin.Start(new WeakReference(source), nameof(InpcTest.NonNegative), _ => { }, _ => validationFired = true);
            source.NonNegative = 5;

            Assert.False(validationFired);
        }

        [Fact]
        public void ValidationChanged_Fires_On_Start_If_Has_Errors()
        {
            var plugin = new InpcPropertyAccessorPlugin();
            var source = new InpcTest { NonNegative = -5 };

            Assert.True(source.HasErrors);

            var validationFired = false;
            plugin.Start(new WeakReference(source), nameof(InpcTest.NonNegative), _ => { }, _ => validationFired = true);
            Assert.True(validationFired);
        }



        [Fact]
        public void ValidationChanged_Fires_When_Validation_Changes()
        {
            var plugin = new InpcPropertyAccessorPlugin();
            var source = new InpcTest { NonNegative = 5 };
            var validationFired = false;
            plugin.Start(new WeakReference(source), nameof(InpcTest.NonNegative), _ => { }, _ => validationFired = true);
            source.NonNegative = -1;
            Assert.True(validationFired);
        }
    }
}
