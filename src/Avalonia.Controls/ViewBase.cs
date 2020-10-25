// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// ViewBase is something that tells the ListView the way to present each 
    /// entity in the data collection, i.e. the default style key.
    /// </summary>

    public abstract class ViewBase : AvaloniaObject
    {
        #region Protected Methods

        /// <summary>
        /// called when ListView is prepare container for item
        /// </summary>
        /// <param name="item">the container</param>
        protected internal virtual void PrepareItem(ListViewItem item)
        {
        }

        /// <summary>
        /// called when ListView is clear container for item
        /// </summary>
        /// <param name="item">the container</param>
        protected internal virtual void ClearItem(ListViewItem item)
        {
        }

        /// <summary>
        /// default style key. 
        /// ListView will degrate to ListBox if sub-class doesn't override 
        /// this value.
        /// </summary>
        protected internal virtual Type DefaultStyleKey
        {
            get { return typeof(ListBox); }
        }

        /// <summary>
        /// default container style key
        /// The container, ListViewItem, will degrate to ListBoxItem if 
        /// sub-class doesn't override this value.
        /// </summary>
        protected internal virtual Type ItemContainerDefaultStyleKey
        {
            get { return typeof(ListBoxItem); }
        }

        // Propagate theme changes to contained headers
        internal virtual void OnThemeChanged()
        {
        }

        #endregion

        #region InheritanceContext
        /*
                /// <summary>
                ///     InheritanceContext
                /// </summary>
                internal override AvaloniaObject InheritanceParent
                {
                    get { return _inheritanceContext; }
                }

                // Receive a new inheritance context
                internal override void AddInheritanceContext(AvaloniaObject context, AvaloniaProperty property)
                {
                    if (_inheritanceContext != context)
                    {
                        // Pick up the new context
                        _inheritanceContext = context;
                        OnInheritanceContextChanged(EventArgs.Empty);
                    }
                }

                // Remove an inheritance context
                internal override void RemoveInheritanceContext(DependencyObject context, AvaloniaProperty property)
                {
                    if (_inheritanceContext == context)
                    {
                        // clear the context
                        _inheritanceContext = null;
                        OnInheritanceContextChanged(EventArgs.Empty);
                    }
                }

                // Fields to implement DO's inheritance context
                DependencyObject _inheritanceContext;
        */
        #endregion InheritanceContext


        // True, when view is assigned to a ListView.
        internal bool IsUsed
        {
            get { return _isUsed; }
            set { _isUsed = value; }
        }

        private bool _isUsed;
    }
}
