// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Xaml.Base.UnitTest.SampleModel
{
    public class Level1 : PropertyChangeNotifier
    {
        private Level2 _level2 = new Level2();
        private DateTime _dateTime;
        private string _text;

        public Level2 Level2
        {
            get { return _level2; }
            set
            {
                _level2 = value;
                OnPropertyChanged();
            }
        }

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                OnPropertyChanged();
            }
        }

        public DateTime DateTime
        {
            get { return _dateTime; }
            set
            {
                _dateTime = value;
                OnPropertyChanged();
            }
        }
    }
}