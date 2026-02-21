using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="PipsPager.SelectedIndexChanged"/> event.
    /// </summary>
    public class PipsPagerSelectedIndexChangedEventArgs : EventArgs
    {
        internal PipsPagerSelectedIndexChangedEventArgs(int oldIndex, int newIndex)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }

        /// <summary>
        /// Gets the previous selected index.
        /// </summary>
        public int OldIndex { get; }

        /// <summary>
        /// Gets the new selected index.
        /// </summary>
        public int NewIndex { get; }
    }
}
