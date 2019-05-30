// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/) 
// 
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Avalonia;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    /// <summary>
    /// DefinitionBase provides core functionality used internally by Grid
    /// and ColumnDefinitionCollection / RowDefinitionCollection
    /// </summary>
    public abstract class DefinitionBase : FrameworkContentElement
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal DefinitionBase(bool isColumnDefinition)
        {
            _isColumnDefinition = isColumnDefinition;
            _parentIndex = -1;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// SharedSizeGroup property.
        /// </summary>
        public string SharedSizeGroup
        {
            get { return (string) GetValue(SharedSizeGroupProperty); }
            set { SetValue(SharedSizeGroupProperty, value); }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Callback to notify about entering model tree.
        /// </summary>
        internal void OnEnterParentTree()
        {
            if (_sharedState == null)
            {
                //  start with getting SharedSizeGroup value. 
                //  this property is NOT inhereted which should result in better overall perf.
                string sharedSizeGroupId = SharedSizeGroup;
                if (sharedSizeGroupId != null)
                {
                    SharedSizeScope privateSharedSizeScope = PrivateSharedSizeScope;
                    if (privateSharedSizeScope != null)
                    {
                        _sharedState = privateSharedSizeScope.EnsureSharedState(sharedSizeGroupId);
                        _sharedState.AddMember(this);
                    }
                }
            }
        }

        /// <summary>
        /// Callback to notify about exitting model tree.
        /// </summary>
        internal void OnExitParentTree()
        {
            _offset = 0;
            if (_sharedState != null)
            {
                _sharedState.RemoveMember(this);
                _sharedState = null;
            }
        }

        /// <summary>
        /// Performs action preparing definition to enter layout calculation mode.
        /// </summary>
        internal void OnBeforeLayout(Grid grid)
        {
            //  reset layout state.
            _minSize = 0;
            LayoutWasUpdated = true;

            //  defer verification for shared definitions
            if (_sharedState != null)   {   _sharedState.EnsureDeferredValidation(grid);    }
        }

        /// <summary>
        /// Updates min size.
        /// </summary>
        /// <param name="minSize">New size.</param>
        internal void UpdateMinSize(double minSize)
        {
            _minSize = Math.Max(_minSize, minSize);
        }

        /// <summary>
        /// Sets min size.
        /// </summary>
        /// <param name="minSize">New size.</param>
        internal void SetMinSize(double minSize)
        {
            _minSize = minSize;
        }

        /// <summary>
        /// <see cref="PropertyMetadata.PropertyChangedCallback"/>
        /// </summary>
        /// <remarks>
        /// This method needs to be internal to be accessable from derived classes.
        /// </remarks>
        internal static void OnUserSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DefinitionBase definition = (DefinitionBase) d;

            if (definition.InParentLogicalTree)
            {
                if (definition._sharedState != null)
                {
                    definition._sharedState.Invalidate();
                }
                else
                {
                    Grid parentGrid = (Grid) definition.Parent;

                    if (((GridLength) e.OldValue).GridUnitType != ((GridLength) e.NewValue).GridUnitType)
                    {
                        parentGrid.Invalidate();
                    }
                    else
                    {   
                        parentGrid.InvalidateMeasure();     
                    }
                }
            }
        }

        /// <summary>
        /// <see cref="DependencyProperty.ValidateValueCallback"/>
        /// </summary>
        /// <remarks>
        /// This method needs to be internal to be accessable from derived classes.
        /// </remarks>
        internal static bool IsUserSizePropertyValueValid(object value)
        {
            return (((GridLength)value).Value >= 0);
        }

        /// <summary>
        /// <see cref="PropertyMetadata.PropertyChangedCallback"/>
        /// </summary>
        /// <remarks>
        /// This method needs to be internal to be accessable from derived classes.
        /// </remarks>
        internal static void OnUserMinSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DefinitionBase definition = (DefinitionBase) d;

            if (definition.InParentLogicalTree)
            {
                Grid parentGrid = (Grid) definition.Parent;
                parentGrid.InvalidateMeasure();
            }
        }

        /// <summary>
        /// <see cref="DependencyProperty.ValidateValueCallback"/>
        /// </summary>
        /// <remarks>
        /// This method needs to be internal to be accessable from derived classes.
        /// </remarks>
        internal static bool IsUserMinSizePropertyValueValid(object value)
        {
            double v = (double)value;
            return (!DoubleUtil.IsNaN(v) && v >= 0.0d && !Double.IsPositiveInfinity(v));
        }

        /// <summary>
        /// <see cref="PropertyMetadata.PropertyChangedCallback"/>
        /// </summary>
        /// <remarks>
        /// This method needs to be internal to be accessable from derived classes.
        /// </remarks>
        internal static void OnUserMaxSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DefinitionBase definition = (DefinitionBase) d;

            if (definition.InParentLogicalTree)
            {
                Grid parentGrid = (Grid) definition.Parent;
                parentGrid.InvalidateMeasure();
            }
        }

        /// <summary>
        /// <see cref="DependencyProperty.ValidateValueCallback"/>
        /// </summary>
        /// <remarks>
        /// This method needs to be internal to be accessable from derived classes.
        /// </remarks>
        internal static bool IsUserMaxSizePropertyValueValid(object value)
        {
            double v = (double)value;
            return (!DoubleUtil.IsNaN(v) && v >= 0.0d);
        }

        /// <summary>
        /// <see cref="PropertyMetadata.PropertyChangedCallback"/>
        /// </summary>
        /// <remarks>
        /// This method reflects Grid.SharedScopeProperty state by setting / clearing
        /// dynamic property PrivateSharedSizeScopeProperty. Value of PrivateSharedSizeScopeProperty
        /// is a collection of SharedSizeState objects for the scope.
        /// Also PrivateSharedSizeScopeProperty is FrameworkPropertyMetadataOptions.Inherits property. So that all children
        /// elements belonging to a certain scope can easily access SharedSizeState collection. As well
        /// as been norified about enter / exit a scope.
        /// </remarks>
        internal static void OnIsSharedSizeScopePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //  is it possible to optimize here something like this:
            //  if ((bool)d.GetValue(Grid.IsSharedSizeScopeProperty) == (d.GetLocalValue(PrivateSharedSizeScopeProperty) != null)
            //  { /* do nothing */ }
            if ((bool) e.NewValue)
            {
                SharedSizeScope sharedStatesCollection = new SharedSizeScope();
                d.SetValue(PrivateSharedSizeScopeProperty, sharedStatesCollection);
            }
            else
            {
                d.ClearValue(PrivateSharedSizeScopeProperty);
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Returns <c>true</c> if this definition is a part of shared group.
        /// </summary>
        internal bool IsShared
        {
            get { return (_sharedState != null); }
        }

        /// <summary>
        /// Internal accessor to user size field.
        /// </summary>
        internal GridLength UserSize
        {
            get { return (_sharedState != null ? _sharedState.UserSize : UserSizeValueCache); }
        }

        /// <summary>
        /// Internal accessor to user min size field.
        /// </summary>
        internal double UserMinSize
        {
            get { return (UserMinSizeValueCache); }
        }

        /// <summary>
        /// Internal accessor to user max size field.
        /// </summary>
        internal double UserMaxSize
        {
            get { return (UserMaxSizeValueCache); }
        }

        /// <summary>
        /// DefinitionBase's index in the parents collection.
        /// </summary>
        internal int Index
        {
            get
            {
                return (_parentIndex);
            }
            set
            {
                Debug.Assert(value >= -1 && _parentIndex != value);
                _parentIndex = value;
            }
        }

        /// <summary>
        /// Layout-time user size type.
        /// </summary>
        internal Grid.LayoutTimeSizeType SizeType
        {
            get { return (_sizeType); }
            set { _sizeType = value; }
        }

        /// <summary>
        /// Returns or sets measure size for the definition.
        /// </summary>
        internal double MeasureSize
        {
            get { return (_measureSize); }
            set { _measureSize = value; }
        }

        /// <summary>
        /// Returns definition's layout time type sensitive preferred size.
        /// </summary>
        /// <remarks>
        /// Returned value is guaranteed to be true preferred size.
        /// </remarks>
        internal double PreferredSize
        {
            get
            {
                double preferredSize = MinSize;
                if (    _sizeType != Grid.LayoutTimeSizeType.Auto
                    &&  preferredSize < _measureSize    )
                {
                    preferredSize = _measureSize;
                }
                return (preferredSize);
            }
        }

        /// <summary>
        /// Returns or sets size cache for the definition.
        /// </summary>
        internal double SizeCache
        {
            get { return (_sizeCache); }
            set { _sizeCache = value; }
        }

        /// <summary>
        /// Returns min size.
        /// </summary>
        internal double MinSize
        {
            get
            {
                double minSize = _minSize;
                if (    UseSharedMinimum
                    &&  _sharedState != null
                    &&  minSize < _sharedState.MinSize  )
                {
                    minSize = _sharedState.MinSize;
                }
                return (minSize);
            }
        }

        /// <summary>
        /// Returns min size, always taking into account shared state.
        /// </summary>
        internal double MinSizeForArrange
        {
            get
            {
                double minSize = _minSize;
                if (    _sharedState != null
                    &&  (UseSharedMinimum || !LayoutWasUpdated)
                    &&  minSize < _sharedState.MinSize  )
                {
                    minSize = _sharedState.MinSize;
                }
                return (minSize);
            }
        }

        /// <summary>
        /// Offset.
        /// </summary>
        internal double FinalOffset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        /// <summary>
        /// Internal helper to access up-to-date UserSize property value.
        /// </summary>
        internal GridLength UserSizeValueCache
        {
            get
            { 
                return (GridLength) GetValue(
                        _isColumnDefinition ?
                        ColumnDefinition.WidthProperty :
                        RowDefinition.HeightProperty);
            }
        }

        /// <summary>
        /// Internal helper to access up-to-date UserMinSize property value.
        /// </summary>
        internal double UserMinSizeValueCache
        {
            get
            {
                return (double) GetValue(
                        _isColumnDefinition ? 
                        ColumnDefinition.MinWidthProperty : 
                        RowDefinition.MinHeightProperty);
            }
        }

        /// <summary>
        /// Internal helper to access up-to-date UserMaxSize property value.
        /// </summary>
        internal double UserMaxSizeValueCache
        {
            get
            {
                return (double) GetValue(
                        _isColumnDefinition ?
                        ColumnDefinition.MaxWidthProperty :
                        RowDefinition.MaxHeightProperty);
            }
        }

        /// <summary>
        /// Protected. Returns <c>true</c> if this DefinitionBase instance is in parent's logical tree.
        /// </summary>
        internal bool InParentLogicalTree
        {
            get { return (_parentIndex != -1); }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// SetFlags is used to set or unset one or multiple
        /// flags on the object.
        /// </summary>
        private void SetFlags(bool value, Flags flags)
        {
            _flags = value ? (_flags | flags) : (_flags & (~flags));
        }

        /// <summary>
        /// CheckFlagsAnd returns <c>true</c> if all the flags in the
        /// given bitmask are set on the object.
        /// </summary>
        private bool CheckFlagsAnd(Flags flags)
        {
            return ((_flags & flags) == flags);
        }

        /// <summary>
        /// <see cref="PropertyMetadata.PropertyChangedCallback"/>
        /// </summary>
        private static void OnSharedSizeGroupPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DefinitionBase definition = (DefinitionBase) d;
            
            if (definition.InParentLogicalTree)
            {
                string sharedSizeGroupId = (string) e.NewValue;

                if (definition._sharedState != null)
                {
                    //  if definition is already registered AND shared size group id is changing,
                    //  then un-register the definition from the current shared size state object.
                    definition._sharedState.RemoveMember(definition);
                    definition._sharedState = null;
                }
                
                if ((definition._sharedState == null) && (sharedSizeGroupId != null))
                {
                    SharedSizeScope privateSharedSizeScope = definition.PrivateSharedSizeScope;
                    if (privateSharedSizeScope != null)
                    {
                        //  if definition is not registered and both: shared size group id AND private shared scope 
                        //  are available, then register definition.
                        definition._sharedState = privateSharedSizeScope.EnsureSharedState(sharedSizeGroupId);
                        definition._sharedState.AddMember(definition);
                    }
                }
            }
        }

        /// <summary>
        /// <see cref="DependencyProperty.ValidateValueCallback"/>
        /// </summary>
        /// <remarks>
        /// Verifies that Shared Size Group Property string
        /// a) not empty.
        /// b) contains only letters, digits and underscore ('_').
        /// c) does not start with a digit.
        /// </remarks>
        private static bool SharedSizeGroupPropertyValueValid(object value)
        {
            //  null is default value
            if (value == null)
            {
                return (true);
            }

            string id = (string)value;

            if (id != string.Empty)
            {
                int i = -1;
                while (++i < id.Length)
                {
                    bool isDigit = Char.IsDigit(id[i]);

                    if (    (i == 0 && isDigit)
                        ||  !(  isDigit
                            ||  Char.IsLetter(id[i])
                            ||  '_' == id[i]    )   )
                    {
                        break;
                    }
                }

                if (i == id.Length)
                {
                    return (true);
                }
            }

            return (false);
        }

        /// <summary>
        /// <see cref="PropertyMetadata.PropertyChangedCallback"/>
        /// </summary>
        /// <remark>
        /// OnPrivateSharedSizeScopePropertyChanged is called when new scope enters or
        /// existing scope just left. In both cases if the DefinitionBase object is already registered
        /// in SharedSizeState, it should un-register and register itself in a new one.
        /// </remark>
        private static void OnPrivateSharedSizeScopePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DefinitionBase definition = (DefinitionBase)d;

            if (definition.InParentLogicalTree)
            {
                SharedSizeScope privateSharedSizeScope = (SharedSizeScope) e.NewValue;

                if (definition._sharedState != null)
                {
                    //  if definition is already registered And shared size scope is changing,
                    //  then un-register the definition from the current shared size state object.
                    definition._sharedState.RemoveMember(definition);
                    definition._sharedState = null;
                }

                if ((definition._sharedState == null) && (privateSharedSizeScope != null))
                {
                    string sharedSizeGroup = definition.SharedSizeGroup;
                    if (sharedSizeGroup != null)
                    {
                        //  if definition is not registered and both: shared size group id AND private shared scope 
                        //  are available, then register definition.
                        definition._sharedState = privateSharedSizeScope.EnsureSharedState(definition.SharedSizeGroup);
                        definition._sharedState.AddMember(definition);
                    }
                }
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// Private getter of shared state collection dynamic property.
        /// </summary>
        private SharedSizeScope PrivateSharedSizeScope
        {
            get { return (SharedSizeScope) GetValue(PrivateSharedSizeScopeProperty); }
        }

        /// <summary>
        /// Convenience accessor to UseSharedMinimum flag
        /// </summary>
        private bool UseSharedMinimum
        {
            get { return (CheckFlagsAnd(Flags.UseSharedMinimum)); }
            set { SetFlags(value, Flags.UseSharedMinimum); }
        }

        /// <summary>
        /// Convenience accessor to LayoutWasUpdated flag
        /// </summary>
        private bool LayoutWasUpdated
        {
            get { return (CheckFlagsAnd(Flags.LayoutWasUpdated)); }
            set { SetFlags(value, Flags.LayoutWasUpdated); }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        private readonly bool _isColumnDefinition;      //  when "true", this is a ColumnDefinition; when "false" this is a RowDefinition (faster than a type check)
        private Flags _flags;                           //  flags reflecting various aspects of internal state
        private int _parentIndex;                       //  this instance's index in parent's children collection

        private Grid.LayoutTimeSizeType _sizeType;      //  layout-time user size type. it may differ from _userSizeValueCache.UnitType when calculating "to-content"

        private double _minSize;                        //  used during measure to accumulate size for "Auto" and "Star" DefinitionBase's
        private double _measureSize;                    //  size, calculated to be the input contstraint size for Child.Measure
        private double _sizeCache;                      //  cache used for various purposes (sorting, caching, etc) during calculations
        private double _offset;                         //  offset of the DefinitionBase from left / top corner (assuming LTR case)

        private SharedSizeState _sharedState;           //  reference to shared state object this instance is registered with
        
        internal const bool ThisIsColumnDefinition = true;
        internal const bool ThisIsRowDefinition = false;

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Private Structures / Classes
        //
        //------------------------------------------------------

        #region Private Structures Classes

        [System.Flags]
        private enum Flags : byte
        {
            //
            //  bool flags
            //
            UseSharedMinimum                    =   0x00000020,     //  when "1", definition will take into account shared state's minimum
            LayoutWasUpdated                    =   0x00000040,     //  set to "1" every time the parent grid is measured
        }

        /// <summary>
        /// Collection of shared states objects for a single scope
        /// </summary>
        private class SharedSizeScope
        {
            /// <summary>
            /// Returns SharedSizeState object for a given group.
            /// Creates a new StatedState object if necessary.
            /// </summary>
            internal SharedSizeState EnsureSharedState(string sharedSizeGroup)
            {
                //  check that sharedSizeGroup is not default
                Debug.Assert(sharedSizeGroup != null);

                SharedSizeState sharedState = _registry[sharedSizeGroup] as SharedSizeState;
                if (sharedState == null)
                {
                    sharedState = new SharedSizeState(this, sharedSizeGroup);
                    _registry[sharedSizeGroup] = sharedState;
                }
                return (sharedState);
            }

            /// <summary>
            /// Removes an entry in the registry by the given key.
            /// </summary>
            internal void Remove(object key)
            {
                Debug.Assert(_registry.Contains(key));
                _registry.Remove(key);
            }

            private Hashtable _registry = new Hashtable();  //  storage for shared state objects
        }

        /// <summary>
        /// Implementation of per shared group state object
        /// </summary>
        private class SharedSizeState
        {
            /// <summary>
            /// Default ctor.
            /// </summary>
            internal SharedSizeState(SharedSizeScope sharedSizeScope, string sharedSizeGroupId)
            {
                Debug.Assert(sharedSizeScope != null && sharedSizeGroupId != null);
                _sharedSizeScope = sharedSizeScope;
                _sharedSizeGroupId = sharedSizeGroupId;
                _registry = new List<DefinitionBase>();
                _layoutUpdated = new EventHandler(OnLayoutUpdated);
                _broadcastInvalidation = true;
            }

            /// <summary>
            /// Adds / registers a definition instance.
            /// </summary>
            internal void AddMember(DefinitionBase member)
            {
                Debug.Assert(!_registry.Contains(member));
                _registry.Add(member);
                Invalidate();
            }

            /// <summary>
            /// Removes / un-registers a definition instance.
            /// </summary>
            /// <remarks>
            /// If the collection of registered definitions becomes empty
            /// instantiates self removal from owner's collection.
            /// </remarks>
            internal void RemoveMember(DefinitionBase member)
            {
                Invalidate();
                _registry.Remove(member);

                if (_registry.Count == 0)
                {
                    _sharedSizeScope.Remove(_sharedSizeGroupId);
                }
            }

            /// <summary>
            /// Propogates invalidations for all registered definitions.
            /// Resets its own state.
            /// </summary>
            internal void Invalidate()
            {
                _userSizeValid = false;

                if (_broadcastInvalidation)
                {
                    for (int i = 0, count = _registry.Count; i < count; ++i)
                    {
                        Grid parentGrid = (Grid)(_registry[i].Parent);
                        parentGrid.Invalidate();
                    }
                    _broadcastInvalidation = false;
                }
            }

            /// <summary>
            /// Makes sure that one and only one layout updated handler is registered for this shared state.
            /// </summary>
            internal void EnsureDeferredValidation(UIElement layoutUpdatedHost)
            {
                if (_layoutUpdatedHost == null)
                {
                    _layoutUpdatedHost = layoutUpdatedHost;
                    _layoutUpdatedHost.LayoutUpdated += _layoutUpdated;
                }
            }

            /// <summary>
            /// DefinitionBase's specific code.
            /// </summary>
            internal double MinSize
            {
                get
                {
                    if (!_userSizeValid) { EnsureUserSizeValid(); }
                    return (_minSize);
                }
            }

            /// <summary>
            /// DefinitionBase's specific code.
            /// </summary>
            internal GridLength UserSize
            {
                get
                {
                    if (!_userSizeValid) { EnsureUserSizeValid(); }
                    return (_userSize);
                }
            }

            private void EnsureUserSizeValid()
            {
                _userSize = new GridLength(1, GridUnitType.Auto);

                for (int i = 0, count = _registry.Count; i < count; ++i)
                {
                    Debug.Assert(   _userSize.GridUnitType == GridUnitType.Auto
                                ||  _userSize.GridUnitType == GridUnitType.Pixel    );

                    GridLength currentGridLength = _registry[i].UserSizeValueCache;
                    if (currentGridLength.GridUnitType == GridUnitType.Pixel)
                    {
                        if (_userSize.GridUnitType == GridUnitType.Auto)
                        {
                            _userSize = currentGridLength;
                        }
                        else if (_userSize.Value < currentGridLength.Value)
                        {
                            _userSize = currentGridLength;
                        }
                    }
                }
                //  taking maximum with user size effectively prevents squishy-ness.
                //  this is a "solution" to avoid shared definitions from been sized to
                //  different final size at arrange time, if / when different grids receive
                //  different final sizes.
                _minSize = _userSize.IsAbsolute ? _userSize.Value : 0.0;

                _userSizeValid = true;
            }

            /// <summary>
            /// OnLayoutUpdated handler. Validates that all participating definitions
            /// have updated min size value. Forces another layout update cycle if needed.
            /// </summary>
            private void OnLayoutUpdated(object sender, EventArgs e)
            {
                double sharedMinSize = 0;

                //  accumulate min size of all participating definitions
                for (int i = 0, count = _registry.Count; i < count; ++i)
                {
                    sharedMinSize = Math.Max(sharedMinSize, _registry[i].MinSize);
                }

                bool sharedMinSizeChanged = !DoubleUtil.AreClose(_minSize, sharedMinSize);

                //  compare accumulated min size with min sizes of the individual definitions
                for (int i = 0, count = _registry.Count; i < count; ++i)
                {
                    DefinitionBase definitionBase = _registry[i];

                    if (sharedMinSizeChanged ||  definitionBase.LayoutWasUpdated)
                    {
                        //  if definition's min size is different, then need to re-measure
                        if (!DoubleUtil.AreClose(sharedMinSize, definitionBase.MinSize))
                        {
                            Grid parentGrid = (Grid)definitionBase.Parent;
                            parentGrid.InvalidateMeasure();
                            definitionBase.UseSharedMinimum = true;
                        }
                        else
                        {
                            definitionBase.UseSharedMinimum = false;
                            
                            //  if measure is valid then also need to check arrange.
                            //  Note: definitionBase.SizeCache is volatile but at this point 
                            //  it contains up-to-date final size
                            if (!DoubleUtil.AreClose(sharedMinSize, definitionBase.SizeCache))
                            {
                                Grid parentGrid = (Grid)definitionBase.Parent;
                                parentGrid.InvalidateArrange();
                            }
                        }

                        definitionBase.LayoutWasUpdated = false;
                    }
                }

                _minSize = sharedMinSize;

                _layoutUpdatedHost.LayoutUpdated -= _layoutUpdated;
                _layoutUpdatedHost = null;

                _broadcastInvalidation = true;
            }
            
            private readonly SharedSizeScope _sharedSizeScope;  //  the scope this state belongs to
            private readonly string _sharedSizeGroupId;         //  Id of the shared size group this object is servicing
            private readonly List<DefinitionBase> _registry;    //  registry of participating definitions
            private readonly EventHandler _layoutUpdated;       //  instance event handler for layout updated event
            private UIElement _layoutUpdatedHost;               //  UIElement for which layout updated event handler is registered
            private bool _broadcastInvalidation;                //  "true" when broadcasting of invalidation is needed
            private bool _userSizeValid;                        //  "true" when _userSize is up to date
            private GridLength _userSize;                       //  shared state
            private double _minSize;                            //  shared state
        }

        #endregion Private Structures Classes

        //------------------------------------------------------
        //
        //  Properties
        //
        //------------------------------------------------------

        #region Properties

        /// <summary>
        /// Private shared size scope property holds a collection of shared state objects for the a given shared size scope.
        /// <see cref="OnIsSharedSizeScopePropertyChanged"/>
        /// </summary>
        internal static readonly DependencyProperty PrivateSharedSizeScopeProperty =
                DependencyProperty.RegisterAttached(
                        "PrivateSharedSizeScope",
                        typeof(SharedSizeScope),
                        typeof(DefinitionBase),
                        new FrameworkPropertyMetadata(
                                null,
                                FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Shared size group property marks column / row definition as belonging to a group "Foo" or "Bar".
        /// </summary>
        /// <remarks>
        /// Value of the Shared Size Group Property must satisfy the following rules:
        /// <list type="bullet">
        /// <item><description>
        /// String must not be empty.
        /// </description></item>
        /// <item><description>
        /// String must consist of letters, digits and underscore ('_') only.
        /// </description></item>
        /// <item><description>
        /// String must not start with a digit.
        /// </description></item>
        /// </list>
        /// </remarks>
        public static readonly DependencyProperty SharedSizeGroupProperty =
                DependencyProperty.Register(
                        "SharedSizeGroup",
                        typeof(string),
                        typeof(DefinitionBase),
                        new FrameworkPropertyMetadata(new PropertyChangedCallback(OnSharedSizeGroupPropertyChanged)),
                        new ValidateValueCallback(SharedSizeGroupPropertyValueValid));

        /// <summary>
        /// Static ctor. Used for static registration of properties.
        /// </summary>
        static DefinitionBase()
        {
            PrivateSharedSizeScopeProperty.OverrideMetadata(
                    typeof(DefinitionBase),
                    new FrameworkPropertyMetadata(new PropertyChangedCallback(OnPrivateSharedSizeScopePropertyChanged)));
        }

        #endregion Properties
    }
}
