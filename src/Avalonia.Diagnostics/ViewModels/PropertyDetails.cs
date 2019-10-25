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
using Avalonia.Diagnostics.Views;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class AvaloniaTypeConverters
    {
        public class ParseTypeConverter : TypeConverter
        {
            private static Dictionary<IPlatformHandle, string> _standardCursors;

            private static string TryGetCursorName(Cursor cursor)
            {
                if (cursor?.PlatformCursor == null)
                    return "";

                if (_standardCursors == null)
                {
                    _standardCursors = new Dictionary<IPlatformHandle, string>();
                    try
                    {
                        var platform = AvaloniaLocator.Current.GetService<IStandardCursorFactory>();

                        foreach (StandardCursorType c in Enum.GetValues(typeof(StandardCursorType)))
                        {
                            _standardCursors[platform.GetCursor(c)] = c.ToString();
                        }
                    }
                    catch
                    {
                    }
                }

                return cursor?.PlatformCursor != null && _standardCursors.TryGetValue(cursor.PlatformCursor, out string r) ? r : cursor?.PlatformCursor?.ToString();
            }

            private readonly Func<string, CultureInfo, object> _parse;

            private static Dictionary<Type, Func<object, string>> _customToString = new Dictionary<Type, Func<object, string>>()
            {
                //TODO: may be override ToString and remove this hardcoded functionality
                { typeof(RelativePoint), o =>
                                {
                                    var rp = (RelativePoint)o;
                                    return rp.Unit== RelativeUnit.Absolute?rp.Point.ToString():$"{rp.Point.X*100}%,{rp.Point.Y*100}%";
                                } },
                { typeof(Cursor), o => TryGetCursorName(o as Cursor) },
            };

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

                parse = type.GetMethod("Parse", bf);
                if (parse?.ReturnParameter?.ParameterType == type)
                {
                    var pars = parse.GetParameters();
                    //parse with string parameter and default second argument
                    if (pars.Length == 2 && pars[0].ParameterType == typeof(string) && pars[1].IsOptional)
                    {
                        return (s, c) => parse.Invoke(null, new object[] { s, Type.Missing });
                    }
                }

                return null;
            }

            public ParseTypeConverter(Func<string, CultureInfo, object> parse)
            {
                _parse = parse;
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
                => sourceType == typeof(string);

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
                => _parse((string)value, culture);

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
                => _customToString.TryGetValue(value?.GetType() ?? typeof(object), out var ts) ? ts(value) : value?.ToString();

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
                => destinationType == typeof(string);
        }

        private static Dictionary<Type, TypeConverter> _converters = new Dictionary<Type, TypeConverter>()
        {
            //here hard coded type converters if needed
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
        private string _priority = "";
        private string _diagnostic;
        private AvaloniaObject _object;
        private AvaloniaProperty _property;
        private WellKnownProperty _wellKnownProperty;
        private TypeConverter _typeConverter;
        private IEnumerable<string> _hintValues;
        private bool _setActive = false;

        private static TypeConverter TryGetTypeConverter(Type propertyType)
        {
            if (propertyType == null)
            {
                return null;
            }

            TypeConverter result;

            if (_typeConverters.TryGetValue(propertyType, out result))
                return result;

            result = AvaloniaTypeConverters.TryGetTypeConverter(propertyType) ??
                        TypeDescriptor.GetConverter(propertyType);

            if (result?.CanConvertFrom(typeof(string)) == false)
            {
                result = null;
            }

            return _typeConverters[propertyType] = result;
        }

        public PropertyDetails(AvaloniaObject o, AvaloniaProperty property)
        {
            Name = property.IsAttached ?
                $"[{property.OwnerType.Name}.{property.Name}]" :
                property.Name;

            _typeConverter = TryGetTypeConverter(property.PropertyType);
            IsAttached = property.IsAttached;
            IsReadOnly = property.IsReadOnly || !(_typeConverter?.CanConvertFrom(typeof(string)) ?? false);
            bool first = true;
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

        public PropertyDetails(AvaloniaObject o, WellKnownProperty property)
        {
            _wellKnownProperty = property;
            _object = o;
            Name = _wellKnownProperty.Name;
            _typeConverter = TryGetTypeConverter(_wellKnownProperty.Type);
            IsReadOnly = !(_wellKnownProperty.Setter != null && (_typeConverter?.CanConvertFrom(typeof(string)) ?? true));

            var getter = _wellKnownProperty.Getter;

            if (_typeConverter != null)
            {
                getter = x => _typeConverter.ConvertTo(_wellKnownProperty.Getter(x), typeof(string)) ?? "(null)";
            }

            _originalValue = getter(o);
            SetValue(_originalValue, false);
            var inpc = o as INotifyPropertyChanged;
            var changed = _wellKnownProperty.Changed(_object) ?? inpc.GetObservable<object>(Name);
            _disposable = changed.Where(_ => !_setActive).Subscribe(_ => SetValue(getter(o) ?? "(null)", false));
        }

        private static Dictionary<Type, string[]> _typespossibleValues = new Dictionary<Type, string[]>();

        private string[] GetPossibleValues()
        {
            string[] result;

            var propertyType = _property?.PropertyType ?? _wellKnownProperty?.Type ?? typeof(object);

            if (_typespossibleValues.TryGetValue(propertyType, out result))
                return result;

            if (_property != null)
            {
                if (propertyType.IsEnum)
                {
                    result = Enum.GetNames(propertyType);
                }
                else if (propertyType == typeof(IBrush))
                {
                    result = typeof(Brushes).GetProperties().Select(p => p.Name).ToArray();
                }
                else if (propertyType == typeof(bool) || propertyType == typeof(bool?))
                {
                    result = new[] { "True", "False" };
                }
                else if (propertyType == typeof(Cursor))
                {
                    result = Enum.GetNames(typeof(StandardCursorType));
                }
            }

            return _typespossibleValues[propertyType] = result ?? Array.Empty<string>();
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

                if (setback && !IsReadOnly && _disposable != null)
                {
                    try
                    {
                        object propValue = string.IsNullOrEmpty(stringValue) || stringValue == "(null)" ?
                                            null : (_typeConverter?.ConvertFrom(stringValue) ?? stringValue);

                        _setActive = true;
                        if (_wellKnownProperty != null)
                        {
                            _wellKnownProperty?.Setter?.Invoke(_object, propValue);
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

        public async Task<IEnumerable<object>> HintValuesPopulator(string text, CancellationToken token)
        {
            if (text.Equals(Value))
                return Array.Empty<string>();

            await Task.Delay(100, token);

            if (token.IsCancellationRequested)
            {
                return Array.Empty<string>();
            }

            return HintValues;
        }

        public void Dispose()
        {
            _disposable?.Dispose();
            _disposable = null;
        }

        public IEnumerable<string> HintValues
        {
            get => _hintValues ?? (_hintValues = GetPossibleValues());
        }
    }
}
