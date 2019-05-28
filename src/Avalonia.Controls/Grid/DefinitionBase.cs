// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Diagnostics;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for <see cref="ColumnDefinition"/> and <see cref="RowDefinition"/>.
    /// </summary>
    public abstract class DefinitionBase : AvaloniaObject
    {
        /// <summary>
        /// Static ctor. Used for static registration of properties.
        /// </summary>
        static DefinitionBase()
        {
            SharedSizeGroupProperty.Changed.AddClassHandler<DefinitionBase>(OnSharedSizeGroupPropertyChanged);
        }
        internal bool UseSharedMinimum { get; set; }
        internal bool LayoutWasUpdated { get; set; }
        
        private int _parentIndex = -1;                       //  this instance's index in parent's children collection
        private LayoutTimeSizeType _sizeType;      //  layout-time user size type. it may differ from _userSizeValueCache.UnitType when calculating "to-content"
        private double _minSize;                        //  used during measure to accumulate size for "Auto" and "Star" DefinitionBase's
        private double _measureSize;                    //  size, calculated to be the input contstraint size for Child.Measure
        private double _sizeCache;                      //  cache used for various purposes (sorting, caching, etc) during calculations
        private double _offset;                         //  offset of the DefinitionBase from left / top corner (assuming LTR case)
        internal SharedSizeScope _privateSharedSizeScope;
        private SharedSizeState _sharedState;           //  reference to shared state object this instance is registered with
        private bool _successUpdateSharedScope;

        /// <summary>
        /// Defines the <see cref="SharedSizeGroup"/> property.
        /// </summary>
        public static readonly StyledProperty<string> SharedSizeGroupProperty =
            AvaloniaProperty.Register<DefinitionBase, string>(nameof(SharedSizeGroup), inherits: true);

        /// <summary>
        /// Gets or sets the name of the shared size group of the column or row.
        /// </summary>
        public string SharedSizeGroup
        {
            get { return GetValue(SharedSizeGroupProperty); }
            set { SetValue(SharedSizeGroupProperty, value); }
        }
        /// <summary>
        /// Callback to notify about entering model tree.
        /// </summary>
        internal void OnEnterParentTree(Grid grid, int index)
        {
            Parent = grid;
            _parentIndex = index;
        }

        internal void UpdateSharedScope()
        {
            if (_sharedState == null & 
                SharedSizeGroup != null & 
                Parent?.sharedSizeScope != null & 
                !_successUpdateSharedScope)
            {
                _privateSharedSizeScope = Parent.sharedSizeScope;
                _sharedState = _privateSharedSizeScope.EnsureSharedState(SharedSizeGroup);
                _sharedState.AddMember(this);
                _successUpdateSharedScope = true;
            }
        }

        internal Grid Parent { get; set; }

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
            if (SharedSizeGroup != null)
                UpdateSharedScope();

            //  reset layout state.
            _minSize = 0;
            LayoutWasUpdated = true;

            //  defer verification for shared definitions
            if (_sharedState != null)
            {
                _sharedState.EnsureDeferredValidation(grid);
            }
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

        /// <remarks>
        /// This method needs to be internal to be accessable from derived classes.
        /// </remarks>
        internal void OnUserSizePropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            _sharedState?.Invalidate();
        }

        /// <remarks>
        /// This method needs to be internal to be accessable from derived classes.
        /// </remarks>
        internal static bool IsUserMinSizePropertyValueValid(object value)
        {
            double v = (double)value;
            return (!double.IsNaN(v) && v >= 0.0d && !Double.IsPositiveInfinity(v));
        }

        /// <remarks>
        /// This method needs to be internal to be accessable from derived classes.
        /// </remarks>
        internal static void OnUserMaxSizePropertyChanged(DefinitionBase definition, AvaloniaPropertyChangedEventArgs e)
        {
            Grid parentGrid = (Grid)definition.Parent;
            parentGrid.InvalidateMeasure();

        }

        /// <remarks>
        /// This method needs to be internal to be accessable from derived classes.
        /// </remarks>
        internal static bool IsUserMaxSizePropertyValueValid(object value)
        {
            double v = (double)value;
            return (!double.IsNaN(v) && v >= 0.0d);
        }

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
        internal LayoutTimeSizeType SizeType
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
                if (_sizeType != LayoutTimeSizeType.Auto
                    && preferredSize < _measureSize)
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
                if (UseSharedMinimum
                    && _sharedState != null
                    && minSize < _sharedState.MinSize)
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
                if (_sharedState != null
                    && (UseSharedMinimum || !LayoutWasUpdated)
                    && minSize < _sharedState.MinSize)
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
        internal abstract GridLength UserSizeValueCache { get; }

        /// <summary>
        /// Internal helper to access up-to-date UserMinSize property value.
        /// </summary>
        internal abstract double UserMinSizeValueCache { get; }

        /// <summary>
        /// Internal helper to access up-to-date UserMaxSize property value.
        /// </summary>
        internal abstract double UserMaxSizeValueCache { get; }

        private static void OnSharedSizeGroupPropertyChanged(DefinitionBase definition, AvaloniaPropertyChangedEventArgs e)
        {
            string sharedSizeGroupId = (string)e.NewValue;

            if (definition._sharedState != null)
            {
                //  if definition is already registered AND shared size group id is changing,
                //  then un-register the definition from the current shared size state object.
                definition._sharedState.RemoveMember(definition);
                definition._sharedState = null;
            }

            if ((definition._sharedState == null) && (sharedSizeGroupId != null))
            {
                var privateSharedSizeScope = definition._privateSharedSizeScope;
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
}