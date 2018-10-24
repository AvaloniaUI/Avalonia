// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class PropertyDetails : ViewModelBase, IDisposable
    {
        private static Dictionary<Type, TypeConverter> _typeConverters = new Dictionary<Type, TypeConverter>();
        private IDisposable _disposable;
        private object _originalValue;
        private object _value;
        private bool _isChanged = false;
        private string _priority;
        private string _diagnostic;

        private AvaloniaObject _object;
        private AvaloniaProperty _property;
        private TypeConverter _typeConverter;
        private IEnumerable<string> _possibleValues;

        private static TypeConverter TryGetTypeConverter(AvaloniaProperty property)
        {
            TypeConverter result;

            if (_typeConverters.TryGetValue(property.PropertyType, out result))
                return result;

            var convType = AvaloniaTypeConverters.GetTypeConverter(property.PropertyType);

            if (convType != null)
                result = (TypeConverter)Activator.CreateInstance(convType);

            if (result == null)
                result = TypeDescriptor.GetConverter(property.PropertyType);

            if (result?.CanConvertFrom(typeof(string)) == false)
            {
                result = null;
            }

            return _typeConverters[property.PropertyType] = result;
        }

        public PropertyDetails(AvaloniaObject o, AvaloniaProperty property)
        {
            Name = property.IsAttached ?
                $"[{property.OwnerType.Name}.{property.Name}]" :
                property.Name;

            _typeConverter = property.IsReadOnly ? null : TryGetTypeConverter(property);
            IsAttached = property.IsAttached;
            IsReadOnly = property.IsReadOnly || _typeConverter == null;
            bool first = true;
            // TODO: Unsubscribe when view model is deactivated.
            _disposable = o.GetObservable(property).Subscribe(x =>
            {
                var diagnostic = o.GetDiagnostic(property);
                //SetValue(diagnostic.Value?.ToString() ?? "(null)", false);
                object value = (_typeConverter != null ?
                _typeConverter?.ConvertToString(diagnostic.Value) : diagnostic.Value?.ToString()) ?? "(null)";
                if (first)
                {
                    first = false;
                    _originalValue = value;
                }

                SetValue(value, false);
                Priority = (diagnostic.Priority != BindingPriority.Unset) ?
                    diagnostic.Priority.ToString() :
                    diagnostic.Property.Inherits ? "Inherited" : "Unset";
                Diagnostic = diagnostic.Diagnostic;
            });

            _object = o;
            _property = property;
        }

        private static Dictionary<Type, string[]> _typespossibleValues = new Dictionary<Type, string[]>();

        private string[] GetPossibleValues()
        {
            string[] result;

            if (_typespossibleValues.TryGetValue(_property.PropertyType, out result))
                return result;

            if (_property.PropertyType.IsEnum)
            {
                result = Enum.GetNames(_property.PropertyType);
            }
            else if (_property.PropertyType == typeof(IBrush))
            {
                result = typeof(Brushes).GetProperties().Select(p => p.Name).ToArray();
            }
            else if (_property.PropertyType == typeof(bool))
            {
                result = new[] { "True", "False" };
            }

            return _typespossibleValues[_property.PropertyType] = result;
        }

        public string Name { get; }

        public bool IsAttached { get; }

        public bool IsReadOnly { get; }

        public string Priority
        {
            get { return _priority; }
            private set { RaiseAndSetIfChanged(ref _priority, value); }
        }

        public string Diagnostic
        {
            get { return _diagnostic; }
            private set { RaiseAndSetIfChanged(ref _diagnostic, value); }
        }

        public bool IsChanged
        {
            get { return _isChanged; }
            set { this.RaiseAndSetIfChanged(ref _isChanged, value); }
        }

        private void SetValue(object value, bool setback)
        {
            ValueErrors = null;

            if (!EqualityComparer<object>.Default.Equals(_value, value))
            {
                string stringValue = (value as string)?.TrimStart(' ');

                if (setback && !IsReadOnly)
                {
                    try
                    {
                        object propValue = string.IsNullOrEmpty(stringValue) || stringValue == "(null)" ?
                                            null : _typeConverter.ConvertFrom(stringValue);

                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (string.IsNullOrEmpty(stringValue))
                            {
                                _object.ClearValue(_property);
                            }
                            else
                            {
                                _object.SetValue(_property, propValue);
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        ValueErrors = new[] { e };
                        throw e;
                    }
                }

                _value = value;
                RaisePropertyChanged(nameof(Value));

                IsChanged = !Equals(value, _originalValue);
            }
        }

        public object Value
        {
            get { return _value; }
            set
            {
                SetValue(value, true);
            }
        }

        private IEnumerable<Exception> _valueErrors;

        public IEnumerable<Exception> ValueErrors
        {
            get { return _valueErrors; }
            set
            {
                this.RaiseAndSetIfChanged(ref _valueErrors, value);
                HasValueError = value != null;
            }
        }

        private bool _hasValueError;

        public bool HasValueError
        {
            get { return _hasValueError; }
            set { this.RaiseAndSetIfChanged(ref _hasValueError, value); }
        }

        public async Task<IEnumerable<object>> PossibleValuesPopulator(string text, CancellationToken token)
        {
            if (text.Equals(Value))
                return Enumerable.Empty<string>();

            await Task.Delay(100, token);

            return PossibleValues;
        }

        public void Dispose()
        {
            _disposable?.Dispose();
        }

        public IEnumerable<string> PossibleValues
        {
            get => _possibleValues ?? (_possibleValues = GetPossibleValues());
        }
    }
}
