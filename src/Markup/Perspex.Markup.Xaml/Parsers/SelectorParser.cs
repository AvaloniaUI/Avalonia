// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Styling;
using Sprache;

namespace Perspex.Markup.Xaml.Parsers
{
    /// <summary>
    /// Parses a <see cref="Selector"/> from text.
    /// </summary>
    public class SelectorParser
    {
        private Func<string, string, Type> _typeResolver;

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
            var result = new Selector();

            foreach (var i in syntax)
            {
                var ofType = i as SelectorGrammar.OfTypeSyntax;
                var @class = i as SelectorGrammar.ClassSyntax;
                var name = i as SelectorGrammar.NameSyntax;
                var property = i as SelectorGrammar.PropertySyntax;
                var child = i as SelectorGrammar.ChildSyntax;
                var descendent = i as SelectorGrammar.DescendentSyntax;
                var template = i as SelectorGrammar.TemplateSyntax;

                if (ofType != null)
                {
                    result = result.OfType(_typeResolver(ofType.TypeName, ofType.Xmlns));
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
                    throw new NotImplementedException();
                }
                else if (child != null)
                {
                    result = result.Child();
                }
                else if (descendent != null)
                {
                    result = result.Descendent();
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
