// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.ComponentModel;        // DesignerSerializationVisibility
using System.Diagnostics;
using Avalonia.Data;          // BindingBase
//using System.Windows.Markup;        // [ContentProperty]
using System;
using Avalonia.Styling;
using Avalonia.Controls.Templates;
//using Avalonia.Markup.Xaml.Templates;

namespace Avalonia.Controls
{
    /// <summary>
    /// template of column of a details view.
    /// </summary>

    //[ContentProperty("Header")]
    //[StyleTypedProperty(Property = "HeaderContainerStyle", StyleTargetType = typeof(System.Windows.Controls.GridViewColumnHeader))]
    //[Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)] // cannot be read & localized as string
    public class GridViewColumn : AvaloniaObject, INotifyPropertyChanged
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// constructor
        /// </summary>
        public GridViewColumn()
        {
            ResetPrivateData();

            // Descendant of this class can override the metadata to give it
            // a value other than NaN and without trigger the propertychange
            // callback and thus, result in _state be out-of-sync with the
            // Width property.
            _state = Double.IsNaN(Width) ? ColumnMeasureState.Init : ColumnMeasureState.SpecificWidth;

            HeaderProperty.Changed.AddClassHandler<GridViewColumn>(OnHeaderChanged);
            HeaderContainerStyleProperty.Changed.AddClassHandler<GridViewColumn>(OnHeaderContainerStyleChanged);
            HeaderTemplateProperty.Changed.AddClassHandler<GridViewColumn>(OnHeaderTemplateChanged);
            HeaderStringFormatProperty.Changed.AddClassHandler<GridViewColumn>(OnHeaderStringFormatChanged);
            CellTemplateProperty.Changed.AddClassHandler<GridViewColumn>(OnCellTemplateChanged);
            WidthProperty.Changed.AddClassHandler<GridViewColumn>(OnWidthChanged);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            throw new NotImplementedException(); //SR.Get(SRID.ToStringFormatString_GridViewColumn, this.GetType(), Header);
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        // For all the DPs on GridViewColumn, null is treated as unset,
        // because it's impossible to distinguish null and unset.
        // Change a property between null and unset, PropertyChangedCallback will not be called.

        #region Header

        /// <summary>
        /// Header DependencyProperty
        /// </summary>
        public static readonly AvaloniaProperty HeaderProperty = AvaloniaProperty.Register<GridViewColumn, object>(nameof(Header));

        /// <summary>
        /// If provide a GridViewColumnHeader or an instance of its sub class , it will be used as header.
        /// Otherwise, it will be used as content of header
        /// </summary>
        /// <remarks>
        /// typical usage is to assign the content of the header or the container
        /// <code>
        ///         GridViewColumn column = new GridViewColumn();
        ///         column.Header = "Name";
        /// </code>
        /// or
        /// <code>
        ///         GridViewColumnHeader header = new GridViewColumnHeader();
        ///         header.Content = "Name";
        ///         header.Click += ...
        ///         ...
        ///         GridViewColumn column = new GridViewColumn();
        ///         column.Header = header;
        /// </code>
        /// </remarks>
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        private static void OnHeaderChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            GridViewColumn c = (GridViewColumn)d;
            c.OnPropertyChanged(HeaderProperty.Name);
        }

        #endregion Header

        #region HeaderContainerStyle

        /// <summary>
        /// HeaderContainerStyle DependencyProperty
        /// </summary>
        public static readonly AvaloniaProperty HeaderContainerStyleProperty = AvaloniaProperty.Register<GridViewColumn, Style>(nameof(HeaderContainerStyle));

        /// <summary>
        /// Header container's style
        /// </summary>
        public Style HeaderContainerStyle
        {
            get { return (Style)GetValue(HeaderContainerStyleProperty); }
            set { SetValue(HeaderContainerStyleProperty, value); }
        }

        private static void OnHeaderContainerStyleChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            GridViewColumn c = (GridViewColumn)d;
            c.OnPropertyChanged(HeaderContainerStyleProperty.Name);
        }

        #endregion HeaderContainerStyle

        #region HeaderTemplate

        /// <summary>
        /// HeaderTemplate DependencyProperty
        /// </summary>
        public static readonly AvaloniaProperty HeaderTemplateProperty = AvaloniaProperty.Register<GridViewColumn, IDataTemplate>(nameof(HeaderTemplate));

        /// <summary>
        /// column header template
        /// </summary>
        public IDataTemplate HeaderTemplate
        {
            get { return (IDataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        private static void OnHeaderTemplateChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            GridViewColumn c = (GridViewColumn)d;
            // Check to prevent Template and TemplateSelector at the same time
            // Avalonia doesn't have TemplateSelectors, so this should be fine
            //Helper.CheckTemplateAndTemplateSelector("Header", HeaderTemplateProperty, HeaderTemplateSelectorProperty, c);
            c.OnPropertyChanged(HeaderTemplateProperty.Name);
        }

        #endregion  HeaderTemplate

        #region HeaderStringFormat

        /// <summary>
        ///     The DependencyProperty for the HeaderStringFormat property.
        ///     Flags:              None
        ///     Default Value:      null
        /// </summary>
        public static readonly AvaloniaProperty HeaderStringFormatProperty = AvaloniaProperty.Register<GridViewColumn, string>(nameof(HeaderStringFormat), null);


        /// <summary>
        ///     HeaderStringFormat is the format used to display the header content as a string.
        ///     This arises only when no template is available.
        /// </summary>
        public String HeaderStringFormat
        {
            get { return (String)GetValue(HeaderStringFormatProperty); }
            set { SetValue(HeaderStringFormatProperty, value); }
        }

        /// <summary>
        ///     Called when HeaderStringFormatProperty is invalidated on "d."
        /// </summary>
        private static void OnHeaderStringFormatChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            GridViewColumn ctrl = (GridViewColumn)d;
            ctrl.OnHeaderStringFormatChanged((String)e.OldValue, (String)e.NewValue);
        }

        /// <summary>
        ///     This method is invoked when the HeaderStringFormat property changes.
        /// </summary>
        /// <param name="oldHeaderStringFormat">The old value of the HeaderStringFormat property.</param>
        /// <param name="newHeaderStringFormat">The new value of the HeaderStringFormat property.</param>
        protected virtual void OnHeaderStringFormatChanged(String oldHeaderStringFormat, String newHeaderStringFormat)
        {
        }

        #endregion HeaderStringFormat

        #region DisplayMemberBinding

        /*/// <summary>
        /// BindingBase is be used to generate each cell of this column.
        /// Set to null make this property do not work.
        /// </summary>
        public BindingBase DisplayMemberBinding
        {
            get { return _displayMemberBinding; }
            set
            {
                if (_displayMemberBinding != value)
                {
                    _displayMemberBinding = value;
                    OnDisplayMemberBindingChanged();
                }
            }
        }

        private BindingBase _displayMemberBinding;

        /// <summary>
        /// If DisplayMemberBinding property changed, NotifyPropertyChanged event will be raised with this string.
        /// </summary>
        internal const string c_DisplayMemberBindingName = "DisplayMemberBinding";

        private void OnDisplayMemberBindingChanged()
        {
            OnPropertyChanged(c_DisplayMemberBindingName);
        }*/

        #endregion

        #region CellTemplate

        /// <summary>
        /// CellTemplate DependencyProperty
        /// </summary>
        public static readonly AvaloniaProperty CellTemplateProperty = AvaloniaProperty.Register<GridViewColumn, IDataTemplate>(nameof(CellTemplate));

        /// <summary>
        /// template for this column's item UI
        /// </summary>
        public IDataTemplate CellTemplate
        {
            get { return (IDataTemplate)GetValue(CellTemplateProperty); }
            set { SetValue(CellTemplateProperty, value); }
        }

        private static void OnCellTemplateChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            GridViewColumn c = (GridViewColumn)d;
            c.OnPropertyChanged(CellTemplateProperty.Name);
        }

        #endregion

        #region Width

        /// <summary>
        /// Width DependencyProperty
        /// </summary>
        public static readonly AvaloniaProperty WidthProperty = AvaloniaProperty.Register<GridViewColumn, double>(nameof(WidthProperty), Double.NaN);
            /*AvaloniaElement.WidthProperty.AddOwner(
                typeof(GridViewColumn),
                new PropertyMetadata(
                    Double.NaN, // default value
                    new PropertyChangedCallback(OnWidthChanged))
            );*/

        /// <summary>
        /// width of the column
        /// </summary>
        /// <remarks>
        /// The default value is Double.NaN which means size to max visible item width.
        /// </remarks>
        ///[TypeConverter(typeof(LengthConverter))]
        public double Width
        {
            get { return (double)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        private static void OnWidthChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            GridViewColumn c = (GridViewColumn)d;

            double newWidth = (double)e.NewValue;

            // reset DesiredWidth if width is set to auto
            c.State = Double.IsNaN(newWidth) ? ColumnMeasureState.Init : ColumnMeasureState.SpecificWidth;

            c.OnPropertyChanged(WidthProperty.Name);
        }

        #endregion

        #region ActualWidth

        /// <summary>
        /// actual width of this column
        /// </summary>
        public double ActualWidth
        {
            get { return _actualWidth; }

            private set
            {
                if (Double.IsNaN(value) || Double.IsInfinity(value) || value < 0.0)
                {
                    Debug.Assert(false, "Invalid value for ActualWidth.");
                }
                else if (_actualWidth != value)
                {
                    _actualWidth = value;
                    OnPropertyChanged(c_ActualWidthName);
                }
            }
        }

        #endregion

        #endregion Public Properties

        #region INotifyPropertyChanged

        /// <summary>
        /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                _propertyChanged += value;
            }
            remove
            {
                _propertyChanged -= value;
            }
        }

        private event PropertyChangedEventHandler _propertyChanged;

        #endregion INotifyPropertyChanged

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Raise INotifyPropertyChanged.PropertyChanged event.
        /// </summary>
        /// <param name="e">event arguments with name of the changed property</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (_propertyChanged != null)
            {
                _propertyChanged(this, e);
            }
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Internal Methodes
        //
        //-------------------------------------------------------------------

        #region Internal Methodes

        // Propagate theme changes to contained headers
        /*internal void OnThemeChanged()
        {
            if (Header != null)
            {
                AvaloniaObject d = Header as AvaloniaObject;

                if (d != null)
                {
                    Control fe;
                    AvaloniaContentElement fce;
                    Helper.DowncastToFEorFCE(d, out fe, out fce, false);

                    if (fe != null || fce != null)
                    {
                        TreeWalkHelper.InvalidateOnResourcesChange(fe, fce, ResourcesChangeInfo.ThemeChangeInfo);
                    }
                }
            }
        }*/

        /// <summary>
        /// ensure final column width is no less than a value
        /// </summary>
        internal double EnsureWidth(double width)
        {
            if (width > DesiredWidth)
            {
                DesiredWidth = width;
            }
            return DesiredWidth;
        }

        /// <summary>
        /// column collection should call this when remove a column from the collection.
        /// </summary>
        internal void ResetPrivateData()
        {
            _actualIndex = -1;
            _desiredWidth = 0.0;
            _state = Double.IsNaN(Width) ? ColumnMeasureState.Init : ColumnMeasureState.SpecificWidth;
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        ///  Reachable State Transition Diagram:
        ///
        ///                        +- - - - - - - - - - +
        ///                        |       Init         |
        ///                        +- - - - - - - - - - +
        ///                           / /|   A   |\ \
        ///                          / /     |     \ \
        ///                         / /      |      \ \
        ///                        / /       |       \ \
        ///                       / /        |        \ \
        ///                      / /         |         \ \
        ///                     / /          |          \ \
        ///                   |/ /           |           \ \|
        ///    +--------------------+        |        +--------------------+
        ///    |      Headered      |--------+------->|        Data        |
        ///    +--------------------+        |        +--------------------+
        ///                      \           |           /
        ///                       \          |          /
        ///                        \         |         /
        ///                         \        |        /
        ///                          \       |       /
        ///                           \      |      /
        ///                            \|    |    |/
        ///                        +--------------------+
        ///                        |   SpecificWidth    |
        ///                        +--------------------+
        ///
        /// Note:
        ///
        /// 1) Init is a intermidiated state, that is a column should not stop on such a state;
        /// 2) Headered, Data and SpecificWidth are terminal state, that is a column can stop at
        ///     the state if no further data change / user interaction to trigger a change.
        ///
        /// Typical state transiton flows:
        ///
        ///   Case 1: column is auto, LV has header and data
        ///     Init --> [ Headered --> ] Data
        ///
        ///   Case 2: column is auto, LV has header but no data
        ///     Init --> Headered
        ///
        ///   Case 3: column has a specified width
        ///     SpecificWidth
        ///
        ///   Case 4: couble click a column of case 3
        ///     SpecificWidth --> Init --> Headered / Data (depends on the data)
        ///
        ///   Case 5: resize a column which has width as auto
        ///     Headered / Data --> SpecificWidth
        ///
        /// </summary>
        internal ColumnMeasureState State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;

                    if (value != ColumnMeasureState.Init) // Headered, Data or SpecificWidth
                    {
                        UpdateActualWidth();
                    }
                    else
                    {
                        DesiredWidth = 0.0;
                    }
                }
                else if (value == ColumnMeasureState.SpecificWidth)
                {
                    UpdateActualWidth();
                }
            }
        }

        // NOTE: Perf optimization. To avoid re-search index again and again
        // by every GridViewRowPresenter, add an index here.
        internal int ActualIndex
        {
            get { return _actualIndex; }
            set { _actualIndex = value; }
        }

        /// <summary>
        /// Minimum width requirement for this column. Shared by all visible cells in this column
        /// </summary>
        /// <remarks>
        /// Below table shows an example of how column width is shared:
        ///
        ///     1. In the first round of layout, DesiredWidth continue to grow when each row comes into measure
        ///
        ///     2. after the 1st round, the desired width for this column is decided, each row on layout updated
        ///         with check this value with its copy of maxDesiredWidth, if not equal, triger another round of
        ///         measure.
        ///
        ///     3. after 2nd round of layout, all rows should be in same size.
        ///     +------------+-----------+--------------+------------+------------+-------------+
        ///     |            |   Width   |    Cell      |  Desired   | Presenter  |   Column    |
        ///     |            |           | DesiredWidth |   Width    | LocalCopy  |    State    |
        ///     |------------+-----------+--------------+------------+------------|-------------|
        ///     | 1st round  |   NaN     |              |    10.0    |            |    Init     |
        ///     |            |           |              |            |            |             |
        ///     |  (row 1)   |           |    12.0      |    12.0    |            |             |
        ///     |  (row 2)   |           |    70.0      |    70.0    |            |             |
        ///     |  (row 3)   |           |    80.0      |    80.0    |            |             |
        ///     |  (row 4)   |           |    60.0      |    80.0    |            |             |
        ///     |------------+-----------+--------------+------------+------------|-------------|
        ///     | layout     |   NaN     |              |            |            |             |
        ///     | updated    |           |              |            |            |             |
        ///     |            |           |              |            |            |             |
        ///     | [hdr_row]  |           |              |            |            | [Headered]* |
        ///     |            |           |              |            |            |             |
        ///     |  (row 1)   |           |              |    80.0    |    12.0    |    Data     |
        ///     |  (row 2)   |           |              |    80.0    |    70.0    |             |
        ///     |  (row 3)   |           |              |    80.0    |    80.0    |             |
        ///     |  (row 4)   |           |              |    80.0    |    80.0    |             |
        ///     |------------+-----------+--------------+------------+------------|-------------|
        ///     | 2nd round  |   NaN     |              |            |            |             |
        ///     |            |           |              |            |            |             |
        ///     |  (row 1)   |           |    12.0      |    80.0    |    80.0    |             |
        ///     |  (row 2)   |           |    70.0      |    80.0    |    80.0    |             |
        ///     +------------+-----------+--------------+------------+------------+-------------+
        ///
        ///   * Depends on the tree structure, it is possible that HeaderRowPresenter accomplish first
        ///     layout first. So the column state can be Headered for a while. But will be changed to
        ///     'Data' once a data row accomplish its first layout.
        ///
        /// </remarks>
        internal double DesiredWidth
        {
            get { return _desiredWidth; }
            private set { _desiredWidth = value; }
        }

        internal const string c_ActualWidthName = "ActualWidth";

        #endregion

        /*#region InheritanceContext

        /// <summary>
        ///     InheritanceContext
        /// </summary>
        internal override DependencyObject InheritanceContext
        {
            get { return _inheritanceContext; }
        }

        // Receive a new inheritance context
        internal override void AddInheritanceContext(DependencyObject context, DependencyProperty property)
        {
            // reinforce that no one can compete to be mentor of this element.
            if (_inheritanceContext == null && context != null)
            {
                // Pick up the new context
                _inheritanceContext = context;
                OnInheritanceContextChanged(EventArgs.Empty);
            }
        }

        // Remove an inheritance context
        internal override void RemoveInheritanceContext(DependencyObject context, DependencyProperty property)
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

        #endregion InheritanceContext*/

        //-------------------------------------------------------------------
        //
        //  Private Methods / Fields
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Helper to raise INotifyPropertyChanged.PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Name of the changed property</param>
        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// force ActualWidth to be reevaluated
        /// </summary>
        private void UpdateActualWidth()
        {
            ActualWidth = (State == ColumnMeasureState.SpecificWidth) ? Width : DesiredWidth;
        }

        #endregion

        #region Private Fields

        private double _desiredWidth;
        private int _actualIndex;
        private double _actualWidth;
        private ColumnMeasureState _state;

        #endregion
    }

    /// <summary>
    /// States of column when doing layout
    /// See GridViewColumn.State for reachable state transition diagram
    /// </summary>
    internal enum ColumnMeasureState
    {
        /// <summary>
        /// Column width is just initialized and will size to content width
        /// </summary>
        Init = 0,

        /// <summary>
        /// Column width reach max desired width of header(s) in this column
        /// </summary>
        Headered = 1,

        /// <summary>
        /// Column width reach max desired width of data row(s) in this column
        /// </summary>
        Data = 2,

        /// <summary>
        /// Column has a specific value as width
        /// </summary>
        SpecificWidth = 3
    }
}
