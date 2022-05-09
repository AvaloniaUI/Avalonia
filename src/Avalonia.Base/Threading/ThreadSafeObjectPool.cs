using System.Collections.Generic;

namespace Avalonia.Threading
{
    public class ThreadSafeObjectPool<T> where T : class, new()
    {
        private Stack<T> _stack = new Stack<T>();
        public static ThreadSafeObjectPool<T> Default { get; } = new ThreadSafeObjectPool<T>();

        public T Get()
        {
            lock (_stack)
            {
                if(_stack.Count == 0)
                    return new T();
                return _stack.Pop();
            }
        }

        public void Return(T obj)
        {
            lock (_stack)
            {
                _stack.Push(obj);
            }
        }
    }
}
