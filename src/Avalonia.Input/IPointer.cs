namespace Avalonia.Input
{
    public interface IPointer
    {
        int Id { get; }
        void Capture(IInputElement control);
        IInputElement Captured { get; }
        PointerType Type { get; }
        bool IsPrimary { get; }
        
    }

    public enum PointerType
    {
        Mouse,
        Touch
    }

    public class PointerIds
    {
        private static int s_nextPointerId = 1000;
        public static int Next() => s_nextPointerId++;
    }
}
