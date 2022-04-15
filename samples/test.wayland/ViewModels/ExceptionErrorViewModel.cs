using MiniMvvm;
using System;

namespace BindingDemo.ViewModels
{
    public class ExceptionErrorViewModel : ViewModelBase
    {
        private int _lessThan10;

        public int LessThan10
        {
            get { return _lessThan10; }
            set
            {
                if (value < 10)
                {
                    this.RaiseAndSetIfChanged(ref _lessThan10, value);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be less than 10.");
                }
            }
        }
    }
}
