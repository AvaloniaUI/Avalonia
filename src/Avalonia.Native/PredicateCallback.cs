using System;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    public class PredicateCallback : CallbackBase, IAvnPredicateCallback
    {
        private Func<bool> _predicate;

        public PredicateCallback(Func<bool> predicate)
        {
            _predicate = predicate;
        }

        bool IAvnPredicateCallback.Evaluate()
        {
            return _predicate();
        }
    }
}
