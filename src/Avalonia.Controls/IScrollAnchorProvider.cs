namespace Avalonia.Controls
{
    public interface IScrollAnchorProvider
    {
        IControl CurrentAnchor { get; }
        void RegisterAnchorCandidate(IControl element);
        void UnregisterAnchorCandidate(IControl element);
    }
}
