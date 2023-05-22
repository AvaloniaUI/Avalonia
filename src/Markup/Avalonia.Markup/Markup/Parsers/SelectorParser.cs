using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Styling;
using Avalonia.Utilities;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Markup.Parsers
{
    /// <summary>
    /// Parses a <see cref="Selector"/> from text.
    /// </summary>
    internal class SelectorParser
    {
        private readonly Func<string, string, Type> _typeResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectorParser"/> class.
        /// </summary>
        /// <param name="typeResolver">
        /// The type resolver to use. The type resolver is a function which accepts two strings:
        /// a type name and a XML namespace prefix and a type name, and should return the resolved
        /// type or throw an exception.
        /// </param>
        public SelectorParser(Func<string, string, Type> typeResolver)
        {
            _typeResolver = typeResolver;
        }

        /// <summary>
        /// Parses a <see cref="Selector"/> from a string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The parsed selector.</returns>
        [RequiresUnreferencedCode(TrimmingMessages.SelectorsParseRequiresUnreferencedCodeMessage)]
        public Selector? Parse(string s)
        {
            var syntax = SelectorGrammar.Parse(s);
            return Create(syntax);
        }

        [RequiresUnreferencedCode(TrimmingMessages.SelectorsParseRequiresUnreferencedCodeMessage)]
        private Selector? Create(IEnumerable<SelectorGrammar.ISyntax> syntax)
        {
            var result = default(Selector);
            var results = default(List<Selector>);

            foreach (var i in syntax)
            {
                switch (i)
                {

                    case SelectorGrammar.OfTypeSyntax ofType:
                        result = result.OfType(Resolve(ofType.Xmlns, ofType.TypeName));
                        break;
                    case SelectorGrammar.IsSyntax @is:
                        result = result.Is(Resolve(@is.Xmlns, @is.TypeName));
                        break;
                    case SelectorGrammar.ClassSyntax @class:
                        result = result.Class(@class.Class);
                        break;
                    case SelectorGrammar.NameSyntax name:
                        result = result.Name(name.Name);
                        break;
                    case SelectorGrammar.PropertySyntax property:
                        {
                            var type = result?.TargetType;

                            if (type == null)
                            {
                                throw new InvalidOperationException("Property selectors must be applied to a type.");
                            }

                            var targetProperty = AvaloniaPropertyRegistry.Instance.FindRegistered(type, property.Property);

                            if (targetProperty == null)
                            {
                                throw new InvalidOperationException($"Cannot find '{property.Property}' on '{type}");
                            }

                            {
                                object? typedValue;

                                if (TypeUtilities.TryConvert(
                                        targetProperty.PropertyType,
                                        property.Value,
                                        CultureInfo.InvariantCulture,
                                        out typedValue))
                                {
                                    result = result.PropertyEquals(targetProperty, typedValue);
                                }
                                else
                                {
                                    throw new InvalidOperationException(
                                        $"Could not convert '{property.Value}' to '{targetProperty.PropertyType}");
                                }
                            }
                            break;
                        }
                    case SelectorGrammar.AttachedPropertySyntax attachedProperty:
                        var targetType = result?.TargetType;

                        if (targetType == null)
                        {
                            throw new InvalidOperationException("Attached Property selectors must be applied to a type.");
                        }

                        var attachedPropertyOwnerType = Resolve(attachedProperty.Xmlns, attachedProperty.TypeName);

                        if (attachedPropertyOwnerType is null)
                        {
                            throw new InvalidOperationException($"Cannot find '{attachedProperty.Xmlns}:{attachedProperty.TypeName}");
                        }

                        var targetAttachedProperty = AvaloniaPropertyRegistry.Instance.GetRegisteredAttached(targetType)
                            .FirstOrDefault(ap => ap.OwnerType == attachedPropertyOwnerType && ap.Name == attachedProperty.Property);

                        if (targetAttachedProperty == null)
                        {
                            throw new InvalidOperationException($"Cannot find '{attachedProperty.Property}' on '{attachedPropertyOwnerType}");
                        }

                        {
                            object? typedValue;

                            if (TypeUtilities.TryConvert(
                                    targetAttachedProperty.PropertyType,
                                    attachedProperty.Value,
                                    CultureInfo.InvariantCulture,
                                    out typedValue))
                            {
                                result = result.PropertyEquals(targetAttachedProperty, typedValue);
                            }
                            else
                            {
                                throw new InvalidOperationException(
                                    $"Could not convert '{attachedProperty.Value}' to '{targetAttachedProperty.PropertyType}");
                            }
                        }

                        break;
                    case SelectorGrammar.ChildSyntax child:
                        result = result!.Child();
                        break;
                    case SelectorGrammar.DescendantSyntax descendant:
                        result = result.Descendant();
                        break;
                    case SelectorGrammar.TemplateSyntax template:
                        result = result!.Template();
                        break;
                    case SelectorGrammar.NotSyntax not:
                        result = result.Not(x => Create(not.Argument)!);
                        break;
                    case SelectorGrammar.NthChildSyntax nth:
                        result = result.NthChild(nth.Step, nth.Offset);
                        break;
                    case SelectorGrammar.NthLastChildSyntax nth:
                        result = result.NthLastChild(nth.Step, nth.Offset);
                        break;
                    case SelectorGrammar.CommaSyntax comma:
                        if (results == null)
                        {
                            results = new List<Selector>();
                        }

                        results.Add(result ?? throw new NotSupportedException("Invalid selector!"));
                        result = null;
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported selector grammar '{i.GetType()}'.");
                }
            }

            if (results != null)
            {
                if (result != null)
                {
                    results.Add(result);
                }

                result = results.Count > 1 ? Selectors.Or(results) : results[0];
            }

            return result;
        }

        private Type Resolve(string xmlns, string typeName)
        {
            var result = _typeResolver(xmlns, typeName);

            if (result == null)
            {
                var type = string.IsNullOrWhiteSpace(xmlns) ? typeName : xmlns + ':' + typeName;
                throw new InvalidOperationException($"Could not resolve type '{type}'");
            }

            return result;
        }
    }
}
