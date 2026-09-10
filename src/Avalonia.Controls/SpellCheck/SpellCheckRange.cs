namespace Avalonia.Controls;

internal readonly struct SpellCheckRange
{
    public SpellCheckRange(
        int start,
        int end,
        bool startIsInsideWord = false,
        bool endIsInsideWord = false)
    {
        Start = start;
        End = end;
        StartIsInsideWord = startIsInsideWord;
        EndIsInsideWord = endIsInsideWord;
    }

    public int Start { get; }

    public int End { get; }

    // "Word" includes the surrounding address/identifier token needed by the provider.
    public bool StartIsInsideWord { get; }

    public bool EndIsInsideWord { get; }
}
