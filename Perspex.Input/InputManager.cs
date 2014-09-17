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

    public class InputManager : IInputManager
    {
        private List<IInputElement> pointerOvers = new List<IInputElement>();

        private Subject<RawInputEventArgs> rawEventReceived = new Subject<RawInputEventArgs>();

        public IObservable<RawInputEventArgs> RawEventReceived
        {
            get { return this.rawEventReceived; }
        }

        public void Process(RawInputEventArgs e)
        {
            this.rawEventReceived.OnNext(e);
        }

        public void SetPointerOver(IPointerDevice device, IVisual visual, Point p)
        {
            IEnumerable<IInputElement> hits = visual.GetVisualsAt(p).OfType<IInputElement>();

            foreach (var control in this.pointerOvers.Except(hits).ToList())
            {
                PointerEventArgs e = new PointerEventArgs
                {
                    RoutedEvent = InputElement.PointerLeaveEvent,
                    Device = device,
                    OriginalSource = control,
                    Source = control,
                };

                this.pointerOvers.Remove(control);
                control.RaiseEvent(e);
            }

            foreach (var control in hits.Except(this.pointerOvers))
            {
                PointerEventArgs e = new PointerEventArgs
                {
                    RoutedEvent = InputElement.PointerEnterEvent,
                    Device = device,
                    OriginalSource = control,
                    Source = control,
                };

                this.pointerOvers.Add(control);
                control.RaiseEvent(e);
            }
        }
    }
}
