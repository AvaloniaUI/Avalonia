// -----------------------------------------------------------------------
// <copyright file="Style.cs" company="Tricycle">
// Copyright 2014 Tricycle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Perspex.Controls;

    public class Style
    {
        private bool applied;

        public Style()
        {
            this.Setters = new List<Setter>();
        }

        public Func<Control, Match> Selector
        {
            get;
            set;
        }

        public IEnumerable<Setter> Setters
        {
            get;
            set;
        }

        public void Attach(Control control)
        {
            Match match = this.Selector(control);

            if (match != null)
            {
                List<IObservable<bool>> o = new List<IObservable<bool>>();
                
                while (match != null)
                {
                    if (match.Observable != null)
                    {
                        o.Add(match.Observable);
                    }

                    match = match.Previous;
                }

                List<SetterSubject> subjects = new List<SetterSubject>();

                foreach (Setter setter in this.Setters)
                {
                    SetterSubject subject = setter.CreateSubject(control);
                    subjects.Add(subject);
                    control.SetValue(setter.Property, subject);
                }

                Observable.CombineLatest(o).Subscribe(x =>
                {
                    bool on = x.All(y => y);

                    foreach (SetterSubject subject in subjects)
                    {
                        subject.Push(on);
                    }
                });
            }
        }
    }
}
