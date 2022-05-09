using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server
{
    internal abstract class ServerObject : IExpressionObject
    {
        public ServerCompositor Compositor { get; }

        public virtual long LastChangedBy => ItselfLastChangedBy;
        public long ItselfLastChangedBy { get; private set; }
        
        public ServerObject(ServerCompositor compositor)
        {
            Compositor = compositor;
        }

        protected virtual void ApplyCore(ChangeSet changes)
        {
            
        }

        public void Apply(ChangeSet changes)
        {
            ApplyCore(changes);
            ItselfLastChangedBy = changes.Batch!.SequenceId;
        }

        public virtual ExpressionVariant GetPropertyForAnimation(string name)
        {
            return default;
        }

        ExpressionVariant IExpressionObject.GetProperty(string name) => GetPropertyForAnimation(name);
    }
}