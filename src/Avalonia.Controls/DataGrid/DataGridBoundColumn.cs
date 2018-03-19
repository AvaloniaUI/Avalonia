// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Media;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a <see cref="T:System.Windows.Controls.DataGrid" /> column that can 
    /// bind to a property in the grid's data source.
    /// </summary>
    public abstract class DataGridBoundColumn : DataGridColumn
    {
        private IBinding _binding;
        
        /// <summary>
        /// Gets or sets the binding that associates the column with a property in the data source.
        /// </summary>
        //TODO
        //Binding
        public virtual IBinding Binding
        {
            get
            {
                return _binding;
            }
            set
            {
                if (_binding != value)
                {
                    if (OwningGrid != null && !OwningGrid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true))
                    {
                        // Edited value couldn't be committed, so we force a CancelEdit
                        OwningGrid.CancelEdit(DataGridEditingUnit.Row, raiseEvents: false);
                    }

                    _binding = value;

                    if (_binding != null)
                    {
                        //// Force the TwoWay binding mode if there is a Path present.  TwoWay binding requires a Path.
                        //if (!String.IsNullOrEmpty(_binding.Path.Path))
                        //{
                        //    _binding.Mode = BindingMode.TwoWay;
                        //}

                        //if (_binding.Converter == null)
                        //{
                        //    _binding.Converter = new DataGridValueConverter();
                        //}

                        //// Setup the binding for validation
                        //_binding.ValidatesOnDataErrors = true;
                        //_binding.ValidatesOnExceptions = true;
                        //_binding.NotifyOnValidationError = true;
                        //_binding.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;

                        // Apply the new Binding to existing rows in the DataGrid
                        if (OwningGrid != null)
                        {
                            OwningGrid.OnColumnBindingChanged(this);
                        }
                    }

                    RemoveEditingElement();
                }
            }
        }

        internal AvaloniaProperty BindingTarget { get; set; }

    }

    /*
     
    [StyleTypedProperty(Property = "ElementStyle", StyleTargetType = typeof(FrameworkElement))]
    [StyleTypedProperty(Property = "EditingElementStyle", StyleTargetType = typeof(FrameworkElement))]
    */

    #region Binding


    /*internal override List<string> CreateBindingPaths()
    {
        if (Binding != null && Binding.Path != null)
        {
            return new List<string>() { Binding.Path.Path };
        }
        return base.CreateBindingPaths();
    }*/

    /*internal override List<BindingInfo> CreateBindings(FrameworkElement element, object dataItem, bool twoWay)
    {
        BindingInfo bindingData = new BindingInfo();
        if (twoWay && BindingTarget != null)
        {
            bindingData.BindingExpression = element.GetBindingExpression(BindingTarget);
            if (bindingData.BindingExpression != null)
            {
                bindingData.BindingTarget = BindingTarget;
                bindingData.Element = element;
                return new List<BindingInfo> { bindingData };
            }
        }
        foreach (DependencyProperty bindingTarget in element.GetDependencyProperties(false))
        {
            bindingData.BindingExpression = element.GetBindingExpression(bindingTarget);
            if (bindingData.BindingExpression != null
                && bindingData.BindingExpression.ParentBinding == Binding)
            {
                BindingTarget = bindingTarget;
                bindingData.BindingTarget = BindingTarget;
                bindingData.Element = element;
                return new List<BindingInfo> { bindingData };
            }
        }
        return base.CreateBindings(element, dataItem, twoWay);
    }*/

    /*internal void SetHeaderFromBinding()
    {
        if (OwningGrid != null && OwningGrid.DataConnection.DataType != null
            && Header == null && Binding != null && Binding.Path != null)
        {
            string header = OwningGrid.DataConnection.DataType.GetDisplayName(Binding.Path.Path);
            if (header != null)
            {
                Header = header;
            }
        }
    }*/

    #endregion

    #region ClipBoard

    /// <summary>
    /// The binding that will be used to get or set cell content for the clipboard.
    /// If the base ClipboardContentBinding is not explicitly set, this will return the value of Binding.
    /// </summary>
    /*public override Binding ClipboardContentBinding
    {
        get
        {
            return base.ClipboardContentBinding ?? Binding;
        }
        set
        {
            base.ClipboardContentBinding = value;
        }
    }*/



    #endregion


    #region Styles

    /*
    private Style _elementStyle;
    private Style _editingElementStyle;
    */


    /// <summary>
    /// Gets or sets the style that is used when rendering the element that the column displays for a cell in editing mode.
    /// </summary>
    /*public Style EditingElementStyle
    {
        get
        {
            return _editingElementStyle;
        }
        set
        {
            if (_editingElementStyle != value)
            {
                _editingElementStyle = value;
                // We choose not to update the elements already editing in the Grid here.  They
                // will get the EditingElementStyle next time they go into edit mode
            }
        }
    }*/

    /// <summary>
    /// Gets or sets the style that is used when rendering the element that the column displays for a cell 
    /// that is not in editing mode.
    /// </summary>
    /*public Style ElementStyle
    {
        get
        {
            return _elementStyle;
        }
        set
        {
            if (_elementStyle != value)
            {
                _elementStyle = value;
                if (OwningGrid != null)
                {
                    OwningGrid.OnColumnElementStyleChanged(this);
                }
            }
        }
    }*/


    #endregion
}
