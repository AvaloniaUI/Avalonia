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
    internal class MediaQueryParser
    {
        private readonly Func<string, string, Type> _typeResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaQueryParser"/> class.
        /// </summary>
        /// <param name="typeResolver">
        /// The type resolver to use. The type resolver is a function which accepts two strings:
        /// a type name and a XML namespace prefix and a type name, and should return the resolved
        /// type or throw an exception.
        /// </param>
        public MediaQueryParser(Func<string, string, Type> typeResolver)
        {
            _typeResolver = typeResolver;
        }

        /// <summary>
        /// Parses a <see cref="Selector"/> from a string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>The parsed selector.</returns>
        [RequiresUnreferencedCode(TrimmingMessages.SelectorsParseRequiresUnreferencedCodeMessage)]
        public Query? Parse(string s)
        {
            var syntax = MediaQueryGrammar.Parse(s);
            return Create(syntax);
        }

        [RequiresUnreferencedCode(TrimmingMessages.SelectorsParseRequiresUnreferencedCodeMessage)]
        private Query? Create(IEnumerable<ISyntax> syntax)
        {
            var result = default(Query);
            var results = default(List<Query>);

            foreach (var i in syntax)
            {
                switch (i)
                {
                    case MediaQueryGrammar.OrientationSyntax orientation:
                        result = Queries.Orientation(result, orientation.Argument);
                        break;
                    case MediaQueryGrammar.WidthSyntax width:
                        result = Queries.Width(result, width.LeftOperator, width.Left, width.RightOperator, width.Right);
                        break;
                    case MediaQueryGrammar.HeightSyntax height:
                        result = Queries.Height(result, height.LeftOperator, height.Left, height.RightOperator, height.Right);
                        break;
                    case MediaQueryGrammar.PlatformSyntax platform:
                        result = Queries.Platform(result, platform.Argument);
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

                result = results.Count > 1 ? Queries.Or(results) : results[0];
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
