using System;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    public class MenuActionCallback : CallbackBase, IAvnActionCallback
    {
        private Action _action;

        public MenuActionCallback(Action action)
        {
            _action = action;
        }

        public void Run()
        {
            _action?.Invoke();
        }
    }
}
