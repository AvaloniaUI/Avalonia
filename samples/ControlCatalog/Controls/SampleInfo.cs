using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace ControlCatalog.Controls
{
    /// <summary>
    /// One entry in a <see cref="SampleGalleryPage"/> registry: a card on the gallery home page that opens the
    /// control created by <see cref="Factory"/>. Construction is deferred until the card is clicked.
    /// </summary>
    public sealed class SampleInfo
    {
        public SampleInfo(string group, string title, string description, Func<Control> factory)
        {
            Group = group ?? throw new ArgumentNullException(nameof(group));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public string Group { get; }

        public string Title { get; }

        public string Description { get; }

        public Func<Control> Factory { get; }
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

        private static readonly IReadOnlyList<string> s_order =
        [
            Overview, Populate, Appearance, Features, Events, Performance, Showcases
        ];

        public static IReadOnlyList<string> Order => s_order;

        internal static int IndexOf(string group)
        {
            for (var i = 0; i < s_order.Count; i++)
            {
                if (string.Equals(s_order[i], group, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return int.MaxValue;
        }
    }
}
