using System;

namespace Avalonia.Data.Core
{
    internal abstract class TypedBindingTrigger<TIn>
    {
        private readonly int _index;
        private Action<int>? _changed;

        public TypedBindingTrigger(int index) => _index = index;

        
        public bool Subscribe(TIn root, Action<int> changed)
        {
            if (_changed is not null)
                throw new AvaloniaInternalException("Trigger is already subscribed.");

            try
            {
                var result = SubscribeCore(root);
                _changed = changed;
                return result;
            }
            catch
            {
                return false;
            }
        }

        public void Unsubscribe()
        {
            _changed = null;
            UnsubscribeCore();
        }

        protected void OnChanged() => _changed?.Invoke(_index);
        protected abstract bool SubscribeCore(TIn root);
        protected abstract void UnsubscribeCore();
    }
}
