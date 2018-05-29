using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;

namespace Avalonia.VisualTree
{
    public class VisualLocator
    {
        public static IObservable<IVisual> Track(IVisual relativeTo, int ancestorLevel, Type ancestorType = null)
        {
            return TrackAttachmentToTree(relativeTo).Select(isAttachedToTree =>
            {
                if (isAttachedToTree)
                {
                    return relativeTo.GetVisualAncestors()
                        .Where(x => ancestorType?.GetTypeInfo().IsAssignableFrom(x.GetType().GetTypeInfo()) ?? true)
                        .ElementAtOrDefault(ancestorLevel);
                }
                else
                {
                    return null;
                }
            });
        }

        private static IObservable<bool> TrackAttachmentToTree(IVisual relativeTo)
        {
            var attached = Observable.FromEventPattern<VisualTreeAttachmentEventArgs>(
                x => relativeTo.AttachedToVisualTree += x,
                x => relativeTo.AttachedToVisualTree -= x)
                .Select(x => true)
                .StartWith(relativeTo.IsAttachedToVisualTree);

            var detached = Observable.FromEventPattern<VisualTreeAttachmentEventArgs>(
                x => relativeTo.DetachedFromVisualTree += x,
                x => relativeTo.DetachedFromVisualTree -= x)
                .Select(x => false);

            var attachmentStatus = attached.Merge(detached);
            return attachmentStatus;
        }
    }
}
