namespace Avalonia.Markup.Xaml
{
    public interface IAddChild
    {
        void AddChild(object child);
    }

    public interface IAddChild<T> : IAddChild
    {
        void AddChild(T child);
    }
}
