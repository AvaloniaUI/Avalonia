using System;
using System.Collections.Generic;
using Avalonia.Styling.Activators;
using Avalonia.Utilities;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// A selector that matches the common case of a type and/or name followed by a collection of
    /// style classes and pseudoclasses.
    /// </summary>
    internal sealed class TypeNameAndClassSelector : Selector
    {
        private readonly Selector? _previous;
        private List<string>? _classes;
        private Type? _targetType;
        private string? _selectorString;

        public static TypeNameAndClassSelector OfType(Selector? previous, Type targetType)
        {
            var result = new TypeNameAndClassSelector(previous);
            result._targetType = targetType;
            result.IsConcreteType = true;

            return result;
        }

        public static TypeNameAndClassSelector Is(Selector? previous, Type targetType)
        {
            var result = new TypeNameAndClassSelector(previous);
            result._targetType = targetType;
            result.IsConcreteType = false;

            return result;
        }

        public static TypeNameAndClassSelector ForName(Selector? previous, string name)
        {
            var result = new TypeNameAndClassSelector(previous);
            result.Name = name;

            return result;
        }

        public static TypeNameAndClassSelector ForClass(Selector? previous, string className)
        {
            var result = new TypeNameAndClassSelector(previous);
            result.Classes.Add(className);

            return result;
        }

        TypeNameAndClassSelector(Selector? previous)
        {
            _previous = previous;
        }

        /// <inheritdoc/>
        internal override bool InTemplate => _previous?.InTemplate ?? false;

        /// <summary>
        /// Gets the name of the control to match.
        /// </summary>
        public string? Name { get; set; }

        /// <inheritdoc/>
        internal override Type? TargetType => _targetType ?? _previous?.TargetType;

        /// <inheritdoc/>
        internal override bool IsCombinator => false;

        /// <summary>
        /// Whether the selector matches the concrete <see cref="TargetType"/> or any object which
        /// implements <see cref="TargetType"/>.
        /// </summary>
        public bool IsConcreteType { get; private set; }

        /// <summary>
        /// The style classes which the selector matches.
        /// </summary>
        public IList<string> Classes => _classes ??= new();

        /// <inheritdoc/>
        public override string ToString(Style? owner)
        {
            return _selectorString ??= BuildSelectorString(owner);
        }

        /// <inheritdoc/>
        private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            if (TargetType != null)
            {
                var controlType = StyledElement.GetStyleKey(control) ?? control.GetType();

                if (IsConcreteType)
                {
                    if (controlType != TargetType)
                    {
                        return SelectorMatch.NeverThisType;
                    }
                }
                else
                {
                    if (!TargetType.IsAssignableFrom(controlType))
                    {
                        return SelectorMatch.NeverThisType;
                    }
                }
            }

            if (Name != null && control.Name != Name)
            {
                return SelectorMatch.NeverThisInstance;
            }

            if (_classes is { Count: > 0 })
            {
                if (subscribe)
                {
                    var observable = new StyleClassActivator(control.Classes, _classes);

                    return new SelectorMatch(observable);
                }

                if (!StyleClassActivator.AreClassesMatching(control.Classes, _classes))
                {
                    return SelectorMatch.NeverThisInstance;
                }
            }

            return Name == null ? SelectorMatch.AlwaysThisType : SelectorMatch.AlwaysThisInstance;
        }

        private protected override Selector? MovePrevious() => _previous;
        private protected override Selector? MovePreviousOrParent() => _previous;

        private string BuildSelectorString(Style? owner)
        {
            var builder = StringBuilderCache.Acquire();

            if (_previous != null)
            {
                builder.Append(_previous.ToString(owner));
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

            if (_classes is { Count: > 0 })
            {
                foreach (var c in _classes)
                {
                    if (!c.StartsWith(":"))
                    {
                        builder.Append('.');
                    }

                    builder.Append(c);
                }
            }

            return StringBuilderCache.GetStringAndRelease(builder);
        }
    }
}
