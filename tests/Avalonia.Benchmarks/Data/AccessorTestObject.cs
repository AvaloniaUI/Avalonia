using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Avalonia.Benchmarks.Data
{
    internal class AccessorTestObject : INotifyPropertyChanged
    {
        private string _test;

        public string Test
        {
            get => _test;
            set
            {
                if (_test == value)
                {
                    return;
                }

                _test = value;

                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Execute()
        {
        }

        public void Execute(object p0)
        {
        }

        public void Execute(object p0, object p1)
        {
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
