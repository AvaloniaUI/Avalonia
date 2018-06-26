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
        protected sealed override  Control GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            Control element = GenerateEditingElementDirect(cell, dataItem);

            if (Binding != null)
            //if (Binding != null || !DesignerProperties.IsInDesignTool)
            {
                //var t1 = Binding.Initiate(textBox, BindingTarget, anchor: null, enableDataValidation: true);
                //BindingOperations.Apply(textBox, BindingTarget, t1, null);

                BindEditingElement(element, BindingTarget, Binding);
                //element.Bind(BindingTarget, Binding);
                //textBox.SetBinding(BindingTarget, Binding);
            }

            return element;
        }

        private static IDisposable BindEditingElement(IAvaloniaObject target, AvaloniaProperty property, IBinding binding)
        {
            var result = binding.Initiate(target, property, enableDataValidation: true);

            if (result != null)
            {
                //if(result.Subject != null)
                //{
                //    var watcher = new BindingWatcher(result.Subject, result);
                //    result = watcher.InstancedBinding;
                //}

                return BindingOperations.Apply(target, property, result, null);
            }
            else
            {
                return Disposable.Empty;
            }
        }

        public class LightweightSubject<T> : LightweightObservableBase<T>, ISubject<T>
        {
            public void OnCompleted()
            {
                PublishCompleted();
            }
            public void OnError(Exception error)
            {
                PublishError(error);
            }
            public void OnNext(T value)
            {
                PublishNext(value);
            }

            protected override void Deinitialize()
            { }
            protected override void Initialize()
            { }

            protected override void Subscribed(IObserver<T> observer, bool first)
            {
                base.Subscribed(observer, first);
            }
        }

        class BindingWatcher
        {
            ISubject<object> _innerSubject;
            ISubject<object> _wrappedSubject;
            InstancedBinding _instancedBinding;
            bool _isWriting = false;

            public BindingWatcher(ISubject<object> innerSubject, InstancedBinding innerBinding)
            {
                _innerSubject = innerSubject;
                _wrappedSubject = new LightweightSubject<object>();
                _instancedBinding = new InstancedBinding(_wrappedSubject, innerBinding.Mode, innerBinding.Priority);

                _innerSubject.Subscribe(InnerSubjectOnNext);
                _wrappedSubject.Subscribe(WrappedSubjectOnNext);
            }

            private void InnerSubjectOnNext(object value)
            {
                Debug.WriteLine($"InnerSubject: {value} ({value?.GetType().Name})");
                if (!_isWriting)
                {
                    _isWriting = true;
                    _wrappedSubject.OnNext(value);
                    _isWriting = false;
                }
            }
            private void WrappedSubjectOnNext(object value)
            {
                Debug.WriteLine($"WrappedSubject: {value} ({value?.GetType().Name})");
                if (!_isWriting)
                {
                    _isWriting = true;
                    _innerSubject.OnNext(value);
                    _isWriting = false;
                }
            }

            public InstancedBinding InstancedBinding => _instancedBinding;
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


        protected abstract Control GenerateEditingElementDirect(DataGridCell cell, object dataItem);


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

        private class AdvancedBinding
        {

            class SubjectWrapper : LightweightObservableBase<object>, ISubject<object>, IDisposable
            {
                private readonly ISubject<object> _sourceSubject;
                private IDisposable _subscription;
                private object _controlValue;
                private bool _isControlValueSet = false;

                public SubjectWrapper(ISubject<object> bindingSourceSubject)
                {
                    _sourceSubject = bindingSourceSubject;
                }

                private void SetSourceValue(object value)
                {
                    _sourceSubject.OnNext(value);
                }
                private void SetControlValue(object value)
                {
                    PublishNext(value);
                }

                private void OnValidationError(BindingNotification notification)
                {

                }
                private void OnControlValueUpdated(object value)
                {
                    _controlValue = value;
                    _isControlValueSet = true;
                }
                private void OnSourceValueUpdated(object value)
                {
                    if(value is BindingNotification notification)
                    {
                        if (notification.ErrorType != BindingErrorType.None)
                            OnValidationError(notification);
                        else
                            SetControlValue(value);
                    }
                    else
                    {
                        SetControlValue(value);
                    }
                }

                protected override void Deinitialize()
                {
                    _subscription?.Dispose();
                    _subscription = null;
                }
                protected override void Initialize()
                {
                    _subscription = _sourceSubject.Subscribe(OnSourceValueUpdated);
                }

                void IObserver<object>.OnCompleted()
                {
                    throw new NotImplementedException();
                }
                void IObserver<object>.OnError(Exception error)
                {
                    throw new NotImplementedException();
                }
                void IObserver<object>.OnNext(object value)
                {
                    OnControlValueUpdated(value);
                }

                public void Dispose()
                {
                    _subscription?.Dispose();
                    _subscription = null;
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

    #region ClipBoard



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
