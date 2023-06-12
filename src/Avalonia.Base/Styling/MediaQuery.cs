using System;

namespace Avalonia.Styling
{
    public abstract class MediaQuery<T> : Query
    {
        private readonly Query? _previous;
        private T _argument;

        public MediaQuery(Query? previous, T argument)
        {
            _previous = previous;
            _argument = argument;
        }

        protected T Argument => _argument;

        internal override bool IsCombinator => false;

        public override string ToString(Media? owner)
        {
            throw new NotImplementedException();
        }

        private protected override Query? MovePrevious() => _previous;

        private protected override Query? MovePreviousOrParent() => _previous;
    }
}
