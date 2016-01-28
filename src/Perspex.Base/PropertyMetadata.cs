// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Data;

namespace Perspex
{
    /// <summary>
    /// Base class for perspex property metadata.
    /// </summary>
    public class PropertyMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyMetadata"/> class.
        /// </summary>
        /// <param name="defaultBindingMode">The default binding mode.</param>
        /// <param name="notifyingCallback">The property notifying callback.</param>
        public PropertyMetadata(
            BindingMode defaultBindingMode = BindingMode.Default,
            Action<PerspexObject, bool> notifyingCallback = null)
        {
            DefaultBindingMode = defaultBindingMode;
            NotifyingCallback = notifyingCallback;
        }

        /// <summary>
        /// Gets the default binding mode for the property.
        /// </summary>
        public BindingMode DefaultBindingMode { get; private set; }

        /// <summary>
        /// Gets a method that gets called before and after the property starts being notified on an
        /// object.
        /// </summary>
        /// <remarks>
        /// When a property changes, change notifications are sent to all property subscribers; 
        /// for example via the <see cref="PerspexProperty.Changed"/> observable and and the 
        /// <see cref="PerspexObject.PropertyChanged"/> event. If this callback is set for a property,
        /// then it will be called before and after these notifications take place. The bool argument
        /// will be true before the property change notifications are sent and false afterwards. This 
        /// callback is intended to support Control.IsDataContextChanging.
        /// </remarks>
        public Action<PerspexObject, bool> NotifyingCallback { get; private set; }

        /// <summary>
        /// Merges the metadata with the base metadata.
        /// </summary>
        /// <param name="baseMetadata">The base metadata to merge.</param>
        /// <param name="property">The property to which the metadata is being applied.</param>
        public virtual void Merge(
            PropertyMetadata baseMetadata, 
            PerspexProperty property)
        {
            if (DefaultBindingMode == BindingMode.Default)
            {
                DefaultBindingMode = baseMetadata.DefaultBindingMode;
            }

            if (NotifyingCallback == null)
            {
                NotifyingCallback = baseMetadata.NotifyingCallback;
            }
        }
    }
}
