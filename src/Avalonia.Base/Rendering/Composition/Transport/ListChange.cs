using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Transport
{
    internal class ListChange<T> where T : ServerObject
    {
        public int Index;
        public ListChangeAction Action;
        public T? Added;
    }

    internal enum ListChangeAction
    {
        InsertAt,
        RemoveAt,
        Clear,
        ReplaceAt
    }
}