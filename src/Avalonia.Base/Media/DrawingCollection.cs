using System.Collections.Generic;
using Avalonia.Collections;

namespace Avalonia.Media
{
    public sealed class DrawingCollection : AvaloniaList<Drawing>
    {
        public DrawingCollection()
        {
            ResetBehavior = ResetBehavior.Remove;
        }

        public DrawingCollection(IEnumerable<Drawing> items) : base(items)
        {
            ResetBehavior = ResetBehavior.Remove;
        }
    }
}
