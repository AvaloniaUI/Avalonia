using System.Collections.Generic;
using Avalonia.Reactive;

#nullable enable

namespace Avalonia.Media
{
    /// <summary>
    /// A system which consumes changes to <see cref="AvaloniaObject"/> instances which exist outside the visual tree, locates the <see cref="Visual"/> objects which 
    /// depend on them for their rendered appearance, and calls the <see cref="Visual.InvalidateVisual"/> method on each one.
    /// </summary>
    public static class MediaInvalidation
    {
        private static readonly HashSet<AvaloniaProperty> s_registeredProperties = new();


        /// <summary>
        /// Marks one or more instances of <see cref="AvaloniaProperty"/> as affecting the appearance of the owner type.
        /// </summary>
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
                newCollection.AddParent(e.Sender);
            }

            InvalidateAncestors(e.Sender);

            if (e.OldValue is AvaloniaObject oldChild)
            {
                oldChild.GetMediaParents()?.Remove(e.Sender);
            }

            if (e.OldValue is IMediaCollection oldCollection)
            {
                oldCollection.RemoveParent(e.Sender);
            }
        }

        /// <summary>
        /// Finds all <see cref="Visual"/> media ancestors of the given object and calls <see cref="Visual.InvalidateVisual"/> on each one.
        /// </summary>
        /// <remarks>
        /// <para>You should only need to manually call this method if your object has mutable children which do not derive from <see cref="AvaloniaObject"/>.</para>
        /// <para>To automatically include an <see cref="AvaloniaObject"/> in the media ancestors tree:</para>
        /// <list type="bullet">
        /// <item>Pass the <see cref="AvaloniaProperty"/> that will contain the object/collection to <see cref="AffectsMediaRender"/>.</item>
        /// <item>Use <see cref="MediaCollection{T}"/> as the base type of any collections of child objects.</item>
        /// <item>Ensure that *all* assignments to direct properties are reported (e.g. by calling <see cref="AvaloniaObject.SetAndRaise"/>).</item>
        /// </list>
        /// </remarks>
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
