namespace Avalonia.Styling
{
    public interface IStyleWithCancel : IStyle
    {
        bool IsCancel { get; }

        Selector Selector { get; }
    }
}
