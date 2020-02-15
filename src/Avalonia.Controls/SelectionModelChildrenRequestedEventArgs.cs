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
        private IndexPath _sourceIndexPath;
        private bool _throwOnAccess;
        
        internal SelectionModelChildrenRequestedEventArgs(
            object source,
            IndexPath sourceIndexPath,
            bool throwOnAccess)
        {
            source = source ?? throw new ArgumentNullException(nameof(source));
            Initialize(source, sourceIndexPath, throwOnAccess);
        }

        public object? Children { get; set; }
        
        public object Source
        {
            get
            {
                if (_throwOnAccess)
                {
                    throw new ObjectDisposedException(nameof(SelectionModelChildrenRequestedEventArgs));
                }

                return _source!;
            }
        }

        public IndexPath SourceIndex
        {
            get
            {
                if (_throwOnAccess)
                {
                    throw new ObjectDisposedException(nameof(SelectionModelChildrenRequestedEventArgs));
                }

                return _sourceIndexPath;
            }
        }

        internal void Initialize(
            object? source,
            IndexPath sourceIndexPath,
            bool throwOnAccess)
        {
            if (!throwOnAccess && source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _source = source;
            _sourceIndexPath = sourceIndexPath;
            _throwOnAccess = throwOnAccess;
        }
    }
}
