





namespace Perspex.Input
{
    using System;
    using Perspex.Input.Raw;
    using Perspex.Layout;

    public interface IInputManager
    {
        IObservable<RawInputEventArgs> RawEventReceived { get; }

        IObservable<RawInputEventArgs> PostProcess { get; }

        void Process(RawInputEventArgs e);
    }
}
