// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="SelectionModel.ChildrenRequested"/> event.
    /// </summary>
    public class SelectionModelChildrenRequestedEventArgs : EventArgs
    {
        private object? _source;
        private IndexPath _sourceIndexPath;
        private IndexPath _finalIndexPath;
        private bool _throwOnAccess;
        
        internal SelectionModelChildrenRequestedEventArgs(
            object source,
            IndexPath sourceIndexPath,
            IndexPath finalIndexPath,
            bool throwOnAccess)
        {
            source = source ?? throw new ArgumentNullException(nameof(source));
            Initialize(source, sourceIndexPath, finalIndexPath, throwOnAccess);
        }

        /// <summary>
        /// Gets or sets an observable which produces the children of the <see cref="Source"/>
        /// object.
        /// </summary>
        public IObservable<object?>? Children { get; set; }

        /// <summary>
        /// Gets the object whose children are being requested.
        /// </summary>        
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

        /// <summary>
        /// Gets the index of the object whose children are being requested.
        /// </summary>        
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

        /// <summary>
        /// Gets the index of the final object which is being attempted to be retrieved.
        /// </summary>
        public IndexPath FinalIndex
        {
            get
            {
                if (_throwOnAccess)
                {
                    throw new ObjectDisposedException(nameof(SelectionModelChildrenRequestedEventArgs));
                }

                return _finalIndexPath;
            }
        }

        internal void Initialize(
            object? source,
            IndexPath sourceIndexPath,
            IndexPath finalIndexPath,
            bool throwOnAccess)
        {
            if (!throwOnAccess && source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _source = source;
            _sourceIndexPath = sourceIndexPath;
            _finalIndexPath = finalIndexPath;
            _throwOnAccess = throwOnAccess;
        }
    }
}
