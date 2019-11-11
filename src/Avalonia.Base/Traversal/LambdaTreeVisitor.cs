using System;

namespace Avalonia.Traversal
{
    public readonly struct LambdaTreeVisitor<T> : ITreeVisitor<T>
    {
        private readonly Func<T, TreeVisit> _operation;

        public LambdaTreeVisitor(Func<T, TreeVisit> operation)
        {
            _operation = operation;
        }

        public TreeVisit Visit(T target)
        {
            return _operation(target);
        }
    }

    public struct StatefulLambdaTreeVisitor<T, TState> : ITreeVisitor<T>
    {
        private readonly Func<T, TState, TreeVisit> _operation;
        private TState _state;

        public StatefulLambdaTreeVisitor(Func<T, TState, TreeVisit> operation, TState state)
        {
            _operation = operation;
            _state = state;
        }

        public TreeVisit Visit(T target)
        {
            return _operation(target, _state);
        }
    }
}
