using MiniMvvm;
using System;
using System.ComponentModel;
using System.Collections;

namespace BindingDemo.ViewModels
{
    public class IndeiErrorViewModel : ViewModelBase, INotifyDataErrorInfo
    {
        private int _maximum = 10;
        private int _value;
        private string _valueError;

        public IndeiErrorViewModel()
        {
            this.WhenAnyValue(x => x.Maximum, x => x.Value)
                .Subscribe(_ => UpdateErrors());
        }

        public bool HasErrors
        {
            get { throw new NotImplementedException(); }
        }

        public int Maximum
        {
            get { return _maximum; }
            set { this.RaiseAndSetIfChanged(ref _maximum, value); }
        }

        public int Value
        {
            get { return _value; }
            set { this.RaiseAndSetIfChanged(ref _value, value); }
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(Value):
                    return new[] { _valueError };
                default:
                    return null;
            }
        }

        private void UpdateErrors()
        {
            if (Value <= Maximum)
            {
                if (_valueError != null)
                {
                    _valueError = null;
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Value)));
                }
            }
            else
            {
                if (_valueError == null)
                {
                    _valueError = "Value must be less than Maximum";
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Value)));
                }
            }
        }
    }
}
