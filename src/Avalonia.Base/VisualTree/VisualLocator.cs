using System;
using Avalonia.Reactive;

namespace Avalonia.VisualTree
{
    public class VisualLocator
    {
        public static IObservable<Visual?> Track(Visual relativeTo, int ancestorLevel, Type? ancestorType = null)
        {
            return new VisualTracker(relativeTo, ancestorLevel, ancestorType);
        }

        private class VisualTracker : LightweightObservableBase<Visual?>
        {
            private readonly Visual _relativeTo;
            private readonly int _ancestorLevel;
            private readonly Type? _ancestorType;

            public VisualTracker(Visual relativeTo, int ancestorLevel, Type? ancestorType)
            {
                _relativeTo = relativeTo;
                _ancestorLevel = ancestorLevel;
                _ancestorType = ancestorType;
            }

            protected override void Initialize()
            {
                _relativeTo.AttachedToVisualTree += AttachedDetached;
                _relativeTo.DetachedFromVisualTree += AttachedDetached;
            }

            protected override void Deinitialize()
            {
                _relativeTo.AttachedToVisualTree -= AttachedDetached;
                _relativeTo.DetachedFromVisualTree -= AttachedDetached;
            }

            protected override void Subscribed(IObserver<Visual?> observer, bool first)
            {
                observer.OnNext(GetResult());
            }

            private void AttachedDetached(object? sender, VisualTreeAttachmentEventArgs e) => PublishNext(GetResult());

            private Visual? GetResult()
            {
                if (!_relativeTo.IsAttachedToVisualTree)
                    return null;

                // Walk ancestor chain manually instead of using LINQ
                int matchCount = 0;
                foreach (var ancestor in _relativeTo.GetVisualAncestors())
                {
                    if (_ancestorType is null || _ancestorType.IsInstanceOfType(ancestor))
                    {
                        if (matchCount == _ancestorLevel)
                            return ancestor;
                        matchCount++;
                    }
                }

                return null;
            }
        }
    }
}
