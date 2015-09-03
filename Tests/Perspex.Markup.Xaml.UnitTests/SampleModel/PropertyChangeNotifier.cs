namespace Perspex.Xaml.Base.UnitTest.SampleModel
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public abstract class PropertyChangeNotifier : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}