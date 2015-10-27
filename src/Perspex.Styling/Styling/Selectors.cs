// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reflection;

namespace Perspex.Styling
{
    /// <summary>
    /// Extension methods for <see cref="Selector"/>.
    /// </summary>
    public static class Selectors
    {
        /// <summary>
        /// Returns a selector which matches a previous selector's child.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <returns>The selector.</returns>
        public static Selector Child(this Selector previous)
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            return new Selector(previous, x => MatchChild(x, previous), " < ", stopTraversal: true);
        }

        /// <summary>
        /// Returns a selector which matches a control's style class.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="name">The name of the style class.</param>
        /// <returns>The selector.</returns>
        public static Selector Class(this Selector previous, string name)
        {
            Contract.Requires<ArgumentNullException>(previous != null);
            Contract.Requires<ArgumentNullException>(name != null);

            return new Selector(previous, x => MatchClass(x, name), name[0] == ':' ? name : '.' + name);
        }

        /// <summary>
        /// Returns a selector which matches a descendent of a previous selector.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <returns>The selector.</returns>
        public static Selector Descendent(this Selector previous)
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            return new Selector(previous, x => MatchDescendent(x, previous), " ", stopTraversal: true);
        }

        /// <summary>
        /// Returns a selector which matches a type or a derived type.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="type">The type.</param>
        /// <returns>The selector.</returns>
        public static Selector Is(this Selector previous, Type type)
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            return new Selector(previous, x => MatchIs(x, type), type.Name, type);
        }

        /// <summary>
        /// Returns a selector which matches a type or a derived type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="previous">The previous selector.</param>
        /// <returns>The selector.</returns>
        public static Selector Is<T>(this Selector previous) where T : IStyleable
        {
            return previous.Is(typeof(T));
        }

        /// <summary>
        /// Returns a selector which matches a control's Name.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="name">The name.</param>
        /// <returns>The selector.</returns>
        public static Selector Name(this Selector previous, string name)
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            return new Selector(previous, x => MatchName(x, name), '#' + name);
        }

        /// <summary>
        /// Returns a selector which matches a type.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="type">The type.</param>
        /// <returns>The selector.</returns>
        public static Selector OfType(this Selector previous, Type type)
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            return new Selector(previous, x => MatchOfType(x, type), type.Name, type);
        }

        /// <summary>
        /// Returns a selector which matches a type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="previous">The previous selector.</param>
        /// <returns>The selector.</returns>
        public static Selector OfType<T>(this Selector previous) where T : IStyleable
        {
            return previous.OfType(typeof(T));
        }

        /// <summary>
        /// Returns a selector which matches a control with the specified property value.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="previous">The previous selector.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The property value.</param>
        /// <returns>The selector.</returns>
        public static Selector PropertyEquals<T>(this Selector previous, PerspexProperty<T> property, object value)
        {
            Contract.Requires<ArgumentNullException>(previous != null);
            Contract.Requires<ArgumentNullException>(property != null);

            return new Selector(previous, x => MatchPropertyEquals(x, property, value), $"[{property.Name}={value}]");
        }

        /// <summary>
        /// Returns a selector which matches a control with the specified property value.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="previous">The previous selector.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The property value.</param>
        /// <returns>The selector.</returns>
        public static Selector PropertyEquals(this Selector previous, PerspexProperty property, object value)
        {
            Contract.Requires<ArgumentNullException>(previous != null);
            Contract.Requires<ArgumentNullException>(property != null);

            return new Selector(previous, x => MatchPropertyEquals(x, property, value), $"[{property.Name}={value}]");
        }

        /// <summary>
        /// Returns a selector which enters a lookless control's template.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <returns>The selector.</returns>
        public static Selector Template(this Selector previous)
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            return new Selector(
                previous,
                x => MatchTemplate(x, previous),
                " /template/ ",
                inTemplate: true,
                stopTraversal: true);
        }

        private static SelectorMatch MatchChild(IStyleable control, Selector previous)
        {
            var parent = ((ILogical)control).LogicalParent;

            if (parent != null)
            {
                return previous.Match((IStyleable)parent);
            }
            else
            {
                return SelectorMatch.False;
            }
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
                            return SelectorMatch.True;
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

        private static SelectorMatch MatchPropertyEquals(IStyleable x, PerspexProperty property, object value)
        {
            if (!x.IsRegistered(property))
            {
                return SelectorMatch.False;
            }
            else
            {
                return new SelectorMatch(x.GetObservable(property).Select(v => Equals(v, value)));
            }
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
