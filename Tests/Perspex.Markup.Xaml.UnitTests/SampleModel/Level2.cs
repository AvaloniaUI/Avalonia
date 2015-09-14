// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Xaml.Base.UnitTest.SampleModel
{
    public class Level2 : PropertyChangeNotifier
    {
        private Level3 _level3 = new Level3();

        public Level3 Level3
        {
            get { return _level3; }
            set
            {
                _level3 = value;
                OnPropertyChanged();
            }
        }
    }
}