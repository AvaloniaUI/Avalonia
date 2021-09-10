#nullable enable
using System;
using System.Text;

using Avalonia.LogicalTree;
using Avalonia.Styling.Activators;

namespace Avalonia.Styling
{
    public class NthChildSelector : Selector
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

        public NthChildSelector(Selector? previous, int step, int offset)
            : this(previous, step, offset, false)
        {

        }

        public override bool InTemplate => _previous?.InTemplate ?? false;

        public override bool IsCombinator => false;

        public override Type? TargetType => _previous?.TargetType;

        public int Step { get; }
        public int Offset { get; }

        protected override SelectorMatch Evaluate(IStyleable control, bool subscribe)
        {
            var logical = (ILogical)control;
            var controlParent = logical.LogicalParent;

            if (controlParent is IChildIndexProvider childIndexProvider)
            {
                return subscribe
                    ? new SelectorMatch(new NthChildActivator(logical, childIndexProvider, Step, Offset, _reversed))
                    : Evaluate(logical, childIndexProvider, Step, Offset, _reversed);
            }
            else
            {
                return SelectorMatch.NeverThisInstance;
            }
        }

        internal static SelectorMatch Evaluate(
            ILogical logical, IChildIndexProvider childIndexProvider,
            int step, int offset, bool reversed)
        {
            var index = childIndexProvider.GetChildIndex(logical);
            if (index < 0)
            {
                return SelectorMatch.NeverThisInstance;
            }

            if (reversed)
            {
                if (childIndexProvider.TotalCount is int totalCountValue)
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

        protected override Selector? MovePrevious() => _previous;

        public override string ToString()
        {
            var expectedCapacity = NthLastChildSelectorName.Length + 8;
            var stringBuilder = new StringBuilder(_previous?.ToString(), expectedCapacity);
            
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

            return stringBuilder.ToString();
        }
    }
}
