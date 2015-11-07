// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Perspex.Markup.Xaml.UnitTests
{
    internal class ViewModelMock : INotifyPropertyChanged
    {
        private string _str;
        private int _intProp;

        public int IntProp
        {
            get { return _intProp; }
            set
            {
                _intProp = value;
                OnPropertyChanged();
            }
        }

        public string StrProp
        {
            get { return _str; }
            set
            {
                _str = value;
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