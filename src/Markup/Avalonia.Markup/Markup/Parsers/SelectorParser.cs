// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using Avalonia.Styling;
using Avalonia.Utilities;

namespace Avalonia.Markup.Parsers
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
            var syntax = SelectorGrammar.Parse(s);
            var result = default(Selector);

            foreach (var i in syntax)
            {
                switch (i)
                {

                    case SelectorGrammar.OfTypeSyntax ofType:
                        result = result.OfType(_typeResolver(ofType.Xmlns, ofType.TypeName));
                        break;
                    case SelectorGrammar.IsSyntax @is:
                        result = result.Is(_typeResolver(@is.Xmlns, @is.TypeName));
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
                            break;
                        }
                    case SelectorGrammar.ChildSyntax child:
                        result = result.Child();
                        break;
                    case SelectorGrammar.DescendantSyntax descendant:
                        result = result.Descendant();
                        break;
                    case SelectorGrammar.TemplateSyntax template:
                        result = result.Template();
                        break;
                }
            }

            return result;
        }
    }
}
