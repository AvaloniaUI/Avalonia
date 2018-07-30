// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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

        /// <inheritdoc />
        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Count => _windows.Count;

        /// <inheritdoc />
        /// <summary>
        /// Gets the <see cref="T:Avalonia.Controls.Window" /> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="T:Avalonia.Controls.Window" />.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public Window this[int index] => _windows[index];

        /// <inheritdoc />
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

        /// <inheritdoc />
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
        /// Adds the specified window.
        /// </summary>
        /// <param name="window">The window.</param>
        internal void Add(Window window)
        {
            if (window == null)
            {
                return;
            }

            _windows.Add(window);
        }

        /// <summary>
        /// Removes the specified window.
        /// </summary>
        /// <param name="window">The window.</param>
        internal void Remove(Window window)
        {
            if (window == null)
            {
                return;
            }

            _windows.Remove(window);

            OnRemoveWindow(window);
        }

        /// <summary>
        /// Closes all windows and removes them from the underlying collection.
        /// </summary>
        internal void Clear()
        {
            while (_windows.Count > 0)
            {
                _windows[0].Close(true);
            }
        }

        private void OnRemoveWindow(Window window)
        {
            if (window == null)
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
