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

    public class Style : IStyle
    {
        public Style()
        {
            this.Setters = new List<Setter>();
        }

        public Style(Func<Control, Match> selector)
            : this()
        {
            this.Selector = selector;
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
                string description = "Style " + match.ToString();
                List<IObservable<bool>> o = new List<IObservable<bool>>();
                
                while (match != null)
                {
                    if (match.Observable != null)
                    {
                        o.Add(match.Observable);
                    }

                    match = match.Previous;
                }

                List<Setter.Subject> subjects = new List<Setter.Subject>();

                foreach (Setter setter in this.Setters)
                {
                    Setter.Subject subject = setter.CreateSubject(control, description);
                    subjects.Add(subject);
                    control.SetValue(setter.Property, subject);
                }

                if (o.Count == 0)
                {
                    foreach (Setter.Subject subject in subjects)
                    {
                        subject.Push(true);
                    }
                }
                else
                {
                    Observable.CombineLatest(o).Subscribe(x =>
                    {
                        bool on = x.All(y => y);

                        foreach (Setter.Subject subject in subjects)
                        {
                            subject.Push(on);
                        }
                    });
                }
            }
        }
    }
}
