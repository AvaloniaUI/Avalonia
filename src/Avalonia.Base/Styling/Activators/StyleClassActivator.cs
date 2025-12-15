using System.Collections.Generic;
using Avalonia.Controls;

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

        public static bool AreClassesMatching(IReadOnlyList<string> classes, IList<string> toMatch)
        {
            int toMatchCount = toMatch.Count;
            int classesCount = classes.Count;

            // Early bail out - we can't match if control does not have enough classes.
            if (classesCount < toMatchCount)
            {
                return false;
            }

            // For small toMatch lists (common case), linear search is faster than HashSet overhead.
            // Threshold of 4 balances HashSet allocation cost vs O(n*m) search cost.
            if (toMatchCount <= 4)
            {
                int remainingMatches = toMatchCount;
                
                for (var i = 0; i < classesCount; i++)
                {
                    var c = classes[i];

                    // Linear search through toMatch - O(m) but m <= 4
                    for (var j = 0; j < toMatchCount; j++)
                    {
                        if (toMatch[j] == c)
                        {
                            --remainingMatches;
                            if (remainingMatches == 0)
                            {
                                return true;
                            }
                            break;
                        }
                    }
                }

                return remainingMatches == 0;
            }
            else
            {
                // For larger toMatch lists, use HashSet for O(1) lookups.
                // This converts O(n*m) to O(n+m).
                var toMatchSet = new HashSet<string>(toMatch);
                int remainingMatches = toMatchSet.Count;

                for (var i = 0; i < classesCount; i++)
                {
                    if (toMatchSet.Remove(classes[i]))
                    {
                        --remainingMatches;
                        if (remainingMatches == 0)
                        {
                            return true;
                        }
                    }
                }

                return remainingMatches == 0;
            }
        }

        void IClassesChangedListener.Changed() => ReevaluateIsActive();
        protected override bool EvaluateIsActive() => AreClassesMatching(_classes, _match);
        protected override void Initialize() => _classes.AddListener(this);
        protected override void Deinitialize() => _classes.RemoveListener(this);
    }
}
