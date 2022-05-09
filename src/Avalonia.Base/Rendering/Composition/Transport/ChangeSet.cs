using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Transport
{
    internal abstract class ChangeSet
    {
        private readonly IChangeSetPool _pool;
        public Batch Batch = null!;
        public ServerObject? Target;
        public bool Dispose;

        public ChangeSet(IChangeSetPool pool)
        {
            _pool = pool;
        }

        public virtual void Reset()
        {
            Batch = null!;
            Target = null;
            Dispose = false;
        }

        public void Return()
        {
            _pool.Return(this);
        }
    }

    internal class CompositionObjectChanges : ChangeSet
    {
        public CompositionObjectChanges(IChangeSetPool pool) : base(pool)
        {
        }
    }
}