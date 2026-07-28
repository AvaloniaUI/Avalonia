using Avalonia.UnitTests;

namespace Avalonia.Markup.Xaml.UnitTests
{
    public class TestViewModel : NotifyingBase
    {
        private string? _string;
        private int _integer;
        private TestViewModel? _child;
        private bool _boolean;

        public int Integer
        {
            get => _integer;
            set
            {
                _integer = value;
                RaisePropertyChanged();
            }
        }

        public string? String
        {
            get => _string;
            set
            {
                _string = value;
                RaisePropertyChanged();
            }
        }

        public TestViewModel? Child
        {
            get => _child;
            set
            {
                _child = value;
                RaisePropertyChanged();
            }
        }

        public bool Boolean
        {
            get => _boolean;
            set
            {
                _boolean = value;
                RaisePropertyChanged();
            }
        }
    }
}
