using System;
using Avalonia.Controls.Documents;

namespace Avalonia.iOS;

internal ref struct CombinedSpan3<T>
{
    public ReadOnlySpan<T> Span1, Span2, Span3;

    public CombinedSpan3(ReadOnlySpan<T> span1, ReadOnlySpan<T> span2, ReadOnlySpan<T> span3)
    {
        Span1 = span1;
        Span2 = span2;
        Span3 = span3;
    }

    public int Length => Span1.Length + Span2.Length + Span3.Length;

    static void CopyFromSpan(ReadOnlySpan<T> from, int offset, ref Span<T> to)
    {
        if(to.Length == 0)
            return;
        if (offset < from.Length)
        {
            var copyNow = Math.Min(from.Length - offset, to.Length);
            from.Slice(offset, copyNow).CopyTo(to);
            to = to.Slice(copyNow);
            offset = 0;
        }
        else
            offset -= from.Length;
    }
    
    public void CopyTo(Span<T> to, int offset)
    {
        CopyFromSpan(Span1, offset, ref to);
        CopyFromSpan(Span2, offset, ref to);
        CopyFromSpan(Span3, offset, ref to);
    }
}
