using System;

namespace Avalonia.Media
{
    internal readonly record struct FontSourceIdentifier
    {
        public FontSourceIdentifier(string name, Uri? source)
        {
            Name = name;
            Source = source;
        }

        public string Name { get; init; }

        public Uri? Source { get; init; }
    }
}
