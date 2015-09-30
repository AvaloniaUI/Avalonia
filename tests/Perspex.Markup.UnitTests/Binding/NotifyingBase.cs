// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.ComponentModel;

namespace Perspex.Markup.UnitTests.Binding
{
    public class NotifyingBase : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler _propertyChanged;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                _propertyChanged += value;
                ++SubscriptionCount;
            }

            remove
            {
                _propertyChanged -= value;
                --SubscriptionCount;
            }
        }

        public int SubscriptionCount
        {
            get;
            private set;
        }

        protected void RaisePropertyChanged(string propertyName)
        {
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
