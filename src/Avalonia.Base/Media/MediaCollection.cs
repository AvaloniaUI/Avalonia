using System;
using System.Collections.Generic;
using Avalonia.Collections;

#nullable enable

namespace Avalonia.Media
{
    /// <summary>
    /// Base type for collections of objects that participate in <see cref="MediaInvalidation"/>.
    /// </summary>
    public abstract class MediaCollection<T> : AvaloniaList<T>, IMediaCollection where T : AvaloniaObject
    {
        private readonly MediaParentsBag<AvaloniaObject> _parentHandles = new();

        /// <summary>
        /// Gets an enumeration of living media parents of this collection.
        /// </summary>
        /// <remarks>
        /// To invoke changes to this property, assign the <see cref="MediaCollection{T}"/> to an <see cref="AvaloniaProperty"/> 
        /// which has been passed to <see cref="MediaInvalidation.AffectsMediaRender"/>.
        /// </remarks>
        public IEnumerable<AvaloniaObject> Parents => _parentHandles;

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
               child =>
               {
                   foreach (var parent in Parents)
                   {
                       MediaInvalidation.AddMediaChild(parent, child);
                   }
               },
               child =>
               {
                   foreach (var parent in Parents)
                   {
                       MediaInvalidation.RemoveMediaChild(parent, child);
                   }
               },
               () => throw new NotSupportedException());
        }

        MediaParentsBag<AvaloniaObject> IMediaCollection.Parents => _parentHandles;
        IEnumerable<AvaloniaObject> IMediaCollection.Items => this;
    }

    internal interface IMediaCollection
    {
        MediaParentsBag<AvaloniaObject> Parents { get; }
        IEnumerable<AvaloniaObject> Items { get; } // Inheriting from IEnumerable<AvaloniaObject> would cause MediaCollection<T> to have two IEnumerable<T> interfaces
    }
}
