using System;
using System.Collections.Generic;
using Avalonia.Collections;

#nullable enable

namespace Avalonia.Media
{
    public abstract class MediaCollection<T> : AvaloniaList<T>, IMediaCollection where T : AvaloniaObject
    {
        public AvaloniaList<AvaloniaObject> Parents { get; } = new() { ResetBehavior = ResetBehavior.Remove };

        protected MediaCollection()
        {
            Setup();
        }

        protected MediaCollection(IEnumerable<T> items) : base(items)
        {
            Setup();
        }

        private void Setup()
        {
            ResetBehavior = ResetBehavior.Remove;

            this.ForEachItem(
               x =>
               {
                   foreach (var parent in Parents)
                   {
                       MediaInvalidation.AddMediaChild(x, parent);
                   }
               },
               x =>
               {
                   foreach (var parent in Parents)
                   {
                       MediaInvalidation.RemoveMediaChild(x, parent);
                   }
               },
               () => throw new NotSupportedException());

            Parents.ForEachItem(
                x =>
                {
                    foreach (var child in this)
                    {
                        MediaInvalidation.AddMediaChild(child, x);
                    }
                },
                x =>
                {
                    foreach (var child in this)
                    {
                        MediaInvalidation.RemoveMediaChild(child, x);
                    }
                },
               () => throw new NotSupportedException());
        }
    }

    public interface IMediaCollection
    {
        AvaloniaList<AvaloniaObject> Parents { get; }
    }
}
