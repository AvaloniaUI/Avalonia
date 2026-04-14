using Avalonia.Metadata;

namespace Avalonia.Controls.Platform
{
    [NotClientImplementable]
    public interface IPlatformFeedback
    {
        bool Play(FeedbackEffect feedback, FeedbackType type);
    }

    public enum FeedbackType
    {
        None,
        Auto,
        Sound,
        Haptic
    }

    public enum FeedbackEffect
    {
        Click,
        LongPress,
    }
}
