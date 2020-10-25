// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Ported from https://github.com/dotnet/wpf/blob/master/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/ListViewItem.cs

using Avalonia.Styling;
using System;

namespace Avalonia.Controls
{
    /// <summary>
    ///     Control that implements a selectable item inside a ListView.
    /// </summary>
    public class ListViewItem : ListBoxItem, IStyleable
    {
        Type _styleKey;
        Type IStyleable.StyleKey
        {
            get
            {
                if (_styleKey != null)
                    return _styleKey;
                return typeof(ListViewItem);
            }
        }
        // NOTE: ListViewItem has no default theme style. It uses ThemeStyleKey 
        // to find default style for different view.

        // helper to set DefaultStyleKey of ListViewItem
        internal void SetDefaultStyleKey(Type key)
        {
            this._styleKey = key;
        }

        //  helper to clear DefaultStyleKey of ListViewItem
        internal void ClearDefaultStyleKey()
        {
            _styleKey = null;
        }
    }
}
