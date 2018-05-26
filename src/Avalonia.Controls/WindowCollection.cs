// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

using Avalonia.Controls;

namespace Avalonia
{
    public class WindowCollection : IReadOnlyList<Window>
    {
        private readonly Application _application;
        private readonly List<Window> _windows = new List<Window>();

        public WindowCollection(Application application)
        {
            _application = application;
        }

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Count => _windows.Count;

        /// <summary>
        /// Gets the <see cref="Window"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="Window"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public Window this[int index] => _windows[index];

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<Window> GetEnumerator()
        {
            return _windows.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Determines whether [contains] [the specified window].
        /// </summary>
        /// <param name="window">The window.</param>
        /// <returns>
        ///   <c>true</c> if [contains] [the specified window]; otherwise, <c>false</c>.
        /// </returns>
        internal bool Contains(Window window)
        {
            return _windows.Contains(window);
        }

        /// <summary>
        /// Adds the specified window.
        /// </summary>
        /// <param name="window">The window.</param>
        internal void Add(Window window)
        {
            _windows.Add(window);

            window.Closed += OnWindowClosed;
        }

        internal void Remove(Window window)
        {
            _windows.Remove(window);
        }

        /// <summary>
        /// Removes the window at a specific location.
        /// </summary>
        /// <param name="index">The index.</param>
        internal void RemoveAt(int index)
        {
            _windows.RemoveAt(index);
        }

        private void OnWindowClosed(object sender, EventArgs eventArgs)
        {
            if (!(sender is Window window))
            {
                return;
            }

            if (_application.IsExiting)
            {
                return;
            }

            switch (_application.ExitMode)
            {
                case ExitMode.OnLastWindowClose:
                    if (Count == 0)
                    {
                        _application.Exit();
                    }

                    break;
                case ExitMode.OnMainWindowClose:
                    if (window == _application.MainWindow)
                    {
                        _application.Exit();
                    }

                    break;
                    
            }
        }
    }
}