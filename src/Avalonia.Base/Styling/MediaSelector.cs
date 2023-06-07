using System;

namespace Avalonia.Styling
{
    public abstract class MediaSelector<T> : Selector
    {
        private readonly Selector? _previous;
        private T _argument;

        public MediaSelector(Selector? previous, T argument)
        {
            _previous = previous;
            _argument = argument;
        }

        protected T Argument => _argument;

        internal override bool InTemplate => _previous?.InTemplate ?? false;

        internal override bool IsCombinator => false;

        internal override Type? TargetType => _previous?.TargetType;

        public override string ToString(Style? owner)
        {
            throw new NotImplementedException();
        }

        private protected override Selector? MovePrevious() => _previous;

        private protected override Selector? MovePreviousOrParent() => _previous;
    }
}
