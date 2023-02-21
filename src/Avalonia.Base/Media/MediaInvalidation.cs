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

        private static readonly AnonymousObserver<AvaloniaPropertyChangedEventArgs> s_referenceTypePropertyChangeObserver = new(OnMediaRenderPropertyChanged);
        private static readonly AnonymousObserver<AvaloniaPropertyChangedEventArgs> s_valueTypePropertyChangeObserver = new(OnMediaRenderPropertyChanged_Struct);

        /// <summary>
        /// Marks one or more instances of <see cref="AvaloniaProperty"/> as affecting the appearance of the owner type.
        /// </summary>
        public static void AffectsMediaRender(params AvaloniaProperty[] properties)
        {
            foreach (var property in properties)
            {
                if (s_registeredProperties.Add(property))
                {
                    property.Changed.Subscribe(property.PropertyType.IsValueType ? s_valueTypePropertyChangeObserver : s_referenceTypePropertyChangeObserver);
                }
            }
        }

        internal static void AddMediaChild(AvaloniaObject parent, AvaloniaObject child)
        {
            child.GetOrCreateMediaParents().Add(parent);
            Invalidate(parent);
        }

        internal static void RemoveMediaChild(AvaloniaObject parent, AvaloniaObject child)
        {
            Invalidate(parent);
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

                foreach (var child in newCollection.Items)
                {
                    child.GetOrCreateMediaParents().Add(e.Sender);
                }
            }

            Invalidate(e.Sender);

            if (e.OldValue is AvaloniaObject oldChild)
            {
                oldChild.GetMediaParents()?.Remove(e.Sender);
            }

            if (e.OldValue is IMediaCollection oldCollection)
            {
                foreach (var child in oldCollection.Items)
                {
                    child.GetMediaParents()?.Remove(e.Sender);
                }

                oldCollection.Parents.Remove(e.Sender);
            }
        }

        private static void OnMediaRenderPropertyChanged_Struct(AvaloniaPropertyChangedEventArgs e)
        {
            Invalidate(e.Sender);
        }

        private static void Invalidate(AvaloniaObject obj)
        {
            if (obj is Visual visual)
            {
                visual.InvalidateVisual();
            }
            else
            {
                InvalidateAncestors(obj);
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
            var ancestors = GetMediaAncestors<Visual>(mediaObject);
            if (ancestors != null)
            {
                foreach (var ancestor in ancestors)
                {
                    ancestor.InvalidateVisual();
                }
            }
        }

        private static List<T>? GetMediaAncestors<T>(AvaloniaObject root) where T : AvaloniaObject
        {
            var parents = root.GetMediaParents();
            if (parents == null)
            {
                return null;
            }

            var visited = new HashSet<AvaloniaObject>(parents);
            var stack = new Stack<AvaloniaObject>(visited); // start with only unique values
            List<T>? result = null;

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current is T target)
                {
                    result ??= new(1); // most objects will only have one parent
                    result.Add(target);
                }
                else
                {
                    parents = current.GetMediaParents();
                    if (parents != null)
                    {
                        foreach (var p in parents)
                        {
                            if (visited.Add(p))
                            {
                                stack.Push(p);
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
