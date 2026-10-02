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
        // Holds the previous items while a new list is read, so that both can be compared without allocating.
        private List<T>? _oldList;

        public List<T> List { get; private set; } = new List<T>();

        protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
        {
            if (reader.Read<byte>() == 1)
            {
                var oldList = List;
                // An empty list has nothing to compare against, so a list that is filled once never allocates the
                // second instance.
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

        /// <summary>
        /// Called after the items of a non-empty <see cref="List"/> have been replaced with the items received
        /// from the UI thread.
        /// </summary>
        /// <param name="oldList">The items that the list contained before the change.</param>
        protected virtual void OnListChanged(List<T> oldList)
        {
        }

        public List<T>.Enumerator GetEnumerator() => List.GetEnumerator();

        public ServerList(ServerCompositor compositor) : base(compositor)
        {
        }
    }
}
