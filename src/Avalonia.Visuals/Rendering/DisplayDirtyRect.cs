using System;

namespace Avalonia.Rendering
{
    public class DisplayDirtyRect
    {
        public static readonly TimeSpan TimeToLive = TimeSpan.FromMilliseconds(500);

        public DisplayDirtyRect(Rect rect)
        {
            Rect = rect;
            ResetAge();
        }

        public Rect Rect { get; }
        public DateTimeOffset Born { get; private set; }
        public DateTimeOffset Dies { get; private set; }

        public double Opacity => (Dies - DateTimeOffset.UtcNow).TotalMilliseconds / TimeToLive.TotalMilliseconds;

        public void ResetAge()
        {
            Born = DateTimeOffset.UtcNow;
            Dies = Born + TimeToLive;
        }
    }
}
