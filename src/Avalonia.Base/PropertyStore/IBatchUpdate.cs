namespace Avalonia.PropertyStore
{
    internal interface IBatchUpdate
    {
        void BeginBatchUpdate();
        void EndBatchUpdate();
    }
}
