﻿using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class DragEventArgs : RoutedEventArgs
    {
        public DragDropEffects DragEffects { get; set; }

        public IDataObject Data { get; private set; }

        public DragEventArgs(RoutedEvent<DragEventArgs> routedEvent, IDataObject data)
            : base(routedEvent)
        {
            this.Data = data;
        }

    }
}