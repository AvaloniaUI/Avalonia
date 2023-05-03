// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved. 

using Avalonia.Data;
using System;
using Avalonia.Controls.Utils;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Metadata;
using Avalonia.Reactive;

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
        [AssignBinding]
        [InheritDataTypeFromItems(nameof(DataGrid.ItemsSource), AncestorType = typeof(DataGrid))]
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
                        if(_binding is BindingBase binding)
                        {
                            if (binding.Mode == BindingMode.OneWayToSource)
                            {
                                throw new InvalidOperationException("DataGridColumn doesn't support BindingMode.OneWayToSource. Use BindingMode.TwoWay instead.");
                            }

                            var path = (binding as Binding)?.Path ?? (binding as CompiledBindingExtension)?.Path.ToString();
                            if (!string.IsNullOrEmpty(path) && binding.Mode == BindingMode.Default)
                            {
                                binding.Mode = BindingMode.TwoWay;
                            } 

                            if (binding.Converter == null && string.IsNullOrEmpty(binding.StringFormat))
                            {
                                binding.Converter = DataGridValueConverter.Instance;
                            }
                        }  

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
        protected sealed override Control GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding editBinding)
        {
            Control element = GenerateEditingElementDirect(cell, dataItem);
            editBinding = null; 

            if (Binding != null)
            {
                editBinding = BindEditingElement(element, BindingTarget, Binding);
            } 

            return element;
        } 

        private static ICellEditBinding BindEditingElement(AvaloniaObject target, AvaloniaProperty property, IBinding binding)
        {
            var result = binding.Initiate(target, property, enableDataValidation: true); 

            if (result != null)
            {
                if(result.Source is IAvaloniaSubject<object> subject)
                {
                    var bindingHelper = new CellEditBinding(subject);
                    var instanceBinding = new InstancedBinding(bindingHelper.InternalSubject, result.Mode, result.Priority); 

                    BindingOperations.Apply(target, property, instanceBinding, null);
                    return bindingHelper;
                } 

                BindingOperations.Apply(target, property, result, null);
            } 

            return null;
        } 

        protected abstract Control GenerateEditingElementDirect(DataGridCell cell, object dataItem); 

        protected AvaloniaProperty BindingTarget { get; set; } 

        internal void SetHeaderFromBinding()
        {
            if (OwningGrid != null && OwningGrid.DataConnection.DataType != null
                && Header == null && Binding != null && Binding is BindingBase binding)
            {
                var path = (binding as Binding)?.Path ?? (binding as CompiledBindingExtension)?.Path.ToString();
                if (!string.IsNullOrWhiteSpace(path))
                {
                    var header = OwningGrid.DataConnection.DataType.GetDisplayName(path);
                    if (header != null)
                    {
                        Header = header;
                    }
                }
            }
        }
    } 
}
