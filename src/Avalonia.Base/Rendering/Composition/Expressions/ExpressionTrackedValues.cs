using System.Collections;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Expressions;

internal class ExpressionTrackedObjects : IEnumerable<IExpressionObject>
{
    private readonly List<IExpressionObject> _list = new();
    private readonly HashSet<IExpressionObject> _hashSet = new();
    
    public void Add(IExpressionObject obj, string member)
    {
        if (_hashSet.Add(obj))
            _list.Add(obj);
    }

    public void Clear()
    {
        _list.Clear();
        _hashSet.Clear();
    }

    IEnumerator<IExpressionObject> IEnumerable<IExpressionObject>.GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_list).GetEnumerator();
    }

    public List<IExpressionObject>.Enumerator GetEnumerator() => _list.GetEnumerator();
    
    public struct Pool
    {
        private readonly Stack<ExpressionTrackedObjects> _stack = new();

        public Pool()
        {
        }

        public ExpressionTrackedObjects Get()
        {
            if (_stack.Count > 0)
                return _stack.Pop();
            return new ExpressionTrackedObjects();
        }

        public void Return(ExpressionTrackedObjects obj)
        {
            _stack.Clear();
            _stack.Push(obj);
        }
    }
}
