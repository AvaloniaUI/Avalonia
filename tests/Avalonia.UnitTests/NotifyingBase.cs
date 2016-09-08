// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Avalonia.UnitTests
{
    public class NotifyingBase : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler _propertyChanged;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                _propertyChanged += value;
                ++PropertyChangedSubscriptionCount;
            }

            remove
            {
                if (_propertyChanged?.GetInvocationList().Contains(value) == true)
                {
                    _propertyChanged -= value;
                    --PropertyChangedSubscriptionCount;
                }
            }
        }

        public int PropertyChangedSubscriptionCount
        {
            get;
            private set;
        }

        public void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
