// -----------------------------------------------------------------------
// <copyright file="InputManager.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

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

        public static IInputManager Instance => Locator.Current.GetService<IInputManager>();

        public IObservable<RawInputEventArgs> RawEventReceived => this.rawEventReceived;

        public void Process(RawInputEventArgs e) => this.rawEventReceived.OnNext(e);
    }
}
