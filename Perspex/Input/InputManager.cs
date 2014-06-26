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
    using Perspex.Controls;
    using Perspex.Input.Raw;

    public class InputManager : IInputManager
    {
        private List<Control> pointerOvers = new List<Control>();

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
            IEnumerable<IVisual> hits = visual.GetVisualsAt(p);

            foreach (var control in this.pointerOvers.ToList().Except(hits).Cast<Control>())
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

            foreach (var control in hits.Except(this.pointerOvers).Cast<Control>())
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
