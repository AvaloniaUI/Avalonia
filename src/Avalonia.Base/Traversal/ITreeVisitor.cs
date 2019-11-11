namespace Avalonia.Traversal
{
    public interface ITreeVisitor<in T>
    {
        TreeVisit Visit(T target);
    }
}