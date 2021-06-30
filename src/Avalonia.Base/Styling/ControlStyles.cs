using System;
using System.Collections.Generic;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Styling
{
    // TODO: Move Application to Avalonia.Base so we can make this internal?
    public class ControlStyles : Styles
    {
        private Dictionary<Type, List<Style>?>? _cache;

        public ControlStyles(IResourceHost owner)
            : base(owner)
        {
        }

        public void Apply(IStyleable target)
        {
            var host = (IStyleHost)Owner!;

            _cache ??= new();

            if (_cache.TryGetValue(target.StyleKey, out var cached))
            {
                if (cached is object)
                {
                    List<Style>? matches = null;
                    foreach (var style in cached)
                        Apply(style, target, host, false, ref matches);
                }
            }
            else
            {
                List<Style>? matches = null;
                Apply(this, target, host, true, ref matches);
                _cache.Add(target.StyleKey, matches);
            }
        }

        public void InvalidateCache() => _cache?.Clear();

        private void Apply(
            IStyle style,
            IStyleable target,
            IStyleHost host,
            bool buildMatches,
            ref List<Style>? matches)
        {
            if (style is Style s)
            {
                var match = target.ApplyStyle(s, host);

                if (buildMatches && match != SelectorMatchResult.NeverThisType)
                {
                    matches ??= new();
                    matches.Add(s);
                }
            }
            else if (style is IEnumerable<IStyle> children)
            {
                foreach (var child in children)
                    Apply(child, target, host, buildMatches, ref matches);
            }
        }
    }
}
