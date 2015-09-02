using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Perspex.Xaml.Base.UnitTest
{
    class ViewModelMock : INotifyPropertyChanged
    {
        private string str;
        private int intProp;

        public int IntProp
        {
            get { return intProp; }
            set
            {
                intProp = value;
                OnPropertyChanged();
            }
        }

        public string StrProp
        {
            get { return str; }
            set
            {
                str = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}