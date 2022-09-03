using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Metadata;

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
        }

        void IMediaCollection.AddParent(AvaloniaObject parent)
        {
            _parentHandles.Add(parent);

            foreach (var child in this)
            {
                MediaInvalidation.AddMediaChild(child, parent);
            }
        }

        void IMediaCollection.RemoveParent(AvaloniaObject parent)
        {
            foreach (var child in this)
            {
                MediaInvalidation.RemoveMediaChild(child, parent);
            }

            _parentHandles.Remove(parent);
        }
    }

    internal interface IMediaCollection : IEnumerable
    {
        IEnumerable<AvaloniaObject> Parents { get; }

        void AddParent(AvaloniaObject parent);
        void RemoveParent(AvaloniaObject parent);
    }
}
