using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.UnitTests;

namespace Avalonia.Base.UnitTests.Data.Core
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
