using System;

namespace Avalonia.Traversal
{
    public struct LambdaTreeVisitorWithResult<T> : ITreeVisitorWithResult<T, T>
    {
        private readonly Func<T, TreeVisit> _operation;

        public LambdaTreeVisitorWithResult(Func<T, TreeVisit> operation)
        {
            _operation = operation;

            Result = default;
        }

        public T Result { get; private set; }

        public TreeVisit Visit(T target)
        {
            if (_operation(target) == TreeVisit.Stop)
            {
                Result = target;

                return TreeVisit.Stop;
            }

            return TreeVisit.Continue;
        }
    }

    public struct StatefulLambdaTreeVisitorWithResult<T, TState> : ITreeVisitorWithResult<T, T>
    {
        private readonly Func<T,TState, TreeVisit> _operation;
        private TState _state;

        public StatefulLambdaTreeVisitorWithResult(Func<T, TState, TreeVisit> operation, TState state)
        {
            _operation = operation;
            _state = state;

            Result = default;
        }

        public T Result { get; private set; }

        public TreeVisit Visit(T target)
        {
            if (_operation(target, _state) == TreeVisit.Stop)
            {
                Result = target;

                return TreeVisit.Stop;
            }

            return TreeVisit.Continue;
        }
    }
}
