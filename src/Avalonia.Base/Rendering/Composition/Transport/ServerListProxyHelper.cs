using System.Collections;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Transport
{
    class ServerListProxyHelper<TClient, TServer> : IList<TClient>
        where TServer : ServerObject
        where TClient : CompositionObject
    {
        private readonly IGetChanges _parent;
        private readonly List<TClient> _list = new List<TClient>();

        public interface IGetChanges
        {
            ListChangeSet<TServer> GetChanges();
        }

        public ServerListProxyHelper(IGetChanges parent)
        {
            _parent = parent;
        }

        IEnumerator<TClient> IEnumerable<TClient>.GetEnumerator() => GetEnumerator();
        public List<TClient>.Enumerator GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(TClient item) => Insert(_list.Count, item);

        public void Clear()
        {
            _list.Clear();
            _parent.GetChanges().ListChanges.Add(new ListChange<TServer>
            {
                Action = ListChangeAction.Clear
            });
        }

        public bool Contains(TClient item) => _list.Contains(item);

        public void CopyTo(TClient[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        public bool Remove(TClient item)
        {
            var idx = _list.IndexOf(item);
            if (idx == -1)
                return false;
            RemoveAt(idx);
            return true;
        }

        public int Count => _list.Count;
        public bool IsReadOnly => false;
        public int IndexOf(TClient item) => _list.IndexOf(item);

        public void Insert(int index, TClient item)
        {
            _list.Insert(index, item);
            _parent.GetChanges().ListChanges.Add(new ListChange<TServer>
            {
                Action = ListChangeAction.InsertAt,
                Index = index,
                Added = (TServer) item.Server
            });
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
            _parent.GetChanges().ListChanges.Add(new ListChange<TServer>
            {
                Action = ListChangeAction.RemoveAt,
                Index = index
            });
        }

        public TClient this[int index]
        {
            get => _list[index];
            set
            {
                _list[index] = value;
                _parent.GetChanges().ListChanges.Add(new ListChange<TServer>
                {
                    Action = ListChangeAction.ReplaceAt,
                    Index = index,
                    Added = (TServer) value.Server
                });
            }
        }
    }
}