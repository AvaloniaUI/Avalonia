// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls
{
    public class SelectionModelChildrenRequestedEventArgs : EventArgs
    {
        private SelectionNode _sourceNode;

        internal SelectionModelChildrenRequestedEventArgs(object source, SelectionNode sourceNode)
        {
            Initialize(source, sourceNode);
        }

        public object Children { get; set; }
        public object Source { get; private set; }
        public IndexPath SourceIndex => _sourceNode.IndexPath;

        internal void Initialize(object source, SelectionNode sourceNode)
        {
            Source = source;
            _sourceNode = sourceNode;
        }
    }
}
