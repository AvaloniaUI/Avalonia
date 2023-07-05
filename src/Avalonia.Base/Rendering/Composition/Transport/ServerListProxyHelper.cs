using System.Collections;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Transport
{
    /// <summary>
    /// A helper class used from generated UI-thread-side collections of composition objects.
    /// </summary>
    // NOTE: This should probably be a base class since TServer isn't used anymore and it was the reason why 
    // it couldn't be exposed as a base class
    class ServerListProxyHelper<TClient, TServer> : IList<TClient>
        where TServer : ServerObject
        where TClient : CompositionObject
    {
        private readonly IRegisterForSerialization _parent;
        private bool _changed;

        public interface IRegisterForSerialization
        {
            void RegisterForSerialization();
        }

        public ServerListProxyHelper(IRegisterForSerialization parent)
        {
            _parent = parent;
        }
        
        private readonly List<TClient> _list = new List<TClient>();
        
        IEnumerator<TClient> IEnumerable<TClient>.GetEnumerator() => GetEnumerator();
        public List<TClient>.Enumerator GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(TClient item) => Insert(_list.Count, item);

        public void Clear()
        {
            _list.Clear();
            _changed = true;
            _parent.RegisterForSerialization();
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
            _changed = true;
            _parent.RegisterForSerialization();
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
            _changed = true;
            _parent.RegisterForSerialization();
        }

        public TClient this[int index]
        {
            get => _list[index];
            set
            {
                _list[index] = value;
                _changed = true;
                _parent.RegisterForSerialization();
            }
        }

        public void Serialize(BatchStreamWriter writer)
        {
            writer.Write((byte)(_changed ? 1 : 0));
            if (_changed)
            {
                writer.Write(_list.Count);
                foreach (var el in _list)
                    writer.WriteObject(el.Server);
            }
            _changed = false;
        }
    }
}
