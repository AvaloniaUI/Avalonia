using System;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public static class DragDrop
    {
        /// <summary>
        /// Event which is raised, when a drag-and-drop operation enters the element.
        /// </summary>
        public static readonly RoutedEvent<DragEventArgs> DragEnterEvent = RoutedEvent.Register<DragEventArgs>("DragEnter", RoutingStrategies.Bubble, typeof(DragDrop));
        /// <summary>
        /// Event which is raised, when a drag-and-drop operation leaves the element.
        /// </summary>
        public static readonly RoutedEvent<DragEventArgs> DragLeaveEvent = RoutedEvent.Register<DragEventArgs>("DragLeave", RoutingStrategies.Bubble, typeof(DragDrop));
        /// <summary>
        /// Event which is raised, when a drag-and-drop operation is updated while over the element.
        /// </summary>
        public static readonly RoutedEvent<DragEventArgs> DragOverEvent = RoutedEvent.Register<DragEventArgs>("DragOver", RoutingStrategies.Bubble, typeof(DragDrop));
        /// <summary>
        /// Event which is raised, when a drag-and-drop operation should complete over the element.
        /// </summary>
        public static readonly RoutedEvent<DragEventArgs> DropEvent = RoutedEvent.Register<DragEventArgs>("Drop", RoutingStrategies.Bubble, typeof(DragDrop));

        public static readonly AttachedProperty<bool> AllowDropProperty = AvaloniaProperty.RegisterAttached<Interactive, bool>("AllowDrop", typeof(DragDrop), inherits: true);

        /// <summary>
        /// Gets a value indicating whether the given element can be used as the target of a drag-and-drop operation. 
        /// </summary>
        public static bool GetAllowDrop(Interactive interactive)
        {
            return interactive.GetValue(AllowDropProperty);
        }

        /// <summary>
        /// Sets a value indicating whether the given interactive can be used as the target of a drag-and-drop operation. 
        /// </summary>
        public static void SetAllowDrop(Interactive interactive, bool value)
        {
            interactive.SetValue(AllowDropProperty, value);
        }

        /// <summary>
        /// Adds a handler for the DragEnter attached event.
        /// </summary>
        /// <param name="element">The element to attach the handler to.</param>
        /// <param name="handler">The handler for the event.</param>
        public static void AddDragEnterHandler(Interactive element, EventHandler<DragEventArgs> handler)
        {
            element.AddHandler(DragEnterEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the DragEnter attached event.
        /// </summary>
        /// <param name="element">The element to remove the handler from.</param>
        /// <param name="handler">The handler to remove.</param>
        public static void RemoveDragEnterHandler(Interactive element, EventHandler<DragEventArgs> handler)
        {
            element.RemoveHandler(DragEnterEvent, handler);
        }

        /// <summary>
        /// Adds a handler for the DragLeave attached event.
        /// </summary>
        /// <param name="element">The element to attach the handler to.</param>
        /// <param name="handler">The handler for the event.</param>
        public static void AddDragLeaveHandler(Interactive element, EventHandler<DragEventArgs> handler)
        {
            element.AddHandler(DragLeaveEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the DragLeave attached event.
        /// </summary>
        /// <param name="element">The element to remove the handler from.</param>
        /// <param name="handler">The handler to remove.</param>
        public static void RemoveDragLeaveHandler(Interactive element, EventHandler<DragEventArgs> handler)
        {
            element.RemoveHandler(DragLeaveEvent, handler);
        }

        /// <summary>
        /// Adds a handler for the DragOver attached event.
        /// </summary>
        /// <param name="element">The element to attach the handler to.</param>
        /// <param name="handler">The handler for the event.</param>
        public static void AddDragOverHandler(Interactive element, EventHandler<DragEventArgs> handler)
        {
            element.AddHandler(DragOverEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the DragOver attached event.
        /// </summary>
        /// <param name="element">The element to remove the handler from.</param>
        /// <param name="handler">The handler to remove.</param>
        public static void RemoveDragOverHandler(Interactive element, EventHandler<DragEventArgs> handler)
        {
            element.RemoveHandler(DragOverEvent, handler);
        }

        /// <summary>
        /// Adds a handler for the Drop attached event.
        /// </summary>
        /// <param name="element">The element to attach the handler to.</param>
        /// <param name="handler">The handler for the event.</param>
        public static void AddDropHandler(Interactive element, EventHandler<DragEventArgs> handler)
        {
            element.AddHandler(DropEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the Drop attached event.
        /// </summary>
        /// <param name="element">The element to remove the handler from.</param>
        /// <param name="handler">The handler to remove.</param>
        public static void RemoveDropHandler(Interactive element, EventHandler<DragEventArgs> handler)
        {
            element.RemoveHandler(DropEvent, handler);
        }

        /// <summary>
        /// Starts a dragging operation with the given <see cref="IDataObject"/> and returns the applied drop effect from the target.
        /// <seealso cref="DataObject"/>
        /// </summary>
        [Obsolete($"Use {nameof(DoDragDropAsync)} instead.")]
        public static Task<DragDropEffects> DoDragDrop(PointerEventArgs triggerEvent, IDataObject data, DragDropEffects allowedEffects)
        {
            return DoDragDropAsync(triggerEvent, new DataObjectToDataTransferWrapper(data), allowedEffects);
        }

        /// <summary>
        /// Starts a dragging operation with the given <see cref="IDataTransfer"/> and returns the applied drop effect from the target.
        /// <seealso cref="DataTransfer"/>
        /// </summary>
        public static Task<DragDropEffects> DoDragDropAsync(
            PointerEventArgs triggerEvent,
            IDataTransfer dataTransfer,
            DragDropEffects allowedEffects)
        {
            if (AvaloniaLocator.Current.GetService<IPlatformDragSource>() is not { } dragSource)
            {
                dataTransfer.Dispose();
                return Task.FromResult(DragDropEffects.None);
            }

            return dragSource.DoDragDropAsync(triggerEvent, dataTransfer, allowedEffects);
        }
    }
}
