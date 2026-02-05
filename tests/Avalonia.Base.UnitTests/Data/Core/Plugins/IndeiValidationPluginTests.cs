using System;
using System.Collections;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core.Plugins
{
    public class IndeiValidationPluginTests
    {
        [Fact]
        public void Validates_Values()
        {
            var data = new Data { Maximum = 5 };
            var validatorPlugin = new IndeiDataValidationPlugin();
            var validator = validatorPlugin.Start(data, nameof(data.Value));

            data.Value = 5;
            Assert.Null(validator.GetDataValidationError());

            data.Value = 6;
            var error = Assert.IsType<DataValidationException>(validator.GetDataValidationError());
            Assert.Equal("Must be less than Maximum", error.Message);

            data.Maximum = 10;
            Assert.Null(validator.GetDataValidationError());

            data.Maximum = 5;
            error = Assert.IsType<DataValidationException>(validator.GetDataValidationError());
            Assert.Equal("Must be less than Maximum", error.Message);
        }

        [Fact]
        public void Raises_Validation_Events()
        {
            var data = new Data { Maximum = 5 };
            var validatorPlugin = new IndeiDataValidationPlugin();
            var validator = validatorPlugin.Start(data, nameof(data.Value));
            Exception? exception = null;

            Assert.True(validator.RaisesEvents);
            validator.DataValidationChanged += (s, e) => exception = validator.GetDataValidationError();

            data.Value = 5;
            Assert.Null(exception);

            data.Value = 6;
            var error = Assert.IsType<DataValidationException>(exception);
            Assert.Equal("Must be less than Maximum", error.Message);

            data.Maximum = 10;
            Assert.Null(exception);

            data.Maximum = 5;
            error = Assert.IsType<DataValidationException>(exception);
            Assert.Equal("Must be less than Maximum", error.Message);
        }

        [Fact]
        public void Subscribes_And_Unsubscribes()
        {
            var data = new Data { Maximum = 5 };
            var validatorPlugin = new IndeiDataValidationPlugin();
            var validator = validatorPlugin.Start(data, nameof(data.Value));

            Assert.Equal(0, data.ErrorsChangedSubscriptionCount);
            validator.DataValidationChanged += Handler;
            Assert.Equal(1, data.ErrorsChangedSubscriptionCount);
            validator.DataValidationChanged -= Handler;

            // Forces WeakEvent compact
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
            Assert.Equal(0, data.ErrorsChangedSubscriptionCount);
            
            static void Handler(object? sender, EventArgs e) { }
        }

        internal class Data : IndeiBase
        {
            private int _value;
            private int _maximum;
            private string? _error;

            public override bool HasErrors => _error != null;

            public int Value
            {
                get { return _value; }
                set
                {
                    _value = value;
                    RaisePropertyChanged();
                    UpdateError();
                }
            }

            public int Maximum
            {
                get { return _maximum; }
                set
                {
                    _maximum = value;
                    UpdateError();
                }
            }

            public override IEnumerable GetErrors(string? propertyName)
            {
                if (propertyName == nameof(Value) && _error != null)
                {
                    return new[] { _error };
                }

                return Array.Empty<string?>();
            }

            private void UpdateError()
            {
                if (_value <= _maximum)
                {
                    if (_error != null)
                    {
                        _error = null;
                        RaiseErrorsChanged(nameof(Value));
                    }
                }
                else
                {
                    if (_error == null)
                    {
                        _error = "Must be less than Maximum";
                        RaiseErrorsChanged(nameof(Value));
                    }
                }
            }
        }
    }
}
