using Perspex.Controls;
using Perspex.Markup.Data;
using Perspex.Markup.Xaml.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests.Data
{
    public class BindingTests_Validation
    {
        public class Data : INotifyPropertyChanged
        {
            private string mustbeNonEmpty;

            public string MustBeNonEmpty
            {
                get { return mustbeNonEmpty; }
                set
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        throw new ArgumentException(nameof(value));
                    }
                    mustbeNonEmpty = value;
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [Fact]
        public void Disabled_Validation_Should_Not_Trigger_Validation_Change()
        {
            var source = new Data { MustBeNonEmpty = "Test" };
            var target = new TextBlock { DataContext = source };
            var binding = new Binding
            {
                Path = nameof(source.MustBeNonEmpty),
                Mode = Perspex.Data.BindingMode.TwoWay,
                ValidationMethods = ValidationMethods.None
            };
            target.Bind(TextBlock.TextProperty, binding);
            
            target.Text = "";

            Assert.True(target.ValidationStatus.IsValid);
        }

        [Fact]
        public void Enabled_Validation_Should_Trigger_Validation_Change()
        {
            var source = new Data { MustBeNonEmpty = "Test" };
            var target = new TextBlock { DataContext = source };
            var binding = new Binding
            {
                Path = nameof(source.MustBeNonEmpty),
                Mode = Perspex.Data.BindingMode.TwoWay,
                ValidationMethods = ValidationMethods.All
            };
            target.Bind(TextBlock.TextProperty, binding);
            
            target.Text = "";
            Assert.False(target.ValidationStatus.IsValid);
        }
    }
}
