﻿using System;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Styling;

namespace Avalonia
{
    public interface IStyledElement :
        IStyleable,
        IStyleHost,
        ILogical,
        IResourceProvider,
        IResourceNode,
        IDataContextProvider
    {
        /// <summary>
        /// Occurs when the control has finished initialization.
        /// </summary>
        event EventHandler Initialized;

        /// <summary>
        /// Gets a value that indicates whether the element has finished initialization.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Gets or sets the control's styling classes.
        /// </summary>
        new Classes Classes { get; set; }

        /// <summary>
        /// Gets the control's logical parent.
        /// </summary>
        IStyledElement Parent { get; }
    }
}
