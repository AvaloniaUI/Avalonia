// -----------------------------------------------------------------------
// <copyright file="Selectors.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using System.Reflection;

    public static class Selectors
    {
        public static Selector Class(this Selector previous, string name)
        {
            Contract.Requires<ArgumentNullException>(previous != null);
            Contract.Requires<ArgumentNullException>(name != null);

            return new Selector(previous, x => MatchClass(x, name), name);
        }

        public static Selector Descendent(this Selector previous)
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            return new Selector(previous, x => MatchDescendent(x, previous), " ", stopTraversal: true);
        }

        public static Selector Is(this Selector previous, Type type)
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            return new Selector(previous, x => MatchIs(x, type), type.Name);
        }

        public static Selector Is<T>(this Selector previous) where T : IStyleable
        {
            return previous.Is(typeof(T));
        }

        public static Selector Name(this Selector previous, string name)
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            return new Selector(previous, x => MatchName(x, name), '#' + name);
        }

        public static Selector OfType(this Selector previous, Type type)
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            return new Selector(previous, x => MatchOfType(x, type), type.Name);
        }

        public static Selector OfType<T>(this Selector previous) where T : IStyleable
        {
            return previous.OfType(typeof(T));
        }

        public static Selector Parent(this Selector previous)
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            return new Selector(previous, x => MatchParent(x, previous), " < ", stopTraversal: true);
        }

        public static Selector Template(this Selector previous)
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            return new Selector(
                previous, 
                x => MatchTemplate(x, previous), 
                " /deep/ ", 
                inTemplate: true, 
                stopTraversal: true);
        }

        private static SelectorMatch MatchClass(IStyleable control, string name)
        {
            return new SelectorMatch(
                Observable
                    .Return(control.Classes.Contains(name))
                    .Concat(control.Classes.Changed.Select(e => control.Classes.Contains(name))));
        }

        private static SelectorMatch MatchDescendent(IStyleable control, Selector previous)
        {
            ILogical c = (ILogical)control;
            List<IObservable<bool>> descendentMatches = new List<IObservable<bool>>();

            while (c != null)
            {
                c = c.LogicalParent;

                if (c is IStyleable)
                {
                    var match = previous.Match((IStyleable)c);

                    if (match.ImmediateResult != null)
                    {
                        if (match.ImmediateResult == true)
                        {
                            return new SelectorMatch(true);
                        }
                    }
                    else
                    {
                        descendentMatches.Add(match.ObservableResult);
                    }
                }
            }

            return new SelectorMatch(new StyleActivator(
                descendentMatches,
                ActivatorMode.Or));
        }

        private static SelectorMatch MatchIs(IStyleable control, Type type)
        {
            var controlType = control.StyleKey ?? control.GetType();
            return new SelectorMatch(type.GetTypeInfo().IsAssignableFrom(controlType.GetTypeInfo()));
        }

        private static SelectorMatch MatchName(IStyleable control, string name)
        {
            return new SelectorMatch(control.Name == name);
        }

        private static SelectorMatch MatchOfType(IStyleable control, Type type)
        {
            var controlType = control.StyleKey ?? control.GetType();
            return new SelectorMatch(controlType == type);
        }

        private static SelectorMatch MatchParent(IStyleable control, Selector previous)
        {
            var parent = ((ILogical)control).LogicalParent;
            return previous.Match((IStyleable)parent);
        }

        private static SelectorMatch MatchTemplate(IStyleable control, Selector previous)
        {
            IStyleable templatedParent = control.TemplatedParent as IStyleable;

            if (templatedParent == null)
            {
                throw new InvalidOperationException(
                    "Cannot call Template selector on control with null TemplatedParent.");
            }

            return previous.Match(templatedParent);
        }
    }
}
