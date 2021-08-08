#nullable enable
using System;
using System.Text;

using Avalonia.LogicalTree;

namespace Avalonia.Styling
{
    public interface IChildIndexProvider
    {
        (int Index, int? TotalCount) GetChildIndex(ILogical child);
    }

    public class NthLastChildSelector : NthChildSelector
    {
        public NthLastChildSelector(Selector? previous, int step, int offset) : base(previous, step, offset, true)
        {
        }
    }

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
                var (index, totalCount) = childIndexProvider.GetChildIndex(logical);
                if (index < 0)
                {
                    return SelectorMatch.NeverThisInstance;
                }

                if (_reversed)
                {
                    if (totalCount is int totalCountValue)
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
                

                var n = Math.Sign(Step);

                var diff = index - Offset;
                var match = diff == 0 || (Math.Sign(diff) == n && diff % Step == 0);

                return match ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;
            }
            else
            {
                return SelectorMatch.NeverThisInstance;
            }

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
