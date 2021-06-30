#nullable enable

namespace Avalonia.Styling
{
    public interface ISetter
    {
        ISetterInstance Instance(IStyleInstance styleInstance, IStyleable target);
    }
}
