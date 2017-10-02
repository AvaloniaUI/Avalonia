// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Threading
{
    /// <summary>
    /// Defines the priorities with which jobs can be invoked on a <see cref="Dispatcher"/>.
    /// </summary>
    // TODO: These are copied from WPF - many won't apply to Avalonia.
    public enum DispatcherPriority
    {
        /// <summary>
        /// Minimum possible priority
        /// </summary>
        MinValue = 1,
        
        /// <summary>
        /// The job will be processed when the system is idle.
        /// </summary>
        SystemIdle = 1,

        /// <summary>
        /// The job will be processed when the application sis idle.
        /// </summary>
        ApplicationIdle = 2,

        /// <summary>
        /// The job will be processed after background operations have completed.
        /// </summary>
        ContextIdle = 3,

        /// <summary>
        /// The job will be processed after other non-idle operations have completed.
        /// </summary>
        Background = 4,

        /// <summary>
        /// The job will be processed with the same priority as input.
        /// </summary>
        Input = 5,

        /// <summary>
        /// The job will be processed after layout and render but before input.
        /// </summary>
        Loaded = 6,

        /// <summary>
        /// The job will be processed with the same priority as render.
        /// </summary>
        Render = 7,
        
        /// <summary>
        /// The job will be processed with the same priority as render.
        /// </summary>
        Layout = 8,
        
        /// <summary>
        /// The job will be processed with the same priority as data binding.
        /// </summary>
        DataBind = 9,

        /// <summary>
        /// The job will be processed with normal priority.
        /// </summary>
        Normal = 10,

        /// <summary>
        /// The job will be processed before other asynchronous operations.
        /// </summary>
        Send = 11,
        
        /// <summary>
        /// Maximum possible priority
        /// </summary>
        MaxValue = 11
    }
}
