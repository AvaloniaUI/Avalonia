namespace Avalonia.Traversal
{
    public interface ITreeVisitorWithResult<in T, out TResult> : ITreeVisitor<T>
    {
        TResult Result { get; }
    }
}