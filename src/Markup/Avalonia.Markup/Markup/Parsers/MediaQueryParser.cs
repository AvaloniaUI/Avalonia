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
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaQueryParser"/> class.
        /// </summary>
        public MediaQueryParser()
        {
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
                        result = result.Orientation(orientation.Argument);
                        break;
                    case MediaQueryGrammar.WidthSyntax width:
                        result = result.Width(width.LeftOperator, width.Left, width.RightOperator, width.Right);
                        break;
                    case MediaQueryGrammar.HeightSyntax height:
                        result = result.Height(height.LeftOperator, height.Left, height.RightOperator, height.Right);
                        break;
                    case MediaQueryGrammar.PlatformSyntax platform:
                        result = result.Platform(platform.Argument);
                        break;
                    case MediaQueryGrammar.OrSyntax or:
                    case MediaQueryGrammar.AndSyntax and:
                        if (results == null)
                        {
                            results = new List<Query>();
                        }

                        results.Add(result ?? throw new NotSupportedException("Invalid query!"));
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

                result = results.Count > 1 ? Queries.Or(results) : results[0];
            }

            return result;
        }
    }
}
