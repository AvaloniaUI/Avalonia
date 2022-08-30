using System.Collections.Generic;
using Avalonia.Collections;

namespace Avalonia.Media
{
    public sealed class DrawingCollection : MediaCollection<Drawing>
    {
        public DrawingCollection()
        {
        }

        public DrawingCollection(IEnumerable<Drawing> items) : base(items)
        {
        }
    }
}
