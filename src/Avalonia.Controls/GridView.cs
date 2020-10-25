// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.ComponentModel;            // DesignerSerializationVisibility
using System.Diagnostics;               // Debug
using System;
using Avalonia.Controls.Templates;
//using Avalonia.Markup.Xaml.Templates;

namespace Avalonia.Controls
{
    /// <summary>
    /// GridView is a built-in view of the ListView control.  It is unique
    /// from other built-in views because of its table-like layout.  Data in
    /// details view is shown in a table with each row corresponding to an
    /// entity in the data collection and each column being generated from a
    /// data-bound template, populated with values from the bound data entity.
    /// </summary>

    public class GridView : ViewBase
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///  Add an object child to this control
        /// </summary>
        protected virtual void AddChild(object column)
        {
            GridViewColumn c = column as GridViewColumn;

            if (c != null)
            {
                Columns.Add(c);
            }
            else
            {
                throw new InvalidOperationException("column is null");
            }
        }

        /// <summary>
        ///  Add a text string to this control
        /// </summary>
        protected virtual void AddText(string text)
        {
            AddChild(text);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        // For all the DPs on GridView, null is treated as unset,
        // because it's impossible to distinguish null and unset.
        // Change a property between null and unset, PropertyChangedCallback will not be called.

        // ----------------------------------------------------------------------------
        //  Defines the names of the resources to be consumed by the GridView style.
        //  Used to restyle several roles of GridView without having to restyle
        //  all of the control.
        // ----------------------------------------------------------------------------

        #region GridViewColumnCollection Attached DP

        /// <summary>
        /// Reads the attached property GridViewColumnCollection from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the GridViewColumnCollection attached property.</param>
        /// <returns>The property's value.</returns>
        public static GridViewColumnCollection GetColumnCollection(AvaloniaObject element)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            return (GridViewColumnCollection)element.GetValue(ColumnCollectionProperty);
        }

