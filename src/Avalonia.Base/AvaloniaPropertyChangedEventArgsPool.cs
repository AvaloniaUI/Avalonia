using System.Collections.Generic;
using Avalonia.Data;

namespace Avalonia
{
    internal static class AvaloniaPropertyChangedEventArgsPool<T>
    {
        private const int MaxPoolSize = 4;
        private static Stack<AvaloniaPropertyChangedEventArgs<T>> _pool = new();

        public static AvaloniaPropertyChangedEventArgs<T> Get(
            IAvaloniaObject sender,
            AvaloniaProperty<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority,
            bool isEffectiveValueChange)
        {
            if (_pool.Count == 0)
            {
                return new(sender, property, oldValue, newValue, priority, isEffectiveValueChange);
            }
            else
            {
                var e = _pool.Pop();
                e.Initialize(sender, property, oldValue, newValue, priority, isEffectiveValueChange);
                return e;
            }
        }

        public static void Release(AvaloniaPropertyChangedEventArgs<T> e)
        {
            if (_pool.Count < MaxPoolSize)
            {
                e.Recycle();
                _pool.Push(e);
            }
        }
    }
}
