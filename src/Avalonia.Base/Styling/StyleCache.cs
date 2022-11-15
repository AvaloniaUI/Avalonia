using System;
using System.Collections.Generic;

namespace Avalonia.Styling
{
    /// <summary>
    /// Simple cache for improving performance of applying styles.
    /// </summary>
    /// <remarks>
    /// Maps <see cref="IStyleable.StyleKey"/> to a list of styles that are known be be possible
    /// matches.
    /// </remarks>
    internal class StyleCache : Dictionary<Type, List<IStyle>?>
    {
        public SelectorMatchResult TryAttach(IList<IStyle> styles, IStyleable target, object? host)
        {
            if (TryGetValue(target.StyleKey, out var cached))
            {
                if (cached is object)
                {
                    var result = SelectorMatchResult.NeverThisType;

                    foreach (var style in cached)
                    {
                        var childResult = style.TryAttach(target, host);
                        if (childResult > result)
                            result = childResult;
                    }

                    return result;
                }
                else
                {
                    return SelectorMatchResult.NeverThisType;
                }
            }
            else
            {
                List<IStyle>? matches = null;

                foreach (var child in styles)
                {
                    if (child.TryAttach(target, host) != SelectorMatchResult.NeverThisType)
                    {
                        matches ??= new List<IStyle>();
                        matches.Add(child);
                    }
                }

                Add(target.StyleKey, matches);

                return matches is null ?
                    SelectorMatchResult.NeverThisType :
                    SelectorMatchResult.AlwaysThisType;
            }
        }
    }
}
