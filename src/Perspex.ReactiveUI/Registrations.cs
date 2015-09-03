using System;
using System.Reactive.Concurrency;
using System.Threading;


namespace ReactiveUI
{
    /// <summary>
    /// Ignore me. This class is a secret handshake between RxUI and RxUI.Xaml
    /// in order to register certain classes on startup that would be difficult
    /// to register otherwise.
    /// </summary>
    public class PlatformRegistrations : IWantsToRegisterStuff
    {
        public void Register(Action<Func<object>, Type> registerFunction)
        {
            RxApp.MainThreadScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current);
        }
    }
}
