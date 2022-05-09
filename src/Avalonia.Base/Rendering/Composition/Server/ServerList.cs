using System;
using System.Collections.Generic;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server
{
    class ServerList<T> : ServerObject where T : ServerObject
    {
        public List<T> List { get; } = new List<T>();
        protected override void ApplyCore(ChangeSet changes)
        {
            var c = (ListChangeSet<T>) changes;
            if (c.HasListChanges)
            {
                foreach (var lc in c.ListChanges)
                {
                    if(lc.Action == ListChangeAction.Clear)
                        List.Clear();
                    if(lc.Action == ListChangeAction.RemoveAt)
                        List.RemoveAt(lc.Index);
                    if(lc.Action == ListChangeAction.InsertAt)
                        List.Insert(lc.Index, lc.Added!);
                    if (lc.Action == ListChangeAction.ReplaceAt)
                        List[lc.Index] = lc.Added!;
                }
            }
        }
        
        public override long LastChangedBy
        {
            get
            {
                var seq = base.LastChangedBy;
                foreach (var i in List)
                    seq = Math.Max(i.LastChangedBy, seq);
                return seq;
            }
        }

        public List<T>.Enumerator GetEnumerator() => List.GetEnumerator();

        public ServerList(ServerCompositor compositor) : base(compositor)
        {
        }
    }
}