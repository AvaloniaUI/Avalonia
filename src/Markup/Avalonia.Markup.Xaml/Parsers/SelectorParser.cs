// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using Avalonia.Styling;
using Avalonia.Utilities;
using Sprache;

namespace Avalonia.Markup.Xaml.Parsers
{
    /// <summary>
    /// Parses a <see cref="Selector"/> from text.
    /// </summary>
    public class SelectorParser
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
            this._typeResolver = typeResolver;
        }

        /// <summary>
        /// Parses a <see cref="Selector"/> from a string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The parsed selector.</returns>
        public Selector Parse(string s)
        {
            var syntax = SelectorGrammar.Selector.Parse(s);
            var result = default(Selector);

            foreach (var i in syntax)
            {
                var ofType = i as SelectorGrammar.OfTypeSyntax;
                var @is = i as SelectorGrammar.IsSyntax;
                var @class = i as SelectorGrammar.ClassSyntax;
                var name = i as SelectorGrammar.NameSyntax;
                var property = i as SelectorGrammar.PropertySyntax;
                var child = i as SelectorGrammar.ChildSyntax;
                var descendant = i as SelectorGrammar.DescendantSyntax;
                var template = i as SelectorGrammar.TemplateSyntax;

                if (ofType != null)
                {
                    result = result.OfType(_typeResolver(ofType.TypeName, ofType.Xmlns));
                }
                if (@is != null)
                {
                    result = result.Is(_typeResolver(@is.TypeName, @is.Xmlns));
                }
                else if (@class != null)
                {
                    result = result.Class(@class.Class);
                }
                else if (name != null)
                {
                    result = result.Name(name.Name);
                }
                else if (property != null)
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

                    object typedValue;

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
                else if (child != null)
                {
                    result = result.Child();
                }
                else if (descendant != null)
                {
                    result = result.Descendant();
                }
                else if (template != null)
                {
                    result = result.Template();
                }
            }

            return result;
        }
    }
}
