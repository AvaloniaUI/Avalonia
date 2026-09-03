using System;
using System.Collections.Generic;
using Avalonia.Collections;

#nullable enable

namespace Avalonia.Media
{
    public sealed class GeometryCollection : AvaloniaList<Geometry> 
    {
        public GeometryCollection()
        {
            ResetBehavior = ResetBehavior.Remove;

            this.ForEachItem(
               x =>
               {
                   x.Changed += ChildChanged;
                   Parent?.Invalidate();
               },
               x =>
               {
                   x.Changed -= ChildChanged;
                   Parent?.Invalidate();
               },
               () => throw new NotSupportedException());
        }

        public GeometryCollection(IEnumerable<Geometry> items) : base(items)
        {
            ResetBehavior = ResetBehavior.Remove;

            this.ForEachItem(
               x =>
               {
                   x.Changed += ChildChanged;
                   Parent?.Invalidate();
               },
               x =>
               {
                   x.Changed -= ChildChanged;
                   Parent?.Invalidate();
               },
               () => throw new NotSupportedException());
        }

        public GeometryGroup? Parent { get; set; }

        private void ChildChanged(object? sender, EventArgs e)
            => Parent?.Invalidate();
    }
}
