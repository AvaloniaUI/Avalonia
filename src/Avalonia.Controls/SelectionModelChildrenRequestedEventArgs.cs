// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;

#nullable enable

namespace Avalonia.Controls
{
    public class SelectionModelChildrenRequestedEventArgs : EventArgs
    {
        private object? _source;
        private SelectionNode? _sourceNode;

        internal SelectionModelChildrenRequestedEventArgs(object source, SelectionNode sourceNode)
        {
            _source = source;
            _sourceNode = sourceNode;
        }

        public object? Children { get; set; }
        
        public object Source
        {
            get
            {
                if (_source == null)
                {
                    throw new ObjectDisposedException(nameof(SelectionModelChildrenRequestedEventArgs));
                }

                return _source;
            }
        }

        public IndexPath SourceIndex
        {
            get
            {
                if (_sourceNode == null)
                {
                    throw new ObjectDisposedException(nameof(SelectionModelChildrenRequestedEventArgs));
                }

                return _sourceNode.IndexPath;
            }
        }

        internal void Initialize(object? source, SelectionNode? sourceNode)
        {
            _source = source;
            _sourceNode = sourceNode;
        }
    }
}
