// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Data;
using Avalonia.Utilities;
using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Avalonia.Reactive;
using System.Diagnostics;
using Avalonia.Controls.Utils;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a <see cref="T:Avalonia.Controls.DataGrid" /> column that can 
    /// bind to a property in the grid's data source.
    /// </summary>
    public abstract class DataGridBoundColumn : DataGridColumn
    {
        private IBinding _binding;
        
        /// <summary>
        /// Gets or sets the binding that associates the column with a property in the data source.
        /// </summary>
        //TODO Binding
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
                        if(_binding is Avalonia.Data.Binding binding)
                        {
                            // Force the TwoWay binding mode if there is a Path present.  TwoWay binding requires a Path.
                            if (!String.IsNullOrEmpty(binding.Path))
                            {
                                binding.Mode = BindingMode.TwoWay;
                            }

                            if (binding.Converter == null)
                            {
                                binding.Converter = DataGridValueConverter.Instance;
                            }
                        }

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

        /// <summary>
        /// The binding that will be used to get or set cell content for the clipboard.
        /// If the base ClipboardContentBinding is not explicitly set, this will return the value of Binding.
        /// </summary>
        public override IBinding ClipboardContentBinding
        {
            get
            {
                return base.ClipboardContentBinding ?? Binding;
            }
            set
            {
                base.ClipboardContentBinding = value;
            }
        }

        //TODO Rename
        //TODO Validation
        protected sealed override IControl GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding editBinding)
        {
            IControl element = GenerateEditingElementDirect(cell, dataItem);
            editBinding = null;

            if (Binding != null)
            //if (Binding != null || !DesignerProperties.IsInDesignTool)
            {
                //var t1 = Binding.Initiate(textBox, BindingTarget, anchor: null, enableDataValidation: true);
                //BindingOperations.Apply(textBox, BindingTarget, t1, null);

                editBinding = BindEditingElement(element, BindingTarget, Binding);
                //element.Bind(BindingTarget, Binding);
                //textBox.SetBinding(BindingTarget, Binding);
            }

            return element;
        }

        private static ICellEditBinding BindEditingElement(IAvaloniaObject target, AvaloniaProperty property, IBinding binding)
        {
            var result = binding.Initiate(target, property, enableDataValidation: true);

            if (result != null)
            {
                if(result.Subject != null)
                {
                    var bindingHelper = new CellEditBinding(result.Subject);
                    var instanceBinding = new InstancedBinding(bindingHelper.InternalSubject, result.Mode, result.Priority);

                    BindingOperations.Apply(target, property, instanceBinding, null);
                    return bindingHelper;
                }

                BindingOperations.Apply(target, property, result, null);
            }

            return null;
        }
        
        /*
         public static IDisposable Apply(
            IAvaloniaObject target,
            AvaloniaProperty property,
            InstancedBinding binding,
            object anchor)
        {
            Contract.Requires<ArgumentNullException>(target != null);
            Contract.Requires<ArgumentNullException>(property != null);
            Contract.Requires<ArgumentNullException>(binding != null);

            var mode = binding.Mode;

            if (mode == BindingMode.Default)
            {
                mode = property.GetMetadata(target.GetType()).DefaultBindingMode;
            }

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    return target.Bind(property, binding.Observable ?? binding.Subject, binding.Priority);
                case BindingMode.TwoWay:
                    return new CompositeDisposable(
                        target.Bind(property, binding.Subject, binding.Priority),
                        target.GetObservable(property).Subscribe(binding.Subject));
                case BindingMode.OneTime:
                    var source = binding.Subject ?? binding.Observable;

                    if (source != null)
                    {
                        return source
                            .Where(x => BindingNotification.ExtractValue(x) != AvaloniaProperty.UnsetValue)
                            .Take(1)
                            .Subscribe(x => target.SetValue(property, x, binding.Priority));
                    }
                    else
                    {
                        target.SetValue(property, binding.Value, binding.Priority);
                        return Disposable.Empty;
                    }
                case BindingMode.OneWayToSource:
                    return target.GetObservable(property).Subscribe(binding.Subject);
                default:
                    throw new ArgumentException("Invalid binding mode.");
            }
             */


        protected abstract IControl GenerateEditingElementDirect(DataGridCell cell, object dataItem);

        internal AvaloniaProperty BindingTarget { get; set; }

        internal void SetHeaderFromBinding()
        {
            if (OwningGrid != null && OwningGrid.DataConnection.DataType != null
                && Header == null && Binding != null && Binding is Binding binding
                && !String.IsNullOrWhiteSpace(binding.Path))
            {
                string header = OwningGrid.DataConnection.DataType.GetDisplayName(binding.Path);
                if (header != null)
                {
                    Header = header;
                }
            }
        }
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


    #endregion
    
    #region Styles

    //TODO Styles

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
