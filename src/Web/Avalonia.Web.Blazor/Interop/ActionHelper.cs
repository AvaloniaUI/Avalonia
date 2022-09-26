using System;
using System.ComponentModel;
using Microsoft.JSInterop;

namespace Avalonia.Web.Blazor.Interop
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ActionHelper
    {
        private readonly Action action;

        public ActionHelper(Action action)
        {
            this.action = action;
        }

        [JSInvokable]
        public void Invoke() => action?.Invoke();
    }
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ActionHelper<T>
    {
        private readonly Action<T> action;

        public ActionHelper(Action<T> action)
        {
            this.action = action;
        }

        [JSInvokable]
        public void Invoke(T param1) => action?.Invoke(param1);
    }
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ActionHelper<T1, T2>
    {
        private readonly Action<T1, T2> action;

        public ActionHelper(Action<T1, T2> action)
        {
            this.action = action;
        }

        [JSInvokable]
        public void Invoke(T1 p1, T2 p2) => action?.Invoke(p1, p2);
    }
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ActionHelper<T1, T2, T3>
    {
        private readonly Action<T1, T2, T3> action;

        public ActionHelper(Action<T1, T2, T3> action)
        {
            this.action = action;
        }

        [JSInvokable]
        public void Invoke(T1 p1, T2 p2, T3 p3) => action?.Invoke(p1, p2, p3);
    }
    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ActionHelper<T1, T2, T3, T4>
    {
        private readonly Action<T1, T2, T3, T4> action;

        public ActionHelper(Action<T1, T2, T3, T4> action)
        {
            this.action = action;
        }

        [JSInvokable]
        public void Invoke(T1 p1, T2 p2, T3 p3, T4 p4) => action?.Invoke(p1, p2, p3, p4);
    }
    
    
    
    
    
}
