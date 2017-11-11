using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Media.Immutable
{
    public interface IImmutableBrush : IBrush
    {
        IImmutableBrush WithOpacity(double opacity);
    }
}
