





namespace Perspex.Input
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Subjects;
    using Perspex.Input.Raw;
    using Splat;

    public class InputManager : IInputManager
    {
        private Subject<RawInputEventArgs> rawEventReceived = new Subject<RawInputEventArgs>();

        private Subject<RawInputEventArgs> postProcess = new Subject<RawInputEventArgs>();

        public static IInputManager Instance => Locator.Current.GetService<IInputManager>();

        public IObservable<RawInputEventArgs> RawEventReceived => this.rawEventReceived;

        public IObservable<RawInputEventArgs> PostProcess => this.postProcess;

        public void Process(RawInputEventArgs e)
        {
            this.rawEventReceived.OnNext(e);
            this.postProcess.OnNext(e);
        }
    }
}
