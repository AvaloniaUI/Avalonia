// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Xaml.Base.UnitTest.SampleModel
{
    public class Level3 : PropertyChangeNotifier
    {
        private int _property = 10;

        public int Property
        {
            get { return _property; }
            set
            {
                _property = value;
                OnPropertyChanged();
            }
        }
    }
}