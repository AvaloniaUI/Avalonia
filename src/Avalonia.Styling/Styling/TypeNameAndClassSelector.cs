// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Text;
using Avalonia.Collections;
using Avalonia.Reactive;

namespace Avalonia.Styling
{
    /// <summary>
    /// A selector that matches the common case of a type and/or name followed by a collection of
    /// style classes and pseudoclasses.
    /// </summary>
    internal class TypeNameAndClassSelector : Selector
    {
        private readonly Selector _previous;
        private readonly Lazy<List<string>> _classes = new Lazy<List<string>>(() => new List<string>());
        private Type _targetType;
        
        private string _selectorString;

        public static TypeNameAndClassSelector OfType(Selector previous, Type targetType)
        {
            var result = new TypeNameAndClassSelector(previous);
            result._targetType = targetType;
            result.IsConcreteType = true;

            return result;
        }

        public static TypeNameAndClassSelector Is(Selector previous, Type targetType)
        {
            var result = new TypeNameAndClassSelector(previous);
            result._targetType = targetType;
            result.IsConcreteType = false;

            return result;
        }

        public static TypeNameAndClassSelector ForName(Selector previous, string name)
        {
            var result = new TypeNameAndClassSelector(previous);
            result.Name = name;

            return result;
        }

        public static TypeNameAndClassSelector ForClass(Selector previous, string className)
        {
            var result = new TypeNameAndClassSelector(previous);
            result.Classes.Add(className);

            return result;
        }

        protected TypeNameAndClassSelector(Selector previous)
        {
            _previous = previous;
        }

        /// <inheritdoc/>
        public override bool InTemplate => _previous?.InTemplate ?? false;

        /// <summary>
        /// Gets the name of the control to match.
        /// </summary>
        public string Name { get; set; }

        /// <inheritdoc/>
        public override Type TargetType => _targetType ?? _previous?.TargetType;

        /// <inheritdoc/>
        public override bool IsCombinator => false;

        /// <summary>
        /// Whether the selector matches the concrete <see cref="TargetType"/> or any object which
        /// implements <see cref="TargetType"/>.
        /// </summary>
        public bool IsConcreteType { get; private set; }

        /// <summary>
        /// The style classes which the selector matches.
        /// </summary>
        public IList<string> Classes => _classes.Value;

        /// <inheritdoc/>
        public override string ToString()
        {
            if (_selectorString == null)
            {
                _selectorString = BuildSelectorString();
            }

            return _selectorString;
        }

        /// <inheritdoc/>
        protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
        {
            if (TargetType != null)
            {
                var controlType = control.StyleKey ?? control.GetType();

                if (IsConcreteType)
                {
                    if (controlType != TargetType)
                    {
                        return SelectorMatch.NeverThisType;
                    }
                }
                else
                {
                    if (!TargetType.GetTypeInfo().IsAssignableFrom(controlType.GetTypeInfo()))
                    {
                        return SelectorMatch.NeverThisType;
                    }
                }
            }

            if (Name != null && control.Name != Name)
            {
                return SelectorMatch.NeverThisInstance;
            }

            if (_classes.IsValueCreated && _classes.Value.Count > 0)
            {
                if (subscribe)
                {
                    var observable = new ClassObserver(control.Classes, _classes.Value);

                    return new SelectorMatch(observable);
                }

                if (!AreClassesMatching(control.Classes, Classes))
                {
                    return SelectorMatch.NeverThisInstance;
                }
            }

            return Name == null ? SelectorMatch.AlwaysThisType : SelectorMatch.AlwaysThisInstance;
        }

        protected override Selector MovePrevious() => _previous;

        private string BuildSelectorString()
        {
            var builder = new StringBuilder();

            if (_previous != null)
            {
                builder.Append(_previous.ToString());
            }

            if (TargetType != null)
            {
                if (IsConcreteType)
                {
                    builder.Append(TargetType.Name);
                }
                else
                {
                    builder.Append(":is(");
                    builder.Append(TargetType.Name);
                    builder.Append(")");
                }
            }

            if (Name != null)
            {
                builder.Append('#');
                builder.Append(Name);
            }

            if (_classes.IsValueCreated && _classes.Value.Count > 0)
            {
                foreach (var c in Classes)
                {
                    if (!c.StartsWith(":"))
                    {
                        builder.Append('.');
                    }

                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        private static bool AreClassesMatching(IReadOnlyList<string> classes, IList<string> toMatch)
        {
            int remainingMatches = toMatch.Count;
            int classesCount = classes.Count;

            // Early bail out - we can't match if control does not have enough classes.
            if (classesCount < remainingMatches)
            {
                return false;
            }

            for (var i = 0; i < classesCount; i++)
            {
                var c = classes[i];

                if (toMatch.Contains(c))
                {
                    --remainingMatches;

                    // Already matched so we can skip checking other classes.
                    if (remainingMatches == 0)
                    {
                        break;
                    }
                }
            }

            return remainingMatches == 0;
        }

        private sealed class ClassObserver : LightweightObservableBase<bool>
        {
            private readonly IList<string> _match;
            private readonly IAvaloniaReadOnlyList<string> _classes;
            private bool _hasMatch;

            public ClassObserver(IAvaloniaReadOnlyList<string> classes, IList<string> match)
            {
                _classes = classes;
                _match = match;
            }

            protected override void Deinitialize() => _classes.CollectionChanged -= ClassesChanged;

            protected override void Initialize()
            {
                _hasMatch = IsMatching();
                _classes.CollectionChanged += ClassesChanged;
            }

            protected override void Subscribed(IObserver<bool> observer, bool first)
            {
                observer.OnNext(_hasMatch);
            }

            private void ClassesChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action != NotifyCollectionChangedAction.Move)
                {
                    var hasMatch = IsMatching();

                    if (hasMatch != _hasMatch)
                    {
                        PublishNext(hasMatch);
                        _hasMatch = hasMatch;
                    }
                }
            }

            private bool IsMatching()
            {
                return AreClassesMatching(_classes, _match);
            }
        }
    }
}
