using System;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Styling
{
    internal abstract class ValueStyleQuery<T> : StyleQuery
    {
        private readonly StyleQuery? _previous;
        private T _argument;

        internal ValueStyleQuery(StyleQuery? previous, T argument)
        {
            _previous = previous;
            _argument = argument;
        }

        protected T Argument => _argument;

        internal override bool IsCombinator => false;

        public override string ToString(ContainerQuery? owner)
        {
            throw new NotImplementedException();
        }

        private protected override StyleQuery? MovePrevious() => _previous;

        private protected override StyleQuery? MovePreviousOrParent() => _previous;
    }
}
