using System;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    internal class MenuActionCallback : NativeCallbackBase, IAvnActionCallback
    {
        private Action _action;

        public MenuActionCallback(Action action)
        {
            _action = action;
        }

        void IAvnActionCallback.Run()
        {
            _action?.Invoke();
        }
    }
}
