// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Avalonia.Styling;
using System;
using System.Collections.Specialized;


namespace Avalonia.Controls
{
    /// <summary>
    /// A general purpose control for data presentation as part of the set of common controls provided with Avalon.
    ///    Drop a control into a layout;
    ///    Enable application developers to display data efficiently;
    ///    Allow the presentation of data to be styled, including the layout and the item visuals;
    ///    No type-specific functionality.
    ///
    /// ListView is a control which has
    ///    A data collection;
    ///    A set of predefined operations to manipulate the data/view.
    /// Also, ListView is a control for the most convenient browsing of data.
    /// </summary>

    //[StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(ListViewItem))]
    public class ListView : ListBox, IStyleable
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        static ListView()
        {
            SelectionModeProperty.OverrideMetadata<ListView>(new StyledPropertyMetadata<SelectionMode>(SelectionMode.Multiple)); /// typeof(ListView), new FrameworkPropertyMetadata(SelectionMode.Extended));

            ///ControlsTraceLogger.AddControl(TelemetryControls.ListView);
            ViewProperty.Changed.AddClassHandler<Grid>(OnViewChanged);
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// View DependencyProperty
        /// </summary>
        public static readonly AvaloniaProperty ViewProperty = AvaloniaProperty.Register<ListView, ViewBase>(nameof(View));

        /// <summary>
        /// descriptor of the whole view. Include chrome/layout/item/...
        /// </summary>
        public ViewBase View
        {
            get { return (ViewBase)GetValue(ViewProperty); }
            set { SetValue(ViewProperty, value); }
        }

        private static void OnViewChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            ListView listView = (ListView)d;

            ViewBase oldView = (ViewBase)e.OldValue;
            ViewBase newView = (ViewBase)e.NewValue;
            if (newView != null)
            {
                if (newView.IsUsed)
                {
                    throw new InvalidOperationException("View cannot be shared between multiple instances of ListView");
                }
                newView.IsUsed = true;
            }

            // In ApplyNewView ListView.ClearContainerForItemOverride will be called for each item.
            // Should use old view to do clear item.
            listView._previousView = oldView;
            listView.ApplyNewView();
            // After ApplyNewView, if item is removed, ListView.ClearContainerForItemOverride will be called.
            // Then should use new view to do clear item.
            listView._previousView = newView;

            if (oldView != null)
            {
                oldView.IsUsed = false;
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods
        /*
        /// <summary>
        /// Prepare the element to display the item. Override the default style
        /// if new view is a GridView and no ItemContainerStyle provided.
        /// Will call View.PrepareItem() to allow view do preparison for item.
        /// </summary>
        protected override void PrepareContainerForItemOverride(AvaloniaObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            ListViewItem lvi = element as ListViewItem;
            if (lvi != null)
            {
                ViewBase view = View;
                if (view != null)
                {
                    // update default style key
                    lvi.SetDefaultStyleKey(view.ItemContainerDefaultStyleKey);
                    view.PrepareItem(lvi);
                }
                else
                {
                    lvi.ClearDefaultStyleKey();
                }
            }
        }
        
        /// <summary>
        /// Clear the element to display the item.
        /// </summary>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            // This method no longer does the work it used to (bug 1445288).
            // It is called when a container is removed from the tree;  such a
            // container will be GC'd soon, so there's no point in changing
            // its properties.

            base.ClearContainerForItemOverride(element, item);
        }

        /// <summary> Return true if the item is (or is eligible to be) its own ItemContainer </summary>
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return (item is ListViewItem);
        }

        /// <summary> Create or identify the element used to display the given item. </summary>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ListViewItem();
        }
        */
        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        /*protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            ListViewAutomationPeer lvPeer = UIElementAutomationPeer.FromElement(this) as ListViewAutomationPeer;
            if (lvPeer != null && lvPeer.ViewAutomationPeer != null)
            {
                lvPeer.ViewAutomationPeer.ItemsChanged(e);
            }
        }*/

        #endregion // Protected Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods
        Type _defaultstyle;
        Type IStyleable.StyleKey
        {
            get
            {
                return typeof(ListBox);
                if (_defaultstyle == null)
                    return typeof(ListBox);
                return _defaultstyle;
            }
        }
        // apply styles described in View.
        private void ApplyNewView()
        {
            ViewBase newView = View;

            if (newView != null)
            {
                // update default style key of ListView
                _defaultstyle = newView.DefaultStyleKey;
            }
            else
            {
                _defaultstyle = null;
            }

            // Encounter a new view after loaded means user is switching view.
            // Force to regenerate all containers.
            ///if (IsLoaded)
            ///{
            ///    ItemContainerGenerator.Refresh();
            ///}
        }

        // Invalidate resources on the view header if the header isn't
        // reachable via the visual/logical tree
        /*internal override void OnThemeChanged()
        {
            // If the ListView does not have a template generated tree then its
            // View.Header is not reachable via a tree walk.
            if (!HasTemplateGeneratedSubTree && View != null)
            {
                View.OnThemeChanged();
            }
        }*/

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        private ViewBase _previousView;
    }
}
