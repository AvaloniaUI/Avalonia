namespace Perspex.Xaml.Base.UnitTest.SampleModel
{
    public class Level3 : PropertyChangeNotifier
    {
        private int property = 10;

        public int Property
        {
            get { return property; }
            set
            {
                property = value;
                OnPropertyChanged();
            }
        }
    }
}