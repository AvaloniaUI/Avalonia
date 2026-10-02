using System;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server
{
    /// <summary>
    /// A server-side list container capable of receiving changes from the UI thread
    /// Right now it's quite dumb since it always receives the full list
    /// </summary>
    class ServerList<T> : ServerObject where T : ServerObject
    {
        private List<T>? _oldList;

        public List<T> List { get; private set; } = new List<T>();

        protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
        {
            if (reader.Read<byte>() == 1)
            {
                var oldList = List;
                if (oldList.Count > 0)
                {
                    List = _oldList ?? new List<T>();
                    _oldList = oldList;
                }

                var count = reader.Read<int>();
                for (var c = 0; c < count; c++) 
                    List.Add(reader.ReadObject<T>());

                if (!ReferenceEquals(oldList, List))
                {
                    OnListChanged(oldList);
                    oldList.Clear();
                }
            }
            base.DeserializeChangesCore(reader, committedAt);
        }

        protected virtual void OnListChanged(List<T> oldList)
        {
        }

        public List<T>.Enumerator GetEnumerator() => List.GetEnumerator();

        public ServerList(ServerCompositor compositor) : base(compositor)
        {
        }
    }
}
