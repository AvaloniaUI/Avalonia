using System.Collections.Generic;
using Avalonia.Reactive;

#nullable enable

namespace Avalonia.Media
{
    public static class MediaInvalidation
    {
        private static readonly HashSet<AvaloniaProperty> s_registeredProperties = new();

        public static void AffectsMediaRender(params AvaloniaProperty[] properties)
        {
            foreach (var property in properties)
            {
                if (s_registeredProperties.Add(property))
                {
                    property.Changed.Subscribe(OnMediaRenderPropertyChanged);
                }
            }
        }

        internal static void AddMediaChild(AvaloniaObject child, AvaloniaObject parent)
        {
            child.GetOrCreateMediaParents().Add(parent);
            InvalidateAncestors(child);
        }

        internal static void RemoveMediaChild(AvaloniaObject child, AvaloniaObject parent)
        {
            InvalidateAncestors(child);
            child.GetMediaParents()?.Remove(parent);
        }

        private static void OnMediaRenderPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue is AvaloniaObject newChild)
            {
                newChild.GetOrCreateMediaParents().Add(e.Sender);
            }

            if (e.NewValue is IMediaCollection newCollection)
            {
                newCollection.Parents.Add(e.Sender);
            }

            InvalidateAncestors(e.Sender);

            if (e.OldValue is AvaloniaObject oldChild)
            {
                oldChild.GetMediaParents()?.Remove(e.Sender);
            }

            if (e.OldValue is IMediaCollection oldCollection)
            {
                oldCollection.Parents.Remove(e.Sender);
            }
        }

        public static void InvalidateAncestors(AvaloniaObject mediaObject)
        {
            foreach (var ancestor in GetMediaAncestors<Visual>(mediaObject, new()))
            {
                ancestor.InvalidateVisual();
            }
        }

        private static IEnumerable<T> GetMediaAncestors<T>(AvaloniaObject current, HashSet<AvaloniaObject> visited) where T : AvaloniaObject
        {
            var parents = current.GetMediaParents();
            if (parents == null)
            {
                yield break;
            }

            foreach (var parent in parents)
            {
                if (!visited.Add(parent))
                {
                    continue;
                }

                if (parent is T target)
                {
                    yield return target;
                }
                else
                {
                    foreach (var ancestor in GetMediaAncestors<T>(parent, visited))
                    {
                        yield return ancestor;
                    }
                }
            }
        }
    }
}
