using System.Threading.Tasks;

namespace Avalonia.Controls
{
    public class ChildNameScope : INameScope
    {
        private readonly INameScope _parentScope;
        private readonly NameScope _inner = new NameScope();

        public ChildNameScope(INameScope parentScope)
        {
            _parentScope = parentScope;
        }
        
        public void Register(string name, object element) => _inner.Register(name, element);

        public ValueTask<object> FindAsync(string name)
        {
            var found = Find(name);
            if (found != null)
                return new ValueTask<object>(found);
            // Not found and both current and parent scope are in completed stage
            if(IsCompleted)
                return new ValueTask<object>(null);
            return DoFindAsync(name);
        }

        async ValueTask<object> DoFindAsync(string name)
        {
            if (!_inner.IsCompleted)
            {
                var found = await _inner.FindAsync(name);
                if (found != null)
                    return found;
            }

            return await _parentScope.FindAsync(name);
        }

        public object Find(string name)
        {
            var found = _inner.Find(name);
            if (found != null)
                return found;
            if (_inner.IsCompleted)
                return _parentScope.Find(name);
            return null;
        }

        public void Complete() => _inner.Complete();

        public bool IsCompleted => _inner.IsCompleted && _parentScope.IsCompleted;
    }
}
