using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Styling.Activators
{
    /// <summary>
    /// An <see cref="IStyleActivator"/> which is active when a set of classes match those on a
    /// control.
    /// </summary>
    internal sealed class StyleClassActivator : StyleActivatorBase, IClassesChangedListener
    {
        private readonly IList<string> _match;
        private readonly Classes _classes;

        public StyleClassActivator(Classes classes, IList<string> match)
        {
            _classes = classes;
            _match = match;
        }

        public override bool IsActive => AreClassesMatching(_classes, _match);

        public static bool AreClassesMatching(IReadOnlyList<string> classes, IList<string> toMatch)
        {
            int remainingMatches = toMatch.Count;
            int classesCount = classes.Count;

            // Early bail out - we can't match if control does not have enough classes.
            if (classesCount < remainingMatches)
            {
                return false;
            }

            for (var i = 0; i < classesCount; i++)
            {
                var c = classes[i];

                if (toMatch.Contains(c))
                {
                    --remainingMatches;

                    // Already matched so we can skip checking other classes.
                    if (remainingMatches == 0)
                    {
                        break;
                    }
                }
            }

            return remainingMatches == 0;
        }

        void IClassesChangedListener.Changed()
        {
            PublishNext(IsActive);
        }

        protected override void Initialize()
        {
            PublishNext(IsActive);
            _classes.AddListener(this);
        }

        protected override void Deinitialize()
        {
            _classes.RemoveListener(this);
        }
    }
}
