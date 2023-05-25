#nullable enable
using System;
using Avalonia.LogicalTree;
using Avalonia.Styling.Activators;
using Avalonia.Utilities;

namespace Avalonia.Styling
{
    /// <summary>
    /// The :nth-child() pseudo-class matches elements based on their position in a group of siblings.
    /// </summary>
    /// <remarks>
    /// Element indices are 1-based.
    /// </remarks>
    internal class NthChildSelector : Selector
    {
        private const string NthChildSelectorName = "nth-child";
        private const string NthLastChildSelectorName = "nth-last-child";
        private readonly Selector? _previous;
        private readonly bool _reversed;

        internal protected NthChildSelector(Selector? previous, int step, int offset, bool reversed)
        {
            _previous = previous;
            Step = step;
            Offset = offset;
            _reversed = reversed;
        }

        /// <summary>
        /// Creates an instance of <see cref="NthChildSelector"/>
        /// </summary>
        /// <param name="previous">Previous selector.</param>
        /// <param name="step">Position step.</param>
        /// <param name="offset">Initial index offset.</param>
        public NthChildSelector(Selector? previous, int step, int offset)
            : this(previous, step, offset, false)
        {

        }

        internal override bool InTemplate => _previous?.InTemplate ?? false;

        internal override bool IsCombinator => false;

        internal override Type? TargetType => _previous?.TargetType;

        public int Step { get; }
        public int Offset { get; }

        private protected override SelectorMatch Evaluate(StyledElement control, IStyle? parent, bool subscribe)
        {
            if (!(control is ILogical logical))
            {
                return SelectorMatch.NeverThisType;
            }

            var controlParent = logical.LogicalParent;

            if (controlParent is IChildIndexProvider childIndexProvider)
            {
                return subscribe
                    ? new SelectorMatch(new NthChildActivator(logical, childIndexProvider, Step, Offset, _reversed))
                    : Evaluate(childIndexProvider.GetChildIndex(logical), childIndexProvider, Step, Offset, _reversed);
            }
            else
            {
                return SelectorMatch.NeverThisInstance;
            }
        }

        internal static SelectorMatch Evaluate(
            int index, IChildIndexProvider childIndexProvider,
            int step, int offset, bool reversed)
        {
            if (index < 0)
            {
                return SelectorMatch.NeverThisInstance;
            }

            if (reversed)
            {
                if (childIndexProvider.TryGetTotalCount(out var totalCountValue))
                {
                    index = totalCountValue - index;
                }
                else
                {
                    return SelectorMatch.NeverThisInstance;
                }
            }
            else
            {
                // nth child index is 1-based
                index += 1;
            }

            var n = Math.Sign(step);

            var diff = index - offset;
            var match = diff == 0 || (Math.Sign(diff) == n && diff % step == 0);

            return match ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
        }

        private protected override Selector? MovePrevious() => _previous;
        private protected override Selector? MovePreviousOrParent() => _previous;

        public override string ToString(Style? owner)
        {
            var expectedCapacity = NthLastChildSelectorName.Length + 8;
            var stringBuilder =  StringBuilderCache.Acquire(expectedCapacity);
            stringBuilder.Append(_previous?.ToString(owner));
            
            stringBuilder.Append(':');
            stringBuilder.Append(_reversed ? NthLastChildSelectorName : NthChildSelectorName);
            stringBuilder.Append('(');

            var hasStep = false;
            if (Step != 0)
            {
                hasStep = true;
                stringBuilder.Append(Step);
                stringBuilder.Append('n');
            }

            if (Offset > 0)
            {
                if (hasStep)
                {
                    stringBuilder.Append('+');
                }
                stringBuilder.Append(Offset);
            }
            else if (Offset < 0)
            {
                stringBuilder.Append('-');
                stringBuilder.Append(-Offset);
            }

            stringBuilder.Append(')');

            return StringBuilderCache.GetStringAndRelease(stringBuilder);
        }
    }
}
