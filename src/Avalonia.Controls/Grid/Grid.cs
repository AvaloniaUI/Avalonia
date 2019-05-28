// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Collections;
using Avalonia.Controls.Utils;
using System.Threading;
using JetBrains.Annotations;
using Avalonia.Controls;
using Avalonia;
using System.Collections;
using Avalonia.Utilities;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    public class Grid : Panel
    {
        internal bool CellsStructureDirty = true;
        internal bool SizeToContentU;
        internal bool SizeToContentV;
        internal bool HasStarCellsU;
        internal bool HasStarCellsV;
        internal bool HasGroup3CellsInAutoRows;
        internal bool DefinitionsDirty;
        internal bool IsTrivialGrid => (_definitionsU?.Length <= 1) &&
                                       (_definitionsV?.Length <= 1);
        internal int CellGroup1;
        internal int CellGroup2;
        internal int CellGroup3;
        internal int CellGroup4;

        /// <summary>
        /// Helper for Comparer methods.
        /// </summary>
        /// <returns>
        /// true if one or both of x and y are null, in which case result holds
        /// the relative sort order.
        /// </returns>
        internal static bool CompareNullRefs(object x, object y, out int result)
        {
            result = 2;

            if (x == null)
            {
                if (y == null)
                {
                    result = 0;
                }
                else
                {
                    result = -1;
                }
            }
            else
            {
                if (y == null)
                {
                    result = 1;
                }
            }

            return (result != 2);
        }

        //  temporary array used during layout for various purposes
        //  TempDefinitions.Length == Max(DefinitionsU.Length, DefinitionsV.Length)
        private DefinitionBase[] _tempDefinitions;


        private GridLinesRenderer _gridLinesRenderer;

        // Keeps track of definition indices.
        private int[] _definitionIndices;

        private GridCellCache[] _cellCache;

        // Stores unrounded values and rounding errors during layout rounding.
        private double[] _roundingErrors;
        private ColumnDefinitions _columnDefinitions;
        private RowDefinitions _rowDefinitions;
        private DefinitionBase[] _definitionsU = new DefinitionBase[1] { new ColumnDefinition() };
        private DefinitionBase[] _definitionsV = new DefinitionBase[1] { new RowDefinition() };

        internal SharedSizeScope PrivateSharedSizeScope
        {
            get { return GetPrivateSharedSizeScope(this); }
            set { SetPrivateSharedSizeScope(this, value); }
        }

        // 5 is an arbitrary constant chosen to end the measure loop
        private const int _layoutLoopMaxCount = 5;
        private static readonly LocalDataStoreSlot _tempDefinitionsDataSlot;
        private static readonly IComparer _spanPreferredDistributionOrderComparer;
        private static readonly IComparer _spanMaxDistributionOrderComparer;
        private static readonly IComparer _minRatioComparer;
        private static readonly IComparer _maxRatioComparer;
        private static readonly IComparer _starWeightComparer;

        /// <summary>
        /// Helper accessor to layout time array of definitions.
        /// </summary>
        private DefinitionBase[] TempDefinitions
        {
            get
            {
                int requiredLength = Math.Max(_definitionsU.Length, _definitionsV.Length) * 2;

                if (_tempDefinitions == null
                    || _tempDefinitions.Length < requiredLength)
                {
                    WeakReference tempDefinitionsWeakRef = (WeakReference)Thread.GetData(_tempDefinitionsDataSlot);
                    if (tempDefinitionsWeakRef == null)
                    {
                        _tempDefinitions = new DefinitionBase[requiredLength];
                        Thread.SetData(_tempDefinitionsDataSlot, new WeakReference(_tempDefinitions));
                    }
                    else
                    {
                        _tempDefinitions = (DefinitionBase[])tempDefinitionsWeakRef.Target;
                        if (_tempDefinitions == null
                            || _tempDefinitions.Length < requiredLength)
                        {
                            _tempDefinitions = new DefinitionBase[requiredLength];
                            tempDefinitionsWeakRef.Target = _tempDefinitions;
                        }
                    }
                }
                return (_tempDefinitions);
            }
        }

        /// <summary>
        /// Helper accessor to definition indices.
        /// </summary>
        private int[] DefinitionIndices
        {
            get
            {
                int requiredLength = Math.Max(Math.Max(_definitionsU.Length, _definitionsV.Length), 1) * 2;

                if (_definitionIndices == null || _definitionIndices.Length < requiredLength)
                {
                    _definitionIndices = new int[requiredLength];
                }

                return _definitionIndices;
            }
        }

        /// <summary>
        /// Helper accessor to rounding errors.
        /// </summary>
        private double[] RoundingErrors
        {
            get
            {
                int requiredLength = Math.Max(_definitionsU.Length, _definitionsV.Length);

                if (_roundingErrors == null && requiredLength == 0)
                {
                    _roundingErrors = new double[1];
                }
                else if (_roundingErrors == null || _roundingErrors.Length < requiredLength)
                {
                    _roundingErrors = new double[requiredLength];
                }
                return _roundingErrors;
            }
        }

        static Grid()
        {
            ShowGridLinesProperty.Changed.AddClassHandler<Grid>(OnShowGridLinesPropertyChanged);
            IsSharedSizeScopeProperty.Changed.AddClassHandler<Grid>(IsSharedSizeScopePropertyChanged);
            BoundsProperty.Changed.AddClassHandler<Grid>(BoundsPropertyChanged);

            AffectsParentMeasure<Grid>(ColumnProperty, ColumnSpanProperty, RowProperty, RowSpanProperty);

            _tempDefinitionsDataSlot = Thread.AllocateDataSlot();
            _spanPreferredDistributionOrderComparer = new SpanPreferredDistributionOrderComparer();
            _spanMaxDistributionOrderComparer = new SpanMaxDistributionOrderComparer();
            _minRatioComparer = new MinRatioComparer();
            _maxRatioComparer = new MaxRatioComparer();
            _starWeightComparer = new StarWeightComparer();
        }

        private static void BoundsPropertyChanged(Grid grid, AvaloniaPropertyChangedEventArgs arg2)
        {
            for (int i = 0; i < grid._definitionsU.Length; i++)
                grid._definitionsU[i].OnUserSizePropertyChanged(arg2);
            for (int i = 0; i < grid._definitionsV.Length; i++)
                grid._definitionsV[i].OnUserSizePropertyChanged(arg2);
        }

        private static void IsSharedSizeScopePropertyChanged(Grid grid, AvaloniaPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                grid.PrivateSharedSizeScope = new SharedSizeScope();
            }
            else
            {
                grid.PrivateSharedSizeScope = null;
            }
        }

        /// <summary>
        /// Defines the Column attached property.
        /// </summary>
        public static readonly AttachedProperty<int> ColumnProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, int>(
                "Column",
                validate: ValidateColumn);

        /// <summary>
        /// Defines the ColumnSpan attached property.
        /// </summary>
        public static readonly AttachedProperty<int> ColumnSpanProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, int>("ColumnSpan", 1);

        /// <summary>
        /// Defines the Row attached property.
        /// </summary>
        public static readonly AttachedProperty<int> RowProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, int>(
                "Row",
                validate: ValidateRow);

        /// <summary>
        /// Defines the RowSpan attached property.
        /// </summary>
        public static readonly AttachedProperty<int> RowSpanProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, int>("RowSpan", 1);

        public static readonly AttachedProperty<bool> IsSharedSizeScopeProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, bool>("IsSharedSizeScope", false);

        internal static readonly AttachedProperty<SharedSizeScope> PrivateSharedSizeScopeProperty =
            AvaloniaProperty.RegisterAttached<Grid, Control, SharedSizeScope>("PrivateSharedSizeScope", null, inherits: true);

        /// <summary>
        /// Defines the <see cref="ShowGridLines"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowGridLinesProperty =
            AvaloniaProperty.Register<Grid, bool>(
                nameof(ShowGridLines),
                defaultValue: false);

        /// <summary>
        /// ShowGridLines property.
        /// </summary>
        public bool ShowGridLines
        {
            get { return GetValue(ShowGridLinesProperty); }
            set { SetValue(ShowGridLinesProperty, value); }
        }

        /// <summary>
        /// Gets or sets the columns definitions for the grid.
        /// </summary>
        public ColumnDefinitions ColumnDefinitions
        {
            get
            {
                if (_columnDefinitions == null)
                {
                    ColumnDefinitions = new ColumnDefinitions();
                }

                return _columnDefinitions;
            }
            set
            {
                _columnDefinitions = value;
                _columnDefinitions.TrackItemPropertyChanged(_ => Invalidate());
                DefinitionsDirty = true;

                if (_columnDefinitions.Count > 0)
                    _definitionsU = _columnDefinitions.Cast<DefinitionBase>().ToArray();

                CallEnterParentTree(_definitionsU);

                _columnDefinitions.CollectionChanged += delegate
                {
                    CallExitParentTree(_definitionsU);

                    if (_columnDefinitions.Count == 0)
                    {
                        _definitionsU = new DefinitionBase[1] { new ColumnDefinition() };
                    }
                    else
                    {
                        _definitionsU = _columnDefinitions.Cast<DefinitionBase>().ToArray();
                        DefinitionsDirty = true;
                    }

                    CallEnterParentTree(_definitionsU);

                    Invalidate();
                };
            }
        }

        private void CallEnterParentTree(DefinitionBase[] definitionsU)
        {
            for (int i = 0; i < definitionsU.Length; i++)
                definitionsU[i].OnEnterParentTree(this, i);
        }

        private void CallExitParentTree(DefinitionBase[] definitionsU)
        {
            for (int i = 0; i < definitionsU.Length; i++)
                definitionsU[i].OnExitParentTree();
        }

        /// <summary>
        /// Gets or sets the row definitions for the grid.
        /// </summary>
        public RowDefinitions RowDefinitions
        {
            get
            {
                if (_rowDefinitions == null)
                {
                    RowDefinitions = new RowDefinitions();
                }

                return _rowDefinitions;
            }
            set
            {
                _rowDefinitions = value;
                _rowDefinitions.TrackItemPropertyChanged(_ => Invalidate());

                DefinitionsDirty = true;

                if (_rowDefinitions.Count > 0)
                    _definitionsV = _rowDefinitions.Cast<DefinitionBase>().ToArray();

                _rowDefinitions.CollectionChanged += delegate
                {
                    CallExitParentTree(_definitionsU);

                    if (_rowDefinitions.Count == 0)
                    {
                        _definitionsV = new DefinitionBase[1] { new RowDefinition() };
                    }
                    else
                    {
                        _definitionsV = _rowDefinitions.Cast<DefinitionBase>().ToArray();
                        DefinitionsDirty = true;
                    }
                    CallEnterParentTree(_definitionsU);

                    Invalidate();
                };
            }
        }


        /// <summary>
        /// Gets the value of the Column attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's column.</returns>
        public static int GetColumn(AvaloniaObject element)
        {
            return element.GetValue(ColumnProperty);
        }

        /// <summary>
        /// Gets the value of the ColumnSpan attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's column span.</returns>
        public static int GetColumnSpan(AvaloniaObject element)
        {
            return element.GetValue(ColumnSpanProperty);
        }

        /// <summary>
        /// Gets the value of the Row attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's row.</returns>
        public static int GetRow(AvaloniaObject element)
        {
            return element.GetValue(RowProperty);
        }

        /// <summary>
        /// Gets the value of the RowSpan attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's row span.</returns>
        public static int GetRowSpan(AvaloniaObject element)
        {
            return element.GetValue(RowSpanProperty);
        }

        /// <summary>
        /// Gets the value of the IsSharedSizeScope attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's IsSharedSizeScope value.</returns>
        public static bool GetIsSharedSizeScope(AvaloniaObject element)
        {
            return element.GetValue(IsSharedSizeScopeProperty);
        }

        /// <summary>
        /// Sets the value of the IsSharedSizeScope attached property for a control.
        /// </summary>
        public static void SetIsSharedSizeScope(AvaloniaObject element, bool value)
        {
            element.SetValue(IsSharedSizeScopeProperty, value);
        }

        internal static SharedSizeScope GetPrivateSharedSizeScope(AvaloniaObject element)
        {
            return element.GetValue(PrivateSharedSizeScopeProperty);
        }

        internal static void SetPrivateSharedSizeScope(AvaloniaObject element, SharedSizeScope value)
        {
            element.SetValue(PrivateSharedSizeScopeProperty, value);
        }

        /// <summary>
        /// Sets the value of the Column attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The column value.</param>
        public static void SetColumn(AvaloniaObject element, int value)
        {
            element.SetValue(ColumnProperty, value);
        }

        /// <summary>
        /// Sets the value of the ColumnSpan attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The column span value.</param>
        public static void SetColumnSpan(AvaloniaObject element, int value)
        {
            element.SetValue(ColumnSpanProperty, value);
        }

        /// <summary>
        /// Sets the value of the Row attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The row value.</param>
        public static void SetRow(AvaloniaObject element, int value)
        {
            element.SetValue(RowProperty, value);
        }

        /// <summary>
        /// Sets the value of the RowSpan attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The row span value.</param>
        public static void SetRowSpan(AvaloniaObject element, int value)
        {
            element.SetValue(RowSpanProperty, value);
        }

        /// <summary>
        /// Content measurement.
        /// </summary>
        /// <param name="constraint">Constraint</param>
        /// <returns>Desired size</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            Size gridDesiredSize;

            try
            {
                if (IsTrivialGrid)
                {
                    gridDesiredSize = new Size();

                    for (int i = 0, count = Children.Count; i < count; ++i)
                    {
                        var child = Children[i];
                        if (child != null)
                        {
                            child.Measure(constraint);
                            gridDesiredSize = new Size(
                                Math.Max(gridDesiredSize.Width, child.DesiredSize.Width),
                                Math.Max(gridDesiredSize.Height, child.DesiredSize.Height));
                        }
                    }
                }
                else
                {
                    {
                        bool sizeToContentU = double.IsPositiveInfinity(constraint.Width);
                        bool sizeToContentV = double.IsPositiveInfinity(constraint.Height);

                        // Clear index information and rounding errors
                        if (DefinitionsDirty)
                        {
                            if (_definitionIndices != null)
                            {
                                Array.Clear(_definitionIndices, 0, _definitionIndices.Length);
                                _definitionIndices = null;
                            }

                            if (UseLayoutRounding)
                            {
                                if (_roundingErrors != null)
                                {
                                    Array.Clear(_roundingErrors, 0, _roundingErrors.Length);
                                    _roundingErrors = null;
                                }
                            }

                            DefinitionsDirty = false;
                        }

                        ValidateDefinitionsLayout(_definitionsU, sizeToContentU);
                        ValidateDefinitionsLayout(_definitionsV, sizeToContentV);

                        CellsStructureDirty |= (SizeToContentU != sizeToContentU)
                                            || (SizeToContentV != sizeToContentV);

                        SizeToContentU = sizeToContentU;
                        SizeToContentV = sizeToContentV;
                    }

                    ValidateCells();

                    Debug.Assert(_definitionsU.Length > 0 && _definitionsV.Length > 0);

                    MeasureCellsGroup(CellGroup1, constraint, false, false);

                    {
                        //  after Group1 is measured,  only Group3 may have cells belonging to Auto rows.
                        bool canResolveStarsV = !HasGroup3CellsInAutoRows;

                        if (canResolveStarsV)
                        {
                            if (HasStarCellsV) { ResolveStar(_definitionsV, constraint.Height); }
                            MeasureCellsGroup(CellGroup2, constraint, false, false);
                            if (HasStarCellsU) { ResolveStar(_definitionsU, constraint.Width); }
                            MeasureCellsGroup(CellGroup3, constraint, false, false);
                        }
                        else
                        {
                            //  if at least one cell exists in Group2, it must be measured before
                            //  StarsU can be resolved.
                            bool canResolveStarsU = CellGroup2 > _cellCache.Length;
                            if (canResolveStarsU)
                            {
                                if (HasStarCellsU) { ResolveStar(_definitionsU, constraint.Width); }
                                MeasureCellsGroup(CellGroup3, constraint, false, false);
                                if (HasStarCellsV) { ResolveStar(_definitionsV, constraint.Height); }
                            }
                            else
                            {
                                // This is a revision to the algorithm employed for the cyclic
                                // dependency case described above. We now repeatedly
                                // measure Group3 and Group2 until their sizes settle. We
                                // also use a count heuristic to break a loop in case of one.

                                bool hasDesiredSizeUChanged = false;
                                int cnt = 0;

                                // Cache Group2MinWidths & Group3MinHeights
                                double[] group2MinSizes = CacheMinSizes(CellGroup2, false);
                                double[] group3MinSizes = CacheMinSizes(CellGroup3, true);

                                MeasureCellsGroup(CellGroup2, constraint, false, true);

                                do
                                {
                                    if (hasDesiredSizeUChanged)
                                    {
                                        // Reset cached Group3Heights
                                        ApplyCachedMinSizes(group3MinSizes, true);
                                    }

                                    if (HasStarCellsU) { ResolveStar(_definitionsU, constraint.Width); }
                                    MeasureCellsGroup(CellGroup3, constraint, false, false);

                                    // Reset cached Group2Widths
                                    ApplyCachedMinSizes(group2MinSizes, false);

                                    if (HasStarCellsV) { ResolveStar(_definitionsV, constraint.Height); }
                                    MeasureCellsGroup(CellGroup2, constraint,
                                                      cnt == _layoutLoopMaxCount, false, out hasDesiredSizeUChanged);
                                }
                                while (hasDesiredSizeUChanged && ++cnt <= _layoutLoopMaxCount);
                            }
                        }
                    }

                    MeasureCellsGroup(CellGroup4, constraint, false, false);

                    gridDesiredSize = new Size(
                            CalculateDesiredSize(_definitionsU),
                            CalculateDesiredSize(_definitionsV));
                }
            }
            finally
            {
            }

            return (gridDesiredSize);
        }

        /// <summary>
        /// Content arrangement.
        /// </summary>
        /// <param name="arrangeSize">Arrange size</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            try
            {
                if (IsTrivialGrid)
                {
                    for (int i = 0, count = Children.Count; i < count; ++i)
                    {
                        var child = Children[i];
                        if (child != null)
                        {
                            child.Arrange(new Rect(arrangeSize));
                        }
                    }
                }
                else
                {
                    Debug.Assert(_definitionsU.Length > 0 && _definitionsV.Length > 0);

                    SetFinalSize(_definitionsU, arrangeSize.Width, true);
                    SetFinalSize(_definitionsV, arrangeSize.Height, false);

                    for (int currentCell = 0; currentCell < _cellCache.Length; ++currentCell)
                    {
                        IControl cell = Children[currentCell];
                        if (cell == null)
                        {
                            continue;
                        }

                        int columnIndex = _cellCache[currentCell].ColumnIndex;
                        int rowIndex = _cellCache[currentCell].RowIndex;
                        int columnSpan = _cellCache[currentCell].ColumnSpan;
                        int rowSpan = _cellCache[currentCell].RowSpan;

                        Rect cellRect = new Rect(
                            columnIndex == 0 ? 0.0 : _definitionsU[columnIndex].FinalOffset,
                            rowIndex == 0 ? 0.0 : _definitionsV[rowIndex].FinalOffset,
                            GetFinalSizeForRange(_definitionsU, columnIndex, columnSpan),
                            GetFinalSizeForRange(_definitionsV, rowIndex, rowSpan));

                        cell.Arrange(cellRect);
                    }

                    //  update render bound on grid lines renderer visual
                    var gridLinesRenderer = EnsureGridLinesRenderer();
                    if (gridLinesRenderer != null)
                    {
                        gridLinesRenderer.UpdateRenderBounds(arrangeSize);
                    }
                }
            }
            finally
            {
                SetValid();
            }

            for (var i = 0; i < ColumnDefinitions.Count; i++)
            {
                ColumnDefinitions[i].ActualWidth = GetFinalColumnDefinitionWidth(i);
            }

            for (var i = 0; i < RowDefinitions.Count; i++)
            {
                RowDefinitions[i].ActualHeight = GetFinalRowDefinitionHeight(i);
            }

            return (arrangeSize);
        }

        /// <summary>
        /// Returns final width for a column.
        /// </summary>
        /// <remarks>
        /// Used from public ColumnDefinition ActualWidth. Calculates final width using offset data.
        /// </remarks>
        private double GetFinalColumnDefinitionWidth(int columnIndex)
        {
            double value = 0.0;

            //  actual value calculations require structure to be up-to-date
            if (!DefinitionsDirty)
            {
                value = _definitionsU[(columnIndex + 1) % _definitionsU.Length].FinalOffset;
                if (columnIndex != 0) { value -= _definitionsU[columnIndex].FinalOffset; }
            }
            return (value);
        }

        /// <summary>
        /// Returns final height for a row.
        /// </summary>
        /// <remarks>
        /// Used from public RowDefinition ActualHeight. Calculates final height using offset data.
        /// </remarks>
        private double GetFinalRowDefinitionHeight(int rowIndex)
        {
            double value = 0.0;

            //  actual value calculations require structure to be up-to-date
            if (!DefinitionsDirty)
            {
                value = _definitionsV[(rowIndex + 1) % _definitionsV.Length].FinalOffset;
                if (rowIndex != 0) { value -= _definitionsV[rowIndex].FinalOffset; }
            }
            return (value);
        }

        /// <summary>
        /// Invalidates grid caches and makes the grid dirty for measure.
        /// </summary>
        internal void Invalidate()
        {
            CellsStructureDirty = true;
            InvalidateMeasure();
        }

        /// <summary>
        /// Lays out cells according to rows and columns, and creates lookup grids.
        /// </summary>
        private void ValidateCells()
        {
            if (!CellsStructureDirty) return;

            _cellCache = new GridCellCache[Children.Count];
            CellGroup1 = int.MaxValue;
            CellGroup2 = int.MaxValue;
            CellGroup3 = int.MaxValue;
            CellGroup4 = int.MaxValue;

            bool hasStarCellsU = false;
            bool hasStarCellsV = false;
            bool hasGroup3CellsInAutoRows = false;

            for (int i = _cellCache.Length - 1; i >= 0; --i)
            {
                var child = Children[i] as Control;

                if (child == null)
                {
                    continue;
                }

                var cell = new GridCellCache();

                //  read indices from the corresponding properties
                //      clamp to value < number_of_columns
                //      column >= 0 is guaranteed by property value validation callback
                cell.ColumnIndex = Math.Min(GetColumn(child), _definitionsU.Length - 1);

                //      clamp to value < number_of_rows
                //      row >= 0 is guaranteed by property value validation callback
                cell.RowIndex = Math.Min(GetRow(child), _definitionsV.Length - 1);

                //  read span properties
                //      clamp to not exceed beyond right side of the grid
                //      column_span > 0 is guaranteed by property value validation callback
                cell.ColumnSpan = Math.Min(GetColumnSpan(child), _definitionsU.Length - cell.ColumnIndex);

                //      clamp to not exceed beyond bottom side of the grid
                //      row_span > 0 is guaranteed by property value validation callback
                cell.RowSpan = Math.Min(GetRowSpan(child), _definitionsV.Length - cell.RowIndex);

                Debug.Assert(0 <= cell.ColumnIndex && cell.ColumnIndex < _definitionsU.Length);
                Debug.Assert(0 <= cell.RowIndex && cell.RowIndex < _definitionsV.Length);

                //
                //  calculate and cache length types for the child
                //
                cell.SizeTypeU = GetLengthTypeForRange(_definitionsU, cell.ColumnIndex, cell.ColumnSpan);
                cell.SizeTypeV = GetLengthTypeForRange(_definitionsV, cell.RowIndex, cell.RowSpan);

                hasStarCellsU |= cell.IsStarU;
                hasStarCellsV |= cell.IsStarV;

                //
                //  distribute cells into four groups.
                //
                if (!cell.IsStarV)
                {
                    if (!cell.IsStarU)
                    {
                        cell.Next = CellGroup1;
                        CellGroup1 = i;
                    }
                    else
                    {
                        cell.Next = CellGroup3;
                        CellGroup3 = i;

                        //  remember if this cell belongs to auto row
                        hasGroup3CellsInAutoRows |= cell.IsAutoV;
                    }
                }
                else
                {
                    if (cell.IsAutoU
                        //  note below: if spans through Star column it is NOT Auto
                        && !cell.IsStarU)
                    {
                        cell.Next = CellGroup2;
                        CellGroup2 = i;
                    }
                    else
                    {
                        cell.Next = CellGroup4;
                        CellGroup4 = i;
                    }
                }

                _cellCache[i] = cell;
            }

            HasStarCellsU = hasStarCellsU;
            HasStarCellsV = hasStarCellsV;
            HasGroup3CellsInAutoRows = hasGroup3CellsInAutoRows;

            CellsStructureDirty = false;
        }

        /// <summary>
        /// Validates layout time size type information on given array of definitions.
        /// Sets MinSize and MeasureSizes.
        /// </summary>
        /// <param name="definitions">Array of definitions to update.</param>
        /// <param name="treatStarAsAuto">if "true" then star definitions are treated as Auto.</param>
        private void ValidateDefinitionsLayout(
            DefinitionBase[] definitions,
            bool treatStarAsAuto)
        {
            for (int i = 0; i < definitions.Length; ++i)
            {
                definitions[i].OnBeforeLayout(this);

                double userMinSize = definitions[i].UserMinSize;
                double userMaxSize = definitions[i].UserMaxSize;
                double userSize = 0;

                switch (definitions[i].UserSize.GridUnitType)
                {
                    case (GridUnitType.Pixel):
                        definitions[i].SizeType = LayoutTimeSizeType.Pixel;
                        userSize = definitions[i].UserSize.Value;

                        // this was brought with NewLayout and defeats squishy behavior
                        userMinSize = Math.Max(userMinSize, Math.Min(userSize, userMaxSize));
                        break;
                    case (GridUnitType.Auto):
                        definitions[i].SizeType = LayoutTimeSizeType.Auto;
                        userSize = double.PositiveInfinity;
                        break;
                    case (GridUnitType.Star):
                        if (treatStarAsAuto)
                        {
                            definitions[i].SizeType = LayoutTimeSizeType.Auto;
                            userSize = double.PositiveInfinity;
                        }
                        else
                        {
                            definitions[i].SizeType = LayoutTimeSizeType.Star;
                            userSize = double.PositiveInfinity;
                        }
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }

                definitions[i].UpdateMinSize(userMinSize);
                definitions[i].MeasureSize = Math.Max(userMinSize, Math.Min(userSize, userMaxSize));
            }
        }

        private double[] CacheMinSizes(int cellsHead, bool isRows)
        {
            double[] minSizes = isRows ? new double[_definitionsV.Length]
                                       : new double[_definitionsU.Length];

            for (int j = 0; j < minSizes.Length; j++)
            {
                minSizes[j] = -1;
            }

            int i = cellsHead;
            do
            {
                if (isRows)
                {
                    minSizes[_cellCache[i].RowIndex] = _definitionsV[_cellCache[i].RowIndex].MinSize;
                }
                else
                {
                    minSizes[_cellCache[i].ColumnIndex] = _definitionsU[_cellCache[i].ColumnIndex].MinSize;
                }

                i = _cellCache[i].Next;
            } while (i < _cellCache.Length);

            return minSizes;
        }

        private void ApplyCachedMinSizes(double[] minSizes, bool isRows)
        {
            for (int i = 0; i < minSizes.Length; i++)
            {
                if (MathUtilities.GreaterThanOrClose(minSizes[i], 0))
                {
                    if (isRows)
                    {
                        _definitionsV[i].SetMinSize(minSizes[i]);
                    }
                    else
                    {
                        _definitionsU[i].SetMinSize(minSizes[i]);
                    }
                }
            }
        }

        private void MeasureCellsGroup(
            int cellsHead,
            Size referenceSize,
            bool ignoreDesiredSizeU,
            bool forceInfinityV)
        {
            bool unusedHasDesiredSizeUChanged;
            MeasureCellsGroup(cellsHead, referenceSize, ignoreDesiredSizeU,
                              forceInfinityV, out unusedHasDesiredSizeUChanged);
        }

        /// <summary>
        /// Measures one group of cells.
        /// </summary>
        /// <param name="cellsHead">Head index of the cells chain.</param>
        /// <param name="referenceSize">Reference size for spanned cells
        /// calculations.</param>
        /// <param name="ignoreDesiredSizeU">When "true" cells' desired
        /// width is not registered in columns.</param>
        /// <param name="forceInfinityV">Passed through to MeasureCell.
        /// When "true" cells' desired height is not registered in rows.</param>
        private void MeasureCellsGroup(
            int cellsHead,
            Size referenceSize,
            bool ignoreDesiredSizeU,
            bool forceInfinityV,
            out bool hasDesiredSizeUChanged)
        {
            hasDesiredSizeUChanged = false;

            if (cellsHead >= _cellCache.Length)
            {
                return;
            }

            Hashtable spanStore = null;
            bool ignoreDesiredSizeV = forceInfinityV;

            int i = cellsHead;
            do
            {
                double oldWidth = Children[i].DesiredSize.Width;

                MeasureCell(i, forceInfinityV);

                hasDesiredSizeUChanged |= !MathUtilities.AreClose(oldWidth, Children[i].DesiredSize.Width);

                if (!ignoreDesiredSizeU)
                {
                    if (_cellCache[i].ColumnSpan == 1)
                    {
                        _definitionsU[_cellCache[i].ColumnIndex]
                                    .UpdateMinSize(Math.Min(Children[i].DesiredSize.Width,
                                                   _definitionsU[_cellCache[i].ColumnIndex].UserMaxSize));
                    }
                    else
                    {
                        RegisterSpan(
                            ref spanStore,
                            _cellCache[i].ColumnIndex,
                            _cellCache[i].ColumnSpan,
                            true,
                            Children[i].DesiredSize.Width);
                    }
                }

                if (!ignoreDesiredSizeV)
                {
                    if (_cellCache[i].RowSpan == 1)
                    {
                        _definitionsV[_cellCache[i].RowIndex]
                                    .UpdateMinSize(Math.Min(Children[i].DesiredSize.Height,
                                                   _definitionsV[_cellCache[i].RowIndex].UserMaxSize));
                    }
                    else
                    {
                        RegisterSpan(
                            ref spanStore,
                            _cellCache[i].RowIndex,
                            _cellCache[i].RowSpan,
                            false,
                            Children[i].DesiredSize.Height);
                    }
                }

                i = _cellCache[i].Next;
            } while (i < _cellCache.Length);

            if (spanStore != null)
            {
                foreach (DictionaryEntry e in spanStore)
                {
                    GridSpanKey key = (GridSpanKey)e.Key;
                    double requestedSize = (double)e.Value;

                    EnsureMinSizeInDefinitionRange(
                        key.U ? _definitionsU : _definitionsV,
                        key.Start,
                        key.Count,
                        requestedSize,
                        key.U ? referenceSize.Width : referenceSize.Height);
                }
            }
        }

        /// <summary>
        /// Helper method to register a span information for delayed processing.
        /// </summary>
        /// <param name="store">Reference to a hashtable object used as storage.</param>
        /// <param name="start">Span starting index.</param>
        /// <param name="count">Span count.</param>
        /// <param name="u"><c>true</c> if this is a column span. <c>false</c> if this is a row span.</param>
        /// <param name="value">Value to store. If an entry already exists the biggest value is stored.</param>
        private static void RegisterSpan(
            ref Hashtable store,
            int start,
            int count,
            bool u,
            double value)
        {
            if (store == null)
            {
                store = new Hashtable();
            }

            GridSpanKey key = new GridSpanKey(start, count, u);
            object o = store[key];

            if (o == null
                || value > (double)o)
            {
                store[key] = value;
            }
        }

        /// <summary>
        /// Takes care of measuring a single cell.
        /// </summary>
        /// <param name="cell">Index of the cell to measure.</param>
        /// <param name="forceInfinityV">If "true" then cell is always
        /// calculated to infinite height.</param>
        private void MeasureCell(
            int cell,
            bool forceInfinityV)
        {
            double cellMeasureWidth;
            double cellMeasureHeight;

            if (_cellCache[cell].IsAutoU
                && !_cellCache[cell].IsStarU)
            {
                //  if cell belongs to at least one Auto column and not a single Star column
                //  then it should be calculated "to content", thus it is possible to "shortcut"
                //  calculations and simply assign PositiveInfinity here.
                cellMeasureWidth = double.PositiveInfinity;
            }
            else
            {
                //  otherwise...
                cellMeasureWidth = GetMeasureSizeForRange(
                                        _definitionsU,
                                        _cellCache[cell].ColumnIndex,
                                        _cellCache[cell].ColumnSpan);
            }

            if (forceInfinityV)
            {
                cellMeasureHeight = double.PositiveInfinity;
            }
            else if (_cellCache[cell].IsAutoV
                    && !_cellCache[cell].IsStarV)
            {
                //  if cell belongs to at least one Auto row and not a single Star row
                //  then it should be calculated "to content", thus it is possible to "shortcut"
                //  calculations and simply assign PositiveInfinity here.
                cellMeasureHeight = double.PositiveInfinity;
            }
            else
            {
                cellMeasureHeight = GetMeasureSizeForRange(
                                        _definitionsV,
                                        _cellCache[cell].RowIndex,
                                        _cellCache[cell].RowSpan);
            }

            var child = Children[cell];

            if (child != null)
            {
                Size childConstraint = new Size(cellMeasureWidth, cellMeasureHeight);
                child.Measure(childConstraint);
            }
        }

        /// <summary>
        /// Calculates one dimensional measure size for given definitions' range.
        /// </summary>
        /// <param name="definitions">Source array of definitions to read values from.</param>
        /// <param name="start">Starting index of the range.</param>
        /// <param name="count">Number of definitions included in the range.</param>
        /// <returns>Calculated measure size.</returns>
        /// <remarks>
        /// For "Auto" definitions MinWidth is used in place of PreferredSize.
        /// </remarks>
        private double GetMeasureSizeForRange(
            DefinitionBase[] definitions,
            int start,
            int count)
        {
            Debug.Assert(0 < count && 0 <= start && (start + count) <= definitions.Length);

            double measureSize = 0;
            int i = start + count - 1;

            do
            {
                measureSize += (definitions[i].SizeType == LayoutTimeSizeType.Auto)
                    ? definitions[i].MinSize
                    : definitions[i].MeasureSize;
            } while (--i >= start);

            return (measureSize);
        }

        /// <summary>
        /// Accumulates length type information for given definition's range.
        /// </summary>
        /// <param name="definitions">Source array of definitions to read values from.</param>
        /// <param name="start">Starting index of the range.</param>
        /// <param name="count">Number of definitions included in the range.</param>
        /// <returns>Length type for given range.</returns>
        private LayoutTimeSizeType GetLengthTypeForRange(
            DefinitionBase[] definitions,
            int start,
            int count)
        {
            Debug.Assert(0 < count && 0 <= start && (start + count) <= definitions.Length);

            LayoutTimeSizeType lengthType = LayoutTimeSizeType.None;
            int i = start + count - 1;

            do
            {
                lengthType |= definitions[i].SizeType;
            } while (--i >= start);

            return (lengthType);
        }

        /// <summary>
        /// Distributes min size back to definition array's range.
        /// </summary>
        /// <param name="start">Start of the range.</param>
        /// <param name="count">Number of items in the range.</param>
        /// <param name="requestedSize">Minimum size that should "fit" into the definitions range.</param>
        /// <param name="definitions">Definition array receiving distribution.</param>
        /// <param name="percentReferenceSize">Size used to resolve percentages.</param>
        private void EnsureMinSizeInDefinitionRange(
            DefinitionBase[] definitions,
            int start,
            int count,
            double requestedSize,
            double percentReferenceSize)
        {
            Debug.Assert(1 < count && 0 <= start && (start + count) <= definitions.Length);

            //  avoid processing when asked to distribute "0"
            if (!MathUtilities.IsZero(requestedSize))
            {
                DefinitionBase[] tempDefinitions = TempDefinitions; //  temp array used to remember definitions for sorting
                int end = start + count;
                int autoDefinitionsCount = 0;
                double rangeMinSize = 0;
                double rangePreferredSize = 0;
                double rangeMaxSize = 0;
                double maxMaxSize = 0;                              //  maximum of maximum sizes

                //  first accumulate the necessary information:
                //  a) sum up the sizes in the range;
                //  b) count the number of auto definitions in the range;
                //  c) initialize temp array
                //  d) cache the maximum size into SizeCache
                //  e) accumulate max of max sizes
                for (int i = start; i < end; ++i)
                {
                    double minSize = definitions[i].MinSize;
                    double preferredSize = definitions[i].PreferredSize;
                    double maxSize = Math.Max(definitions[i].UserMaxSize, minSize);

                    rangeMinSize += minSize;
                    rangePreferredSize += preferredSize;
                    rangeMaxSize += maxSize;

                    definitions[i].SizeCache = maxSize;

                    //  sanity check: no matter what, but min size must always be the smaller;
                    //  max size must be the biggest; and preferred should be in between
                    Debug.Assert(minSize <= preferredSize
                                && preferredSize <= maxSize
                                && rangeMinSize <= rangePreferredSize
                                && rangePreferredSize <= rangeMaxSize);

                    if (maxMaxSize < maxSize) maxMaxSize = maxSize;
                    if (definitions[i].UserSize.IsAuto) autoDefinitionsCount++;
                    tempDefinitions[i - start] = definitions[i];
                }

                //  avoid processing if the range already big enough
                if (requestedSize > rangeMinSize)
                {
                    if (requestedSize <= rangePreferredSize)
                    {
                        //
                        //  requestedSize fits into preferred size of the range.
                        //  distribute according to the following logic:
                        //  * do not distribute into auto definitions - they should continue to stay "tight";
                        //  * for all non-auto definitions distribute to equi-size min sizes, without exceeding preferred size.
                        //
                        //  in order to achieve that, definitions are sorted in a way that all auto definitions
                        //  are first, then definitions follow ascending order with PreferredSize as the key of sorting.
                        //
                        double sizeToDistribute;
                        int i;

                        Array.Sort(tempDefinitions, 0, count, _spanPreferredDistributionOrderComparer);
                        for (i = 0, sizeToDistribute = requestedSize; i < autoDefinitionsCount; ++i)
                        {
                            //  sanity check: only auto definitions allowed in this loop
                            Debug.Assert(tempDefinitions[i].UserSize.IsAuto);

                            //  adjust sizeToDistribute value by subtracting auto definition min size
                            sizeToDistribute -= (tempDefinitions[i].MinSize);
                        }

                        for (; i < count; ++i)
                        {
                            //  sanity check: no auto definitions allowed in this loop
                            Debug.Assert(!tempDefinitions[i].UserSize.IsAuto);

                            double newMinSize = Math.Min(sizeToDistribute / (count - i), tempDefinitions[i].PreferredSize);
                            if (newMinSize > tempDefinitions[i].MinSize) { tempDefinitions[i].UpdateMinSize(newMinSize); }
                            sizeToDistribute -= newMinSize;
                        }

                        //  sanity check: requested size must all be distributed
                        Debug.Assert(MathUtilities.IsZero(sizeToDistribute));
                    }
                    else if (requestedSize <= rangeMaxSize)
                    {
                        //
                        //  requestedSize bigger than preferred size, but fit into max size of the range.
                        //  distribute according to the following logic:
                        //  * do not distribute into auto definitions, if possible - they should continue to stay "tight";
                        //  * for all non-auto definitions distribute to euqi-size min sizes, without exceeding max size.
                        //
                        //  in order to achieve that, definitions are sorted in a way that all non-auto definitions
                        //  are last, then definitions follow ascending order with MaxSize as the key of sorting.
                        //
                        double sizeToDistribute;
                        int i;

                        Array.Sort(tempDefinitions, 0, count, _spanMaxDistributionOrderComparer);
                        for (i = 0, sizeToDistribute = requestedSize - rangePreferredSize; i < count - autoDefinitionsCount; ++i)
                        {
                            //  sanity check: no auto definitions allowed in this loop
                            Debug.Assert(!tempDefinitions[i].UserSize.IsAuto);

                            double preferredSize = tempDefinitions[i].PreferredSize;
                            double newMinSize = preferredSize + sizeToDistribute / (count - autoDefinitionsCount - i);
                            tempDefinitions[i].UpdateMinSize(Math.Min(newMinSize, tempDefinitions[i].SizeCache));
                            sizeToDistribute -= (tempDefinitions[i].MinSize - preferredSize);
                        }

                        for (; i < count; ++i)
                        {
                            //  sanity check: only auto definitions allowed in this loop
                            Debug.Assert(tempDefinitions[i].UserSize.IsAuto);

                            double preferredSize = tempDefinitions[i].MinSize;
                            double newMinSize = preferredSize + sizeToDistribute / (count - i);
                            tempDefinitions[i].UpdateMinSize(Math.Min(newMinSize, tempDefinitions[i].SizeCache));
                            sizeToDistribute -= (tempDefinitions[i].MinSize - preferredSize);
                        }

                        //  sanity check: requested size must all be distributed
                        Debug.Assert(MathUtilities.IsZero(sizeToDistribute));
                    }
                    else
                    {
                        //
                        //  requestedSize bigger than max size of the range.
                        //  distribute according to the following logic:
                        //  * for all definitions distribute to equi-size min sizes.
                        //
                        double equalSize = requestedSize / count;

                        if (equalSize < maxMaxSize
                            && !MathUtilities.AreClose(equalSize, maxMaxSize))
                        {
                            //  equi-size is less than maximum of maxSizes.
                            //  in this case distribute so that smaller definitions grow faster than
                            //  bigger ones.
                            double totalRemainingSize = maxMaxSize * count - rangeMaxSize;
                            double sizeToDistribute = requestedSize - rangeMaxSize;

                            //  sanity check: totalRemainingSize and sizeToDistribute must be real positive numbers
                            Debug.Assert(!double.IsInfinity(totalRemainingSize)
                                        && !double.IsNaN(totalRemainingSize)
                                        && totalRemainingSize > 0
                                        && !double.IsInfinity(sizeToDistribute)
                                        && !double.IsNaN(sizeToDistribute)
                                        && sizeToDistribute > 0);

                            for (int i = 0; i < count; ++i)
                            {
                                double deltaSize = (maxMaxSize - tempDefinitions[i].SizeCache) * sizeToDistribute / totalRemainingSize;
                                tempDefinitions[i].UpdateMinSize(tempDefinitions[i].SizeCache + deltaSize);
                            }
                        }
                        else
                        {
                            //
                            //  equi-size is greater or equal to maximum of max sizes.
                            //  all definitions receive equalSize as their mim sizes.
                            //
                            for (int i = 0; i < count; ++i)
                            {
                                tempDefinitions[i].UpdateMinSize(equalSize);
                            }
                        }
                    }
                }
            }
        }

        // new implementation as of 4.7.  Several improvements:
        // 1. Allocate to *-defs hitting their min or max constraints, before allocating
        //      to other *-defs.  A def that hits its min uses more space than its
        //      proportional share, reducing the space available to everyone else.
        //      The legacy algorithm deducted this space only from defs processed
        //      after the min;  the new algorithm deducts it proportionally from all
        //      defs.   This avoids the "*-defs exceed available space" problem,
        //      and other related problems where *-defs don't receive proportional
        //      allocations even though no constraints are preventing it.
        // 2. When multiple defs hit min or max, resolve the one with maximum
        //      discrepancy (defined below).   This avoids discontinuities - small
        //      change in available space resulting in large change to one def's allocation.
        // 3. Correct handling of large *-values, including Infinity.

        /// <summary>
        /// Resolves Star's for given array of definitions.
        /// </summary>
        /// <param name="definitions">Array of definitions to resolve stars.</param>
        /// <param name="availableSize">All available size.</param>
        /// <remarks>
        /// Must initialize LayoutSize for all Star entries in given array of definitions.
        /// </remarks>
        private void ResolveStar(
            DefinitionBase[] definitions,
            double availableSize)
        {
            int defCount = definitions.Length;
            DefinitionBase[] tempDefinitions = TempDefinitions;
            int minCount = 0, maxCount = 0;
            double takenSize = 0;
            double totalStarWeight = 0.0;
            int starCount = 0;      // number of unresolved *-definitions
            double scale = 1.0;     // scale factor applied to each *-weight;  negative means "Infinity is present"

            // Phase 1.  Determine the maximum *-weight and prepare to adjust *-weights
            double maxStar = 0.0;
            for (int i = 0; i < defCount; ++i)
            {
                DefinitionBase def = definitions[i];

                if (def.SizeType == LayoutTimeSizeType.Star)
                {
                    ++starCount;
                    def.MeasureSize = 1.0;  // meaning "not yet resolved in phase 3"
                    if (def.UserSize.Value > maxStar)
                    {
                        maxStar = def.UserSize.Value;
                    }
                }
            }

            if (double.IsPositiveInfinity(maxStar))
            {
                // negative scale means one or more of the weights was Infinity
                scale = -1.0;
            }
            else if (starCount > 0)
            {
                // if maxStar * starCount > double.Max, summing all the weights could cause
                // floating-point overflow.  To avoid that, scale the weights by a factor to keep
                // the sum within limits.  Choose a power of 2, to preserve precision.
                double power = Math.Floor(Math.Log(double.MaxValue / maxStar / starCount, 2.0));
                if (power < 0.0)
                {
                    scale = Math.Pow(2.0, power - 4.0); // -4 is just for paranoia
                }
            }

            // normally Phases 2 and 3 execute only once.  But certain unusual combinations of weights
            // and constraints can defeat the algorithm, in which case we repeat Phases 2 and 3.
            // More explanation below...
            for (bool runPhase2and3 = true; runPhase2and3;)
            {
                // Phase 2.   Compute total *-weight W and available space S.
                // For *-items that have Min or Max constraints, compute the ratios used to decide
                // whether proportional space is too big or too small and add the item to the
                // corresponding list.  (The "min" list is in the first half of tempDefinitions,
                // the "max" list in the second half.  TempDefinitions has capacity at least
                // 2*defCount, so there's room for both lists.)
                totalStarWeight = 0.0;
                takenSize = 0.0;
                minCount = maxCount = 0;

                for (int i = 0; i < defCount; ++i)
                {
                    DefinitionBase def = definitions[i];

                    switch (def.SizeType)
                    {
                        case (LayoutTimeSizeType.Auto):
                            takenSize += definitions[i].MinSize;
                            break;
                        case (LayoutTimeSizeType.Pixel):
                            takenSize += def.MeasureSize;
                            break;
                        case (LayoutTimeSizeType.Star):
                            if (def.MeasureSize < 0.0)
                            {
                                takenSize += -def.MeasureSize;  // already resolved
                            }
                            else
                            {
                                double starWeight = StarWeight(def, scale);
                                totalStarWeight += starWeight;

                                if (def.MinSize > 0.0)
                                {
                                    // store ratio w/min in MeasureSize (for now)
                                    tempDefinitions[minCount++] = def;
                                    def.MeasureSize = starWeight / def.MinSize;
                                }

                                double effectiveMaxSize = Math.Max(def.MinSize, def.UserMaxSize);
                                if (!double.IsPositiveInfinity(effectiveMaxSize))
                                {
                                    // store ratio w/max in SizeCache (for now)
                                    tempDefinitions[defCount + maxCount++] = def;
                                    def.SizeCache = starWeight / effectiveMaxSize;
                                }
                            }
                            break;
                    }
                }

                // Phase 3.  Resolve *-items whose proportional sizes are too big or too small.
                int minCountPhase2 = minCount, maxCountPhase2 = maxCount;
                double takenStarWeight = 0.0;
                double remainingAvailableSize = availableSize - takenSize;
                double remainingStarWeight = totalStarWeight - takenStarWeight;
                Array.Sort(tempDefinitions, 0, minCount, _minRatioComparer);
                Array.Sort(tempDefinitions, defCount, maxCount, _maxRatioComparer);

                while (minCount + maxCount > 0 && remainingAvailableSize > 0.0)
                {
                    // the calculation
                    //            remainingStarWeight = totalStarWeight - takenStarWeight
                    // is subject to catastrophic cancellation if the two terms are nearly equal,
                    // which leads to meaningless results.   Check for that, and recompute from
                    // the remaining definitions.   [This leads to quadratic behavior in really
                    // pathological cases - but they'd never arise in practice.]
                    const double starFactor = 1.0 / 256.0;      // lose more than 8 bits of precision -> recalculate
                    if (remainingStarWeight < totalStarWeight * starFactor)
                    {
                        takenStarWeight = 0.0;
                        totalStarWeight = 0.0;

                        for (int i = 0; i < defCount; ++i)
                        {
                            DefinitionBase def = definitions[i];
                            if (def.SizeType == LayoutTimeSizeType.Star && def.MeasureSize > 0.0)
                            {
                                totalStarWeight += StarWeight(def, scale);
                            }
                        }

                        remainingStarWeight = totalStarWeight - takenStarWeight;
                    }

                    double minRatio = (minCount > 0) ? tempDefinitions[minCount - 1].MeasureSize : double.PositiveInfinity;
                    double maxRatio = (maxCount > 0) ? tempDefinitions[defCount + maxCount - 1].SizeCache : -1.0;

                    // choose the def with larger ratio to the current proportion ("max discrepancy")
                    double proportion = remainingStarWeight / remainingAvailableSize;
                    bool? chooseMin = Choose(minRatio, maxRatio, proportion);

                    // if no def was chosen, advance to phase 4;  the current proportion doesn't
                    // conflict with any min or max values.
                    if (!(chooseMin.HasValue))
                    {
                        break;
                    }

                    // get the chosen definition and its resolved size
                    DefinitionBase resolvedDef;
                    double resolvedSize;
                    if (chooseMin == true)
                    {
                        resolvedDef = tempDefinitions[minCount - 1];
                        resolvedSize = resolvedDef.MinSize;
                        --minCount;
                    }
                    else
                    {
                        resolvedDef = tempDefinitions[defCount + maxCount - 1];
                        resolvedSize = Math.Max(resolvedDef.MinSize, resolvedDef.UserMaxSize);
                        --maxCount;
                    }

                    // resolve the chosen def, deduct its contributions from W and S.
                    // Defs resolved in phase 3 are marked by storing the negative of their resolved
                    // size in MeasureSize, to distinguish them from a pending def.
                    takenSize += resolvedSize;
                    resolvedDef.MeasureSize = -resolvedSize;
                    takenStarWeight += StarWeight(resolvedDef, scale);
                    --starCount;

                    remainingAvailableSize = availableSize - takenSize;
                    remainingStarWeight = totalStarWeight - takenStarWeight;

                    // advance to the next candidate defs, removing ones that have been resolved.
                    // Both counts are advanced, as a def might appear in both lists.
                    while (minCount > 0 && tempDefinitions[minCount - 1].MeasureSize < 0.0)
                    {
                        --minCount;
                        tempDefinitions[minCount] = null;
                    }
                    while (maxCount > 0 && tempDefinitions[defCount + maxCount - 1].MeasureSize < 0.0)
                    {
                        --maxCount;
                        tempDefinitions[defCount + maxCount] = null;
                    }
                }

                // decide whether to run Phase2 and Phase3 again.  There are 3 cases:
                // 1. There is space available, and *-defs remaining.  This is the
                //      normal case - move on to Phase 4 to allocate the remaining
                //      space proportionally to the remaining *-defs.
                // 2. There is space available, but no *-defs.  This implies at least one
                //      def was resolved as 'max', taking less space than its proportion.
                //      If there are also 'min' defs, reconsider them - we can give
                //      them more space.   If not, all the *-defs are 'max', so there's
                //      no way to use all the available space.
                // 3. We allocated too much space.   This implies at least one def was
                //      resolved as 'min'.  If there are also 'max' defs, reconsider
                //      them, otherwise the over-allocation is an inevitable consequence
                //      of the given min constraints.
                // Note that if we return to Phase2, at least one *-def will have been
                // resolved.  This guarantees we don't run Phase2+3 infinitely often.
                runPhase2and3 = false;
                if (starCount == 0 && takenSize < availableSize)
                {
                    // if no *-defs remain and we haven't allocated all the space, reconsider the defs
                    // resolved as 'min'.   Their allocation can be increased to make up the gap.
                    for (int i = minCount; i < minCountPhase2; ++i)
                    {
                        DefinitionBase def = tempDefinitions[i];
                        if (def != null)
                        {
                            def.MeasureSize = 1.0;      // mark as 'not yet resolved'
                            ++starCount;
                            runPhase2and3 = true;       // found a candidate, so re-run Phases 2 and 3
                        }
                    }
                }

                if (takenSize > availableSize)
                {
                    // if we've allocated too much space, reconsider the defs
                    // resolved as 'max'.   Their allocation can be decreased to make up the gap.
                    for (int i = maxCount; i < maxCountPhase2; ++i)
                    {
                        DefinitionBase def = tempDefinitions[defCount + i];
                        if (def != null)
                        {
                            def.MeasureSize = 1.0;      // mark as 'not yet resolved'
                            ++starCount;
                            runPhase2and3 = true;    // found a candidate, so re-run Phases 2 and 3
                        }
                    }
                }
            }

            // Phase 4.  Resolve the remaining defs proportionally.
            starCount = 0;
            for (int i = 0; i < defCount; ++i)
            {
                DefinitionBase def = definitions[i];

                if (def.SizeType == LayoutTimeSizeType.Star)
                {
                    if (def.MeasureSize < 0.0)
                    {
                        // this def was resolved in phase 3 - fix up its measure size
                        def.MeasureSize = -def.MeasureSize;
                    }
                    else
                    {
                        // this def needs resolution, add it to the list, sorted by *-weight
                        tempDefinitions[starCount++] = def;
                        def.MeasureSize = StarWeight(def, scale);
                    }
                }
            }

            if (starCount > 0)
            {
                Array.Sort(tempDefinitions, 0, starCount, _starWeightComparer);

                // compute the partial sums of *-weight, in increasing order of weight
                // for minimal loss of precision.
                totalStarWeight = 0.0;
                for (int i = 0; i < starCount; ++i)
                {
                    DefinitionBase def = tempDefinitions[i];
                    totalStarWeight += def.MeasureSize;
                    def.SizeCache = totalStarWeight;
                }

                // resolve the defs, in decreasing order of weight
                for (int i = starCount - 1; i >= 0; --i)
                {
                    DefinitionBase def = tempDefinitions[i];
                    double resolvedSize = (def.MeasureSize > 0.0) ? Math.Max(availableSize - takenSize, 0.0) * (def.MeasureSize / def.SizeCache) : 0.0;

                    // min and max should have no effect by now, but just in case...
                    resolvedSize = Math.Min(resolvedSize, def.UserMaxSize);
                    resolvedSize = Math.Max(def.MinSize, resolvedSize);

                    def.MeasureSize = resolvedSize;
                    takenSize += resolvedSize;
                }
            }
        }

        /// <summary>
        /// Calculates desired size for given array of definitions.
        /// </summary>
        /// <param name="definitions">Array of definitions to use for calculations.</param>
        /// <returns>Desired size.</returns>
        private double CalculateDesiredSize(
                DefinitionBase[] definitions)
        {
            double desiredSize = 0;

            for (int i = 0; i < definitions.Length; ++i)
            {
                desiredSize += definitions[i].MinSize;
            }

            return (desiredSize);
        }

        /// <summary>
        /// Calculates and sets final size for all definitions in the given array.
        /// </summary>
        /// <param name="definitions">Array of definitions to process.</param>
        /// <param name="finalSize">Final size to lay out to.</param>
        /// <param name="rows">True if sizing row definitions, false for columns</param>
        private void SetFinalSize(
            DefinitionBase[] definitions,
            double finalSize,
            bool columns)
        {
            int defCount = definitions.Length;
            int[] definitionIndices = DefinitionIndices;
            int minCount = 0, maxCount = 0;
            double takenSize = 0.0;
            double totalStarWeight = 0.0;
            int starCount = 0;      // number of unresolved *-definitions
            double scale = 1.0;   // scale factor applied to each *-weight;  negative means "Infinity is present"

            // Phase 1.  Determine the maximum *-weight and prepare to adjust *-weights
            double maxStar = 0.0;
            for (int i = 0; i < defCount; ++i)
            {
                DefinitionBase def = definitions[i];

                if (def.UserSize.IsStar)
                {
                    ++starCount;
                    def.MeasureSize = 1.0;  // meaning "not yet resolved in phase 3"
                    if (def.UserSize.Value > maxStar)
                    {
                        maxStar = def.UserSize.Value;
                    }
                }
            }

            if (double.IsPositiveInfinity(maxStar))
            {
                // negative scale means one or more of the weights was Infinity
                scale = -1.0;
            }
            else if (starCount > 0)
            {
                // if maxStar * starCount > double.Max, summing all the weights could cause
                // floating-point overflow.  To avoid that, scale the weights by a factor to keep
                // the sum within limits.  Choose a power of 2, to preserve precision.
                double power = Math.Floor(Math.Log(double.MaxValue / maxStar / starCount, 2.0));
                if (power < 0.0)
                {
                    scale = Math.Pow(2.0, power - 4.0); // -4 is just for paranoia
                }
            }


            // normally Phases 2 and 3 execute only once.  But certain unusual combinations of weights
            // and constraints can defeat the algorithm, in which case we repeat Phases 2 and 3.
            // More explanation below...
            for (bool runPhase2and3 = true; runPhase2and3;)
            {
                // Phase 2.   Compute total *-weight W and available space S.
                // For *-items that have Min or Max constraints, compute the ratios used to decide
                // whether proportional space is too big or too small and add the item to the
                // corresponding list.  (The "min" list is in the first half of definitionIndices,
                // the "max" list in the second half.  DefinitionIndices has capacity at least
                // 2*defCount, so there's room for both lists.)
                totalStarWeight = 0.0;
                takenSize = 0.0;
                minCount = maxCount = 0;

                for (int i = 0; i < defCount; ++i)
                {
                    DefinitionBase def = definitions[i];

                    if (def.UserSize.IsStar)
                    {
                        Debug.Assert(!def.IsShared, "*-defs cannot be shared");

                        if (def.MeasureSize < 0.0)
                        {
                            takenSize += -def.MeasureSize;  // already resolved
                        }
                        else
                        {
                            double starWeight = StarWeight(def, scale);
                            totalStarWeight += starWeight;

                            if (def.MinSizeForArrange > 0.0)
                            {
                                // store ratio w/min in MeasureSize (for now)
                                definitionIndices[minCount++] = i;
                                def.MeasureSize = starWeight / def.MinSizeForArrange;
                            }

                            double effectiveMaxSize = Math.Max(def.MinSizeForArrange, def.UserMaxSize);
                            if (!double.IsPositiveInfinity(effectiveMaxSize))
                            {
                                // store ratio w/max in SizeCache (for now)
                                definitionIndices[defCount + maxCount++] = i;
                                def.SizeCache = starWeight / effectiveMaxSize;
                            }
                        }
                    }
                    else
                    {
                        double userSize = 0;

                        switch (def.UserSize.GridUnitType)
                        {
                            case (GridUnitType.Pixel):
                                userSize = def.UserSize.Value;
                                break;

                            case (GridUnitType.Auto):
                                userSize = def.MinSizeForArrange;
                                break;
                        }

                        double userMaxSize;

                        if (def.IsShared)
                        {
                            //  overriding userMaxSize effectively prevents squishy-ness.
                            //  this is a "solution" to avoid shared definitions from been sized to
                            //  different final size at arrange time, if / when different grids receive
                            //  different final sizes.
                            userMaxSize = userSize;
                        }
                        else
                        {
                            userMaxSize = def.UserMaxSize;
                        }

                        def.SizeCache = Math.Max(def.MinSizeForArrange, Math.Min(userSize, userMaxSize));
                        takenSize += def.SizeCache;
                    }
                }

                // Phase 3.  Resolve *-items whose proportional sizes are too big or too small.
                int minCountPhase2 = minCount, maxCountPhase2 = maxCount;
                double takenStarWeight = 0.0;
                double remainingAvailableSize = finalSize - takenSize;
                double remainingStarWeight = totalStarWeight - takenStarWeight;

                MinRatioIndexComparer minRatioIndexComparer = new MinRatioIndexComparer(definitions);
                Array.Sort(definitionIndices, 0, minCount, minRatioIndexComparer);
                MaxRatioIndexComparer maxRatioIndexComparer = new MaxRatioIndexComparer(definitions);
                Array.Sort(definitionIndices, defCount, maxCount, maxRatioIndexComparer);

                while (minCount + maxCount > 0 && remainingAvailableSize > 0.0)
                {
                    // the calculation
                    //            remainingStarWeight = totalStarWeight - takenStarWeight
                    // is subject to catastrophic cancellation if the two terms are nearly equal,
                    // which leads to meaningless results.   Check for that, and recompute from
                    // the remaining definitions.   [This leads to quadratic behavior in really
                    // pathological cases - but they'd never arise in practice.]
                    const double starFactor = 1.0 / 256.0;      // lose more than 8 bits of precision -> recalculate
                    if (remainingStarWeight < totalStarWeight * starFactor)
                    {
                        takenStarWeight = 0.0;
                        totalStarWeight = 0.0;

                        for (int i = 0; i < defCount; ++i)
                        {
                            DefinitionBase def = definitions[i];
                            if (def.UserSize.IsStar && def.MeasureSize > 0.0)
                            {
                                totalStarWeight += StarWeight(def, scale);
                            }
                        }

                        remainingStarWeight = totalStarWeight - takenStarWeight;
                    }

                    double minRatio = (minCount > 0) ? definitions[definitionIndices[minCount - 1]].MeasureSize : double.PositiveInfinity;
                    double maxRatio = (maxCount > 0) ? definitions[definitionIndices[defCount + maxCount - 1]].SizeCache : -1.0;

                    // choose the def with larger ratio to the current proportion ("max discrepancy")
                    double proportion = remainingStarWeight / remainingAvailableSize;
                    bool? chooseMin = Choose(minRatio, maxRatio, proportion);

                    // if no def was chosen, advance to phase 4;  the current proportion doesn't
                    // conflict with any min or max values.
                    if (!(chooseMin.HasValue))
                    {
                        break;
                    }

                    // get the chosen definition and its resolved size
                    int resolvedIndex;
                    DefinitionBase resolvedDef;
                    double resolvedSize;
                    if (chooseMin == true)
                    {
                        resolvedIndex = definitionIndices[minCount - 1];
                        resolvedDef = definitions[resolvedIndex];
                        resolvedSize = resolvedDef.MinSizeForArrange;
                        --minCount;
                    }
                    else
                    {
                        resolvedIndex = definitionIndices[defCount + maxCount - 1];
                        resolvedDef = definitions[resolvedIndex];
                        resolvedSize = Math.Max(resolvedDef.MinSizeForArrange, resolvedDef.UserMaxSize);
                        --maxCount;
                    }

                    // resolve the chosen def, deduct its contributions from W and S.
                    // Defs resolved in phase 3 are marked by storing the negative of their resolved
                    // size in MeasureSize, to distinguish them from a pending def.
                    takenSize += resolvedSize;
                    resolvedDef.MeasureSize = -resolvedSize;
                    takenStarWeight += StarWeight(resolvedDef, scale);
                    --starCount;

                    remainingAvailableSize = finalSize - takenSize;
                    remainingStarWeight = totalStarWeight - takenStarWeight;

                    // advance to the next candidate defs, removing ones that have been resolved.
                    // Both counts are advanced, as a def might appear in both lists.
                    while (minCount > 0 && definitions[definitionIndices[minCount - 1]].MeasureSize < 0.0)
                    {
                        --minCount;
                        definitionIndices[minCount] = -1;
                    }
                    while (maxCount > 0 && definitions[definitionIndices[defCount + maxCount - 1]].MeasureSize < 0.0)
                    {
                        --maxCount;
                        definitionIndices[defCount + maxCount] = -1;
                    }
                }

                // decide whether to run Phase2 and Phase3 again.  There are 3 cases:
                // 1. There is space available, and *-defs remaining.  This is the
                //      normal case - move on to Phase 4 to allocate the remaining
                //      space proportionally to the remaining *-defs.
                // 2. There is space available, but no *-defs.  This implies at least one
                //      def was resolved as 'max', taking less space than its proportion.
                //      If there are also 'min' defs, reconsider them - we can give
                //      them more space.   If not, all the *-defs are 'max', so there's
                //      no way to use all the available space.
                // 3. We allocated too much space.   This implies at least one def was
                //      resolved as 'min'.  If there are also 'max' defs, reconsider
                //      them, otherwise the over-allocation is an inevitable consequence
                //      of the given min constraints.
                // Note that if we return to Phase2, at least one *-def will have been
                // resolved.  This guarantees we don't run Phase2+3 infinitely often.
                runPhase2and3 = false;
                if (starCount == 0 && takenSize < finalSize)
                {
                    // if no *-defs remain and we haven't allocated all the space, reconsider the defs
                    // resolved as 'min'.   Their allocation can be increased to make up the gap.
                    for (int i = minCount; i < minCountPhase2; ++i)
                    {
                        if (definitionIndices[i] >= 0)
                        {
                            DefinitionBase def = definitions[definitionIndices[i]];
                            def.MeasureSize = 1.0;      // mark as 'not yet resolved'
                            ++starCount;
                            runPhase2and3 = true;       // found a candidate, so re-run Phases 2 and 3
                        }
                    }
                }

                if (takenSize > finalSize)
                {
                    // if we've allocated too much space, reconsider the defs
                    // resolved as 'max'.   Their allocation can be decreased to make up the gap.
                    for (int i = maxCount; i < maxCountPhase2; ++i)
                    {
                        if (definitionIndices[defCount + i] >= 0)
                        {
                            DefinitionBase def = definitions[definitionIndices[defCount + i]];
                            def.MeasureSize = 1.0;      // mark as 'not yet resolved'
                            ++starCount;
                            runPhase2and3 = true;    // found a candidate, so re-run Phases 2 and 3
                        }
                    }
                }
            }

            // Phase 4.  Resolve the remaining defs proportionally.
            starCount = 0;
            for (int i = 0; i < defCount; ++i)
            {
                DefinitionBase def = definitions[i];

                if (def.UserSize.IsStar)
                {
                    if (def.MeasureSize < 0.0)
                    {
                        // this def was resolved in phase 3 - fix up its size
                        def.SizeCache = -def.MeasureSize;
                    }
                    else
                    {
                        // this def needs resolution, add it to the list, sorted by *-weight
                        definitionIndices[starCount++] = i;
                        def.MeasureSize = StarWeight(def, scale);
                    }
                }
            }

            if (starCount > 0)
            {
                StarWeightIndexComparer starWeightIndexComparer = new StarWeightIndexComparer(definitions);
                Array.Sort(definitionIndices, 0, starCount, starWeightIndexComparer);

                // compute the partial sums of *-weight, in increasing order of weight
                // for minimal loss of precision.
                totalStarWeight = 0.0;
                for (int i = 0; i < starCount; ++i)
                {
                    DefinitionBase def = definitions[definitionIndices[i]];
                    totalStarWeight += def.MeasureSize;
                    def.SizeCache = totalStarWeight;
                }

                // resolve the defs, in decreasing order of weight.
                for (int i = starCount - 1; i >= 0; --i)
                {
                    DefinitionBase def = definitions[definitionIndices[i]];
                    double resolvedSize = (def.MeasureSize > 0.0) ? Math.Max(finalSize - takenSize, 0.0) * (def.MeasureSize / def.SizeCache) : 0.0;

                    // min and max should have no effect by now, but just in case...
                    resolvedSize = Math.Min(resolvedSize, def.UserMaxSize);
                    resolvedSize = Math.Max(def.MinSizeForArrange, resolvedSize);

                    // Use the raw (unrounded) sizes to update takenSize, so that
                    // proportions are computed in the same terms as in phase 3;
                    // this avoids errors arising from min/max constraints.
                    takenSize += resolvedSize;
                    def.SizeCache = resolvedSize;
                }
            }

            // Phase 5.  Apply layout rounding.  We do this after fully allocating
            // unrounded sizes, to avoid breaking assumptions in the previous phases
            if (UseLayoutRounding)
            {
                var dpi = (VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1.0;

                double[] roundingErrors = RoundingErrors;
                double roundedTakenSize = 0.0;

                // round each of the allocated sizes, keeping track of the deltas
                for (int i = 0; i < definitions.Length; ++i)
                {
                    DefinitionBase def = definitions[i];
                    double roundedSize = RoundLayoutValue(def.SizeCache, dpi);
                    roundingErrors[i] = (roundedSize - def.SizeCache);
                    def.SizeCache = roundedSize;
                    roundedTakenSize += roundedSize;
                }

                // The total allocation might differ from finalSize due to rounding
                // effects.  Tweak the allocations accordingly.

                // Theoretical and historical note.  The problem at hand - allocating
                // space to columns (or rows) with *-weights, min and max constraints,
                // and layout rounding - has a long history.  Especially the special
                // case of 50 columns with min=1 and available space=435 - allocating
                // seats in the U.S. House of Representatives to the 50 states in
                // proportion to their population.  There are numerous algorithms
                // and papers dating back to the 1700's, including the book:
                // Balinski, M. and H. Young, Fair Representation, Yale University Press, New Haven, 1982.
                //
                // One surprising result of all this research is that *any* algorithm
                // will suffer from one or more undesirable features such as the
                // "population paradox" or the "Alabama paradox", where (to use our terminology)
                // increasing the available space by one pixel might actually decrease
                // the space allocated to a given column, or increasing the weight of
                // a column might decrease its allocation.   This is worth knowing
                // in case someone complains about this behavior;  it's not a bug so
                // much as something inherent to the problem.  Cite the book mentioned
                // above or one of the 100s of references, and resolve as WontFix.
                //
                // Fortunately, our scenarios tend to have a small number of columns (~10 or fewer)
                // each being allocated a large number of pixels (~50 or greater), and
                // people don't even notice the kind of 1-pixel anomolies that are
                // theoretically inevitable, or don't care if they do.  At least they shouldn't
                // care - no one should be using the results WPF's grid layout to make
                // quantitative decisions; its job is to produce a reasonable display, not
                // to allocate seats in Congress.
                //
                // Our algorithm is more susceptible to paradox than the one currently
                // used for Congressional allocation ("Huntington-Hill" algorithm), but
                // it is faster to run:  O(N log N) vs. O(S * N), where N=number of
                // definitions, S = number of available pixels.  And it produces
                // adequate results in practice, as mentioned above.
                //
                // To reiterate one point:  all this only applies when layout rounding
                // is in effect.  When fractional sizes are allowed, the algorithm
                // behaves as well as possible, subject to the min/max constraints
                // and precision of floating-point computation.  (However, the resulting
                // display is subject to anti-aliasing problems.   TANSTAAFL.)

                if (!MathUtilities.AreClose(roundedTakenSize, finalSize))
                {
                    // Compute deltas
                    for (int i = 0; i < definitions.Length; ++i)
                    {
                        definitionIndices[i] = i;
                    }

                    // Sort rounding errors
                    RoundingErrorIndexComparer roundingErrorIndexComparer = new RoundingErrorIndexComparer(roundingErrors);
                    Array.Sort(definitionIndices, 0, definitions.Length, roundingErrorIndexComparer);
                    double adjustedSize = roundedTakenSize;
                    double dpiIncrement = 1.0 / dpi;

                    if (roundedTakenSize > finalSize)
                    {
                        int i = definitions.Length - 1;
                        while ((adjustedSize > finalSize && !MathUtilities.AreClose(adjustedSize, finalSize)) && i >= 0)
                        {
                            DefinitionBase definition = definitions[definitionIndices[i]];
                            double final = definition.SizeCache - dpiIncrement;
                            final = Math.Max(final, definition.MinSizeForArrange);
                            if (final < definition.SizeCache)
                            {
                                adjustedSize -= dpiIncrement;
                            }
                            definition.SizeCache = final;
                            i--;
                        }
                    }
                    else if (roundedTakenSize < finalSize)
                    {
                        int i = 0;
                        while ((adjustedSize < finalSize && !MathUtilities.AreClose(adjustedSize, finalSize)) && i < definitions.Length)
                        {
                            DefinitionBase definition = definitions[definitionIndices[i]];
                            double final = definition.SizeCache + dpiIncrement;
                            final = Math.Max(final, definition.MinSizeForArrange);
                            if (final > definition.SizeCache)
                            {
                                adjustedSize += dpiIncrement;
                            }
                            definition.SizeCache = final;
                            i++;
                        }
                    }
                }
            }

            // Phase 6.  Compute final offsets
            definitions[0].FinalOffset = 0.0;
            for (int i = 0; i < definitions.Length; ++i)
            {
                definitions[(i + 1) % definitions.Length].FinalOffset = definitions[i].FinalOffset + definitions[i].SizeCache;
            }
        }

        // Choose the ratio with maximum discrepancy from the current proportion.
        // Returns:
        //     true    if proportion fails a min constraint but not a max, or
        //                 if the min constraint has higher discrepancy
        //     false   if proportion fails a max constraint but not a min, or
        //                 if the max constraint has higher discrepancy
        //     null    if proportion doesn't fail a min or max constraint
        // The discrepancy is the ratio of the proportion to the max- or min-ratio.
        // When both ratios hit the constraint,  minRatio < proportion < maxRatio,
        // and the minRatio has higher discrepancy if
        //         (proportion / minRatio) > (maxRatio / proportion)
        private static bool? Choose(double minRatio, double maxRatio, double proportion)
        {
            if (minRatio < proportion)
            {
                if (maxRatio > proportion)
                {
                    // compare proportion/minRatio : maxRatio/proportion, but
                    // do it carefully to avoid floating-point overflow or underflow
                    // and divide-by-0.
                    double minPower = Math.Floor(Math.Log(minRatio, 2.0));
                    double maxPower = Math.Floor(Math.Log(maxRatio, 2.0));
                    double f = Math.Pow(2.0, Math.Floor((minPower + maxPower) / 2.0));
                    if ((proportion / f) * (proportion / f) > (minRatio / f) * (maxRatio / f))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            else if (maxRatio > proportion)
            {
                return false;
            }

            return null;
        }

        /// <summary>
        /// Sorts row/column indices by rounding error if layout rounding is applied.
        /// </summary>
        /// <param name="x">Index, rounding error pair</param>
        /// <param name="y">Index, rounding error pair</param>
        /// <returns>1 if x.Value > y.Value, 0 if equal, -1 otherwise</returns>
        private static int CompareRoundingErrors(KeyValuePair<int, double> x, KeyValuePair<int, double> y)
        {
            if (x.Value < y.Value)
            {
                return -1;
            }
            else if (x.Value > y.Value)
            {
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Calculates final (aka arrange) size for given range.
        /// </summary>
        /// <param name="definitions">Array of definitions to process.</param>
        /// <param name="start">Start of the range.</param>
        /// <param name="count">Number of items in the range.</param>
        /// <returns>Final size.</returns>
        private double GetFinalSizeForRange(
           DefinitionBase[] definitions,
            int start,
            int count)
        {
            double size = 0;
            int i = start + count - 1;

            do
            {
                size += definitions[i].SizeCache;
            } while (--i >= start);

            return (size);
        }

        /// <summary>
        /// Clears dirty state for the grid and its columns / rows
        /// </summary>
        private void SetValid()
        {
            if (IsTrivialGrid && _tempDefinitions != null)
            {
                //  TempDefinitions has to be cleared to avoid "memory leaks"
                Array.Clear(_tempDefinitions, 0, Math.Max(_definitionsU.Length, _definitionsV.Length));
                _tempDefinitions = null;
            }
        }

        /// <summary>
        /// Synchronized ShowGridLines property with the state of the grid's visual collection
        /// by adding / removing GridLinesRenderer visual.
        /// Returns a reference to GridLinesRenderer visual or null.
        /// </summary>
        private GridLinesRenderer EnsureGridLinesRenderer()
        {
            //
            //  synchronize the state
            //
            if (ShowGridLines && (_gridLinesRenderer == null))
            {
                _gridLinesRenderer = new GridLinesRenderer();
                this.VisualChildren.Add(_gridLinesRenderer);
            }

            if ((!ShowGridLines) && (_gridLinesRenderer != null))
            {
                this.VisualChildren.Remove(_gridLinesRenderer);
                _gridLinesRenderer = null;
            }

            return (_gridLinesRenderer);
        }

        private double RoundLayoutValue(double value, double dpiScale)
        {
            double newValue;

            // If DPI == 1, don't use DPI-aware rounding.
            if (!MathUtilities.AreClose(dpiScale, 1.0))
            {
                newValue = Math.Round(value * dpiScale) / dpiScale;
                // If rounding produces a value unacceptable to layout (NaN, Infinity or MaxValue), use the original value.
                if (double.IsNaN(newValue) ||
                    double.IsInfinity(newValue) ||
                    MathUtilities.AreClose(newValue, double.MaxValue))
                {
                    newValue = value;
                }
            }
            else
            {
                newValue = Math.Round(value);
            }

            return newValue;
        }

        private static int ValidateColumn(AvaloniaObject o, int value)
        {
            if (value < 0)
            {
                throw new ArgumentException("Invalid Grid.Column value.");
            }

            return value;
        }

        private static int ValidateRow(AvaloniaObject o, int value)
        {
            if (value < 0)
            {
                throw new ArgumentException("Invalid Grid.Row value.");
            }

            return value;
        }

        private static void OnShowGridLinesPropertyChanged(Grid grid, AvaloniaPropertyChangedEventArgs e)
        {
            if (!grid.IsTrivialGrid)  // trivial grid is 1 by 1. there is no grid lines anyway
            {
                grid.Invalidate();
            }
        }

        /// <summary>
        /// Returns *-weight, adjusted for scale computed during Phase 1
        /// </summary>
        private static double StarWeight(DefinitionBase def, double scale)
        {
            if (scale < 0.0)
            {
                // if one of the *-weights is Infinity, adjust the weights by mapping
                // Infinty to 1.0 and everything else to 0.0:  the infinite items share the
                // available space equally, everyone else gets nothing.
                return (double.IsPositiveInfinity(def.UserSize.Value)) ? 1.0 : 0.0;
            }
            else
            {
                return def.UserSize.Value * scale;
            }
        }
    }
}