using System;

namespace Perspex.Xaml.Base.UnitTest.SampleModel
{
    public class Level1 : PropertyChangeNotifier
    {
        private Level2 level2 = new Level2();
        private DateTime dateTime;
        private string text;

        public Level2 Level2
        {
            get { return level2; }
            set
            {
                level2 = value;
                OnPropertyChanged();
            }
        }

        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                OnPropertyChanged();
            }
        }

        public DateTime DateTime
        {
            get { return dateTime; }
            set
            {
                dateTime = value;
                OnPropertyChanged();
            }
        }
    }
}