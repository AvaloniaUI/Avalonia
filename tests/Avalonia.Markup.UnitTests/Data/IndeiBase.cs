// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.UnitTests;

namespace Avalonia.Markup.UnitTests.Data
{
    internal abstract class IndeiBase : NotifyingBase, INotifyDataErrorInfo
    {
        private EventHandler<DataErrorsChangedEventArgs> _errorsChanged;

        public abstract bool HasErrors { get; }
        public int ErrorsChangedSubscriptionCount { get; private set; }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged
        {
            add { _errorsChanged += value; ++ErrorsChangedSubscriptionCount; }
            remove { _errorsChanged -= value; --ErrorsChangedSubscriptionCount; }
        }

        public abstract IEnumerable GetErrors(string propertyName);

        protected void RaiseErrorsChanged([CallerMemberName] string propertyName = "")
        {
            _errorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }
}
