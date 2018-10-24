// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Media;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class AvaloniaTypeConverters
    {
        public class ParseTypeConverter : TypeConverter
        {
            private readonly Func<string, CultureInfo, object> _parse;

            public static Func<string, CultureInfo, object> TryGetParse(Type type)
            {
                var bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

                var parse = type.GetMethod("Parse", bf, null, new Type[] { typeof(string), typeof(CultureInfo) }, null) ??
                             type.GetMethod("Parse", bf, null, new Type[] { typeof(string), typeof(IFormatProvider) }, null);

                if (parse?.ReturnParameter?.ParameterType == type)
                {
                    return (s, c) => parse.Invoke(null, new object[] { s, c });
                }

                parse = type.GetMethod("Parse", bf, null, new Type[] { typeof(string) }, null);
                if (parse?.ReturnParameter?.ParameterType == type)
                {
                    return (s, c) => parse.Invoke(null, new object[] { s });
                }

                return null;
            }

            public ParseTypeConverter(Func<string, CultureInfo, object> parse)
            {
                _parse = parse;
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                return _parse((string)value, culture);
            }
        }

        private static Dictionary<Type, TypeConverter> _converters = new Dictionary<Type, TypeConverter>()
        {
            {typeof(TimeSpan), new Markup.Xaml.Converters.TimeSpanTypeConverter() },
            {typeof(FontFamily), new Markup.Xaml.Converters.FontFamilyTypeConverter() },
            {typeof(int), null},
            {typeof(double), null},
            {typeof(string), null}
        };

        static public TypeConverter TryGetTypeConverter(Type type)
        {
            _converters.TryGetValue(type, out TypeConverter result);

            if (result == null)
            {
                var tca = type.GetCustomAttribute<System.ComponentModel.TypeConverterAttribute>();
                if (tca == null)
                {
                    var p = ParseTypeConverter.TryGetParse(type);
                    if (p != null)
                    {
                        result = new ParseTypeConverter(p);
                    }

                    _converters[type] = result;
                }
            }

            return result;
        }
    }

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
        private Func<object> _getter;
        private Action<object> _setter;
        private bool _setActive = false;

        private static TypeConverter TryGetTypeConverter(AvaloniaProperty property)
        {
            TypeConverter result;

            if (_typeConverters.TryGetValue(property.PropertyType, out result))
                return result;

            result = AvaloniaTypeConverters.TryGetTypeConverter(property.PropertyType) ??
                        TypeDescriptor.GetConverter(property.PropertyType);

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
            _disposable = o.GetObservable(property).Where(_ => !_setActive).Subscribe(x =>
            {
                var diagnostic = o.GetDiagnostic(property);
                object value = _typeConverter?.ConvertToString(diagnostic.Value) ?? diagnostic.Value?.ToString() ?? "(null)";
                if (first)
                {
                    first = false;
                    _originalValue = value;
                }

                SetValue(value, false);
                Priority = (diagnostic.Priority != BindingPriority.Unset) ?
                    diagnostic.Priority.ToString() :
                    diagnostic.Property.Inherits ?
                        "Inherited" :
                        "Unset";
                Diagnostic = diagnostic.Diagnostic;
            });

            _object = o;
            _property = property;
        }

        public PropertyDetails(AvaloniaObject o, string propertyName, Func<object> getter, Action<object> setter, IObservable<object> changed)
        {
            _object = o;
            Name = propertyName;
            _getter = getter;
            _setter = setter;
            IsReadOnly = setter == null;
            _originalValue = _getter();
            SetValue(_getter(), false);
            var inpc = o as INotifyPropertyChanged;
            changed = changed ?? Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>
                                (v => inpc.PropertyChanged += v, v => inpc.PropertyChanged -= v)
                                .Where(v => v.EventArgs.PropertyName == propertyName);
            _disposable = changed.Where(_ => !_setActive).Subscribe(_ => SetValue(_getter() ?? "(null)", false));
        }

        private static Dictionary<Type, string[]> _typespossibleValues = new Dictionary<Type, string[]>();

        private string[] GetPossibleValues()
        {
            string[] result;

            if (_typespossibleValues.TryGetValue(_property.PropertyType, out result))
                return result;

            if (_property != null)
            {
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
            }

            return _typespossibleValues[_property.PropertyType] = result;
        }

        public string Name { get; }

        public bool IsAttached { get; }

        public bool IsReadOnly { get; }

        public string Priority
        {
            get => _priority;
            private set => RaiseAndSetIfChanged(ref _priority, value);
        }

        public string Diagnostic
        {
            get => _diagnostic;
            private set => RaiseAndSetIfChanged(ref _diagnostic, value);
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
                                            null : (_typeConverter?.ConvertFrom(stringValue) ?? stringValue);

                        _setActive = true;
                        if (_setter != null)
                        {
                            _setter(propValue);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(stringValue))
                            {
                                _object.ClearValue(_property);
                            }
                            else
                            {
                                _object.SetValue(_property, propValue);
                            }
                        }
                        _setActive = false;
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
