namespace Perspex.Xaml.Base.UnitTest.SampleModel
{
    public class Level2 : PropertyChangeNotifier
    {
        private Level3 level3 = new Level3();

        public Level3 Level3
        {
            get { return level3; }
            set
            {
                level3 = value;
                OnPropertyChanged();
            }
        }
    }
}