using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Avalonia.Utilities
{
    /// <summary>
    /// A task-like operation that is guaranteed to finish continuations synchronously,
    /// can be used for parametrized one-shot events
    /// </summary>
    public record struct SynchronousCompletionAsyncResult<T> : INotifyCompletion
    {
        private readonly SynchronousCompletionAsyncResultSource<T>? _source;
        private readonly T? _result;
        private readonly bool _isValid;
        internal SynchronousCompletionAsyncResult(SynchronousCompletionAsyncResultSource<T> source)
        {
            _source = source;
            _result = default;
            _isValid = true;
        }

        public SynchronousCompletionAsyncResult(T result)
        {
            _result = result;
            _source = null;
            _isValid = true;
        }

        static void ThrowNotInitialized() =>
            throw new InvalidOperationException("This SynchronousCompletionAsyncResult was not initialized");

        public bool IsCompleted
        {
            get
            {
                if (!_isValid)
                    ThrowNotInitialized();
                return _source == null || _source.IsCompleted;
            }
        }

        public T GetResult()
        {
            if (!_isValid)
                ThrowNotInitialized();
            return _source == null ? _result! : _source.Result;
        }


        public void OnCompleted(Action continuation)
        {
            if (!_isValid)
                ThrowNotInitialized();
            if (_source == null)
                continuation();
            else
                _source.OnCompleted(continuation);
        }

        public SynchronousCompletionAsyncResult<T> GetAwaiter() => this;
    }

    /// <summary>
    /// Source for incomplete SynchronousCompletionAsyncResult
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SynchronousCompletionAsyncResultSource<T>
    {
        private T? _result;
        internal bool IsCompleted { get; private set; }
        public SynchronousCompletionAsyncResult<T> AsyncResult => new SynchronousCompletionAsyncResult<T>(this);
        
        internal T Result => IsCompleted ?
            _result! :
            throw new InvalidOperationException("Asynchronous operation is not yet completed");
        
        private List<Action>? _continuations;

        internal void OnCompleted(Action continuation)
        {
            if(_continuations==null)
                _continuations = new List<Action>();
            _continuations.Add(continuation);
        }

        public void SetResult(T result)
        {
            if (IsCompleted)
                throw new InvalidOperationException("Asynchronous operation is already completed");
            _result = result;
            IsCompleted = true;
            if(_continuations!=null)
                foreach (var c in _continuations)
                    c();
            _continuations = null;
        }

        public void TrySetResult(T result)
        {
            if(IsCompleted)
                return;
            SetResult(result);
        }
    }
}