        /// <summary>
        /// Writes the attached property GridViewColumnCollection to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the GridViewColumnCollection attached property.</param>
        /// <param name="collection">The collection to set</param>
        public static void SetColumnCollection(AvaloniaObject element, GridViewColumnCollection collection)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            element.SetValue(ColumnCollectionProperty, collection);
        }

        /// <summary>
        /// This is the dependency property registered for the GridView' ColumnCollection attached property.
        /// </summary>
        public static readonly AttachedProperty<GridViewColumnCollection> ColumnCollectionProperty
            = AvaloniaProperty.RegisterAttached<GridView, Control, GridViewColumnCollection>("ColumnCollection");

        #endregion

        /// <summary> GridViewColumn List</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public GridViewColumnCollection Columns
        {
            get
            {
                if (_columns == null)
                {
                    _columns = new GridViewColumnCollection();

                    // Give the collection a back-link, this is used for the inheritance context
                    _columns.Owner = this;
                    _columns.InViewMode = true;
                }

                return _columns;
            }
        }

        #region ColumnHeaderTemplate

        /// <summary>
        /// ColumnHeaderTemplate DependencyProperty
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> ColumnHeaderTemplateProperty =
            AvaloniaProperty.Register<GridView, IDataTemplate>("ColumnHeaderTemplate");
               /*typeof(DataTemplate),
                typeof(GridView),
                new FrameworkPropertyMetadata(
                    new PropertyChangedCallback(OnColumnHeaderTemplateChanged))
            );*/


        /// <summary>
        /// column header template
        /// </summary>
        public IDataTemplate ColumnHeaderTemplate
        {
            get { return GetValue(ColumnHeaderTemplateProperty); }
            set { SetValue(ColumnHeaderTemplateProperty, value); }
        }

        private static void OnColumnHeaderTemplateChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            GridView dv = (GridView)d;

            // Check to prevent Template and TemplateSelector at the same time
        }

        #endregion  ColumnHeaderTemplate


        #region ColumnHeaderStringFormat

        /// <summary>
        /// ColumnHeaderStringFormat DependencyProperty
        /// </summary>
        public static readonly StyledProperty<string> ColumnHeaderStringFormatProperty =
            AvaloniaProperty.Register<GridView, string>(nameof(ColumnHeaderStringFormat));


        /// <summary>
        /// column header string format
        /// </summary>
        public String ColumnHeaderStringFormat
        {
            get { return (String)GetValue(ColumnHeaderStringFormatProperty); }
            set { SetValue(ColumnHeaderStringFormatProperty, value); }
        }

        #endregion  ColumnHeaderStringFormat

        #region AllowsColumnReorder

        /// <summary>
        /// AllowsColumnReorderProperty DependencyProperty
        /// </summary>
        public static readonly StyledProperty<bool> AllowsColumnReorderProperty = AvaloniaProperty.Register<GridView, bool>(nameof(AllowsColumnReorder), true);

        /// <summary>
        /// AllowsColumnReorder
        /// </summary>
        public bool AllowsColumnReorder
        {
            get { return (bool)GetValue(AllowsColumnReorderProperty); }
            set { SetValue(AllowsColumnReorderProperty, value); }
        }

        #endregion AllowsColumnReorder

        #region ColumnHeaderContextMenu

        /// <summary>
        /// ColumnHeaderContextMenuProperty DependencyProperty
        /// </summary>
        public static readonly StyledProperty<ContextMenu> ColumnHeaderContextMenuProperty = AvaloniaProperty.Register<GridView, ContextMenu>(nameof(ColumnHeaderContextMenu));

        /// <summary>
        /// ColumnHeaderContextMenu
        /// </summary>
        public ContextMenu ColumnHeaderContextMenu
        {
            get { return GetValue(ColumnHeaderContextMenuProperty); }
            set { SetValue(ColumnHeaderContextMenuProperty, value); }
        }

        #endregion ColumnHeaderContextMenu

        #region ColumnHeaderToolTip

        /// <summary>
        /// ColumnHeaderToolTipProperty DependencyProperty
        /// </summary>
        public static readonly StyledProperty<ToolTip> ColumnHeaderToolTipProperty =
                AvaloniaProperty.Register<GridView, ToolTip>(nameof(ColumnHeaderToolTip));

        /// <summary>
        /// ColumnHeaderToolTip
        /// </summary>
        public ToolTip ColumnHeaderToolTip
        {
            get { return GetValue(ColumnHeaderToolTipProperty); }
            set { SetValue(ColumnHeaderToolTipProperty, value); }
        }

        #endregion ColumnHeaderToolTip

        #endregion // Public Properties

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// called when ListView is prepare container for item.
        /// GridView override this method to attache the column collection
        /// </summary>
        /// <param name="item">the container</param>
        protected internal override void PrepareItem(ListViewItem item)
        {
            base.PrepareItem(item);

            // attach GridViewColumnCollection to ListViewItem.
            SetColumnCollection(item, _columns);
        }

        /// <summary>
        /// called when ListView is clear container for item.
        /// GridView override this method to clear the column collection
        /// </summary>
        /// <param name="item">the container</param>
        protected internal override void ClearItem(ListViewItem item)
        {
            item.ClearValue(ColumnCollectionProperty);

            base.ClearItem(item);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /*internal override void OnInheritanceContextChangedCore(EventArgs args)
        {
            base.OnInheritanceContextChangedCore(args);

            if (_columns != null)
            {
                foreach (GridViewColumn column in _columns)
                {
                    column.OnInheritanceContextChanged(args);
                }
            }
        }

        // Propagate theme changes to contained headers
        internal override void OnThemeChanged()
        {
            if (_columns != null)
            {
                for (int i = 0; i < _columns.Count; i++)
                {
                    _columns[i].OnThemeChanged();
                }
            }
        }*/

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private GridViewColumnCollection _columns;

        #endregion // Private Fields

        internal GridViewHeaderRowPresenter HeaderRowPresenter
        {
            get { return _gvheaderRP; }
            set { _gvheaderRP = value; }
        }

        private GridViewHeaderRowPresenter _gvheaderRP;
    }
}
