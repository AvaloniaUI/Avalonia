#nullable enable

namespace Avalonia.Styling
{
    public class NthLastChildSelector : NthChildSelector
    {
        public NthLastChildSelector(Selector? previous, int step, int offset) : base(previous, step, offset, true)
        {
        }
    }
}
