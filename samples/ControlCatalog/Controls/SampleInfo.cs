using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace ControlCatalog.Controls
{
    /// <summary>
    /// One entry in a <see cref="SampleGalleryPage"/> registry: a card on the gallery home page that opens the
    /// control created by <see cref="Factory"/>. Construction is deferred until the card is clicked.
    /// </summary>
    public sealed class SampleInfo(string group, string title, string description, Func<Control> factory)
    {
        public string Group { get; } = group;

        public string Title { get; } = title;

        public string Description { get; } = description;

        public Func<Control> Factory { get; } = factory;
    }

    /// <summary>
    /// The samples of one group, as shown under a single header on a <see cref="SampleGalleryPage"/>.
    /// </summary>
    public sealed class SampleInfoGroup(string header, IReadOnlyList<SampleInfo> samples)
    {
        public string Header { get; } = header;

        public IReadOnlyList<SampleInfo> Samples { get; } = samples;
    }

    /// <summary>
    /// The group names shared by every sample gallery, in the order they are shown. A registry may use other
    /// group names; those are appended after the known ones in the order they first appear.
    /// </summary>
    public static class SampleGroups
    {
        public const string Overview = "Overview";
        public const string Populate = "Populate";
        public const string Appearance = "Appearance";
        public const string Features = "Features";
        public const string Events = "Events";
        public const string Performance = "Performance";
        public const string Showcases = "Showcases";

        private static readonly string[] s_orderedNames =
        [
            Overview, Populate, Appearance, Features, Events, Performance, Showcases
        ];

        internal static int IndexOf(string group)
        {
            var index = Array.IndexOf(s_orderedNames, group);
            return index < 0 ? int.MaxValue : index;
        }
    }
}
