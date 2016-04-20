using Perspex.Data;
using Perspex.Markup.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Perspex.Markup.UnitTests.Data
{
    public class ExpressionObserverTests_Validation
    {
        public class Data : INotifyPropertyChanged
        {
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
        public void Enabled_Validation_Sends_ValidationUpdate()
        {
            var data = new Data { MustBePositive = 5 };
            var observer = new ExpressionObserver(data, nameof(data.MustBePositive), ValidationMethods.All);
            var validationMessageFound = false;
            observer.Where(o => o is IValidationStatus).Subscribe(_ => validationMessageFound = true);
            observer.SetValue(-5);
            Assert.True(validationMessageFound);
        }

        [Fact]
        public void Disabled_Validation_Does_Not_Send_ValidationUpdate()
        {
            var data = new Data { MustBePositive = 5 };
            var observer = new ExpressionObserver(data, nameof(data.MustBePositive), ValidationMethods.None);
            var validationMessageFound = false;
            observer.Where(o => o is IValidationStatus).Subscribe(_ => validationMessageFound = true);
            observer.SetValue(-5);
            Assert.False(validationMessageFound);
        }

        [Fact]
        public void Disabled_Validation_Of_Current_Validation_Type_Does_Not_Send_ValidationUpdate()
        {
            var data = new Data { MustBePositive = 5 };
            var observer = new ExpressionObserver(data, nameof(data.MustBePositive), ~ValidationMethods.Exceptions);
            var validationMessageFound = false;
            observer.Where(o => o is IValidationStatus).Subscribe(_ => validationMessageFound = true);
            observer.SetValue(-5);
            Assert.False(validationMessageFound);
        }
    }
}
