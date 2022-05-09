using System.Collections.Generic;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Transport
{
    class ListChangeSet<T> : ChangeSet where T : ServerObject
    {
        private List<ListChange<T>>? _listChanges;
        public List<ListChange<T>> ListChanges => _listChanges ??= new List<ListChange<T>>();
        public bool HasListChanges => _listChanges != null;

        public override void Reset()
        {
            _listChanges?.Clear();
            base.Reset();
        }

        public ListChangeSet(IChangeSetPool pool) : base(pool)
        {
        }

        public static readonly ChangeSetPool<ListChangeSet<T>> Pool =
            new ChangeSetPool<ListChangeSet<T>>(pool => new ListChangeSet<T>(pool));
    }
}