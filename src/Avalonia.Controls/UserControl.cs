// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides the base class for defining a new control that encapsulates related existing controls and provides its own logic.
    /// </summary>
    public class UserControl : ContentControl, IStyleable, INameScope
    {
        private readonly NameScope _nameScope = new NameScope();

        /// <inheritdoc/>
        event EventHandler<NameScopeEventArgs> INameScope.Registered
        {
            add { _nameScope.Registered += value; }
            remove { _nameScope.Registered -= value; }
        }

        /// <inheritdoc/>
        event EventHandler<NameScopeEventArgs> INameScope.Unregistered
        {
            add { _nameScope.Unregistered += value; }
            remove { _nameScope.Unregistered -= value; }
        }

        /// <inheritdoc/>
        Type IStyleable.StyleKey => typeof(ContentControl);

        /// <inheritdoc/>
        void INameScope.Register(string name, object element)
        {
            _nameScope.Register(name, element);
        }

        /// <inheritdoc/>
        object INameScope.Find(string name)
        {
            return _nameScope.Find(name);
        }

        /// <inheritdoc/>
        void INameScope.Unregister(string name)
        {
            _nameScope.Unregister(name);
        }
    }
}
