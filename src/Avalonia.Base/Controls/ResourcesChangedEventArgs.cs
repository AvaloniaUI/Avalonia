using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents the event args for the ResourcesChanged event. 
    /// </summary>
    public class ResourcesChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Get the empty event args with <see cref="Key"/> as null.
        /// </summary>
        public new static readonly ResourcesChangedEventArgs Empty = new();
        
        /// <summary>
        /// Get the key of changed resource. Key is null if event is raised not for a particular resource change, such as attaching or owner changing.
        /// </summary>
        public object? Key { get; }

        /// <summary>
        /// Create an empty <see cref="ResourcesChangedEventArgs"/>.
        /// </summary>
        public ResourcesChangedEventArgs() { }
        
        /// <summary>
        /// Create an instance of <see cref="ResourcesChangedEventArgs"/> with the specified key.
        /// </summary>
        /// <param name="key">The key of changed resource. Key can be null if event is not raised for a particular resource. </param>
        public ResourcesChangedEventArgs(object? key)
        {
            Key = key;
        }

        
    }
}
