using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.Parsers;
using Avalonia.UnitTests;
using Avalonia.Utilities;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core;

[InvariantCulture]
public abstract partial class BindingExpressionTests
{
    public partial class Reflection : BindingExpressionTests
    {
        private protected override (TargetClass, BindingExpression) CreateTargetCore<TIn, TOut>(
            Expression<Func<TIn, TOut>> expression,
            AvaloniaProperty targetProperty,
            IValueConverter? converter,
            object? converterParameter,
            object? dataContext,
            bool enableDataValidation,
            Optional<object?> fallbackValue,
            BindingMode mode,
            RelativeSource? relativeSource,
            Optional<TIn> source,
            object? targetNullValue,
            string? stringFormat,
            UpdateSourceTrigger updateSourceTrigger)
        {
            var target = new TargetClass { DataContext = dataContext };
            var (path, resolver) = BindingPathFromExpressionBuilder.Build(expression);
            var fallback = fallbackValue.HasValue ? fallbackValue.Value : AvaloniaProperty.UnsetValue;
            List<ExpressionNode>? nodes = null;

            if (relativeSource is not null && relativeSource.Mode is not RelativeSourceMode.Self)
                throw new NotImplementedException();

            if (!string.IsNullOrEmpty(path))
            {
                var reader = new CharacterReader(path.AsSpan());
                var (astNodes, sourceMode) = BindingExpressionGrammar.Parse(ref reader);
                nodes = ExpressionNodeFactory.CreateFromAst(astNodes, resolver, null, out _);
            }

            if (!source.HasValue && relativeSource is null)
            {
                nodes ??= new();
                nodes.Insert(0, new DataContextNode());
            }

            var bindingExpression = new BindingExpression(
                source.HasValue ? source.Value : target,
                nodes,
                fallback,
                converter: converter,
                converterParameter: converterParameter,
                enableDataValidation: enableDataValidation,
                mode: mode,
                targetNullValue: targetNullValue,
                targetTypeConverter: TargetTypeConverter.GetReflectionConverter(),
                stringFormat: stringFormat,
                updateSourceTrigger: updateSourceTrigger);

            target.GetValueStore().AddBinding(targetProperty, bindingExpression);
            return (target, bindingExpression);
        }
    }

    public partial class Compiled : BindingExpressionTests
    {
        private protected override (TargetClass, BindingExpression) CreateTargetCore<TIn, TOut>(
            Expression<Func<TIn, TOut>> expression,
            AvaloniaProperty targetProperty,
            IValueConverter? converter,
            object? converterParameter,
            object? dataContext,
            bool enableDataValidation,
            Optional<object?> fallbackValue,
            BindingMode mode,
            RelativeSource? relativeSource,
            Optional<TIn> source,
            object? targetNullValue,
            string? stringFormat,
            UpdateSourceTrigger updateSourceTrigger)
        {
            var target = new TargetClass { DataContext = dataContext };
            var nodes = new List<ExpressionNode>();
            var fallback = fallbackValue.HasValue ? fallbackValue.Value : AvaloniaProperty.UnsetValue;
            var path = CompiledBindingPathFromExpressionBuilder.Build(expression, enableDataValidation);

            if (relativeSource is not null && relativeSource.Mode is not RelativeSourceMode.Self)
                throw new NotImplementedException();

            nodes = BindingPath.BuildExpressionNodes(path, source, targetProperty);

            if (!source.HasValue && relativeSource is null)
                (nodes ??= []).Insert(0, new DataContextNode());

            var bindingExpression = new BindingExpression(
                source.HasValue ? source.Value : target,
                nodes,
                fallback,
                converter: converter,
                converterParameter: converterParameter,
                enableDataValidation: enableDataValidation,
                mode: mode,
                targetNullValue: targetNullValue,
                targetTypeConverter: TargetTypeConverter.GetReflectionConverter(),
                stringFormat: stringFormat,
                updateSourceTrigger: updateSourceTrigger);
            target.GetValueStore().AddBinding(targetProperty, bindingExpression);
            return (target, bindingExpression);
        }
    }

    protected TargetClass CreateTarget<TIn, TOut>(
        Expression<Func<TIn, TOut>> expression,
        AvaloniaProperty? targetProperty = null,
        IValueConverter? converter = null,
        object? converterParameter = null,
        object? dataContext = null,
        bool enableDataValidation = false,
        Optional<object?> fallbackValue = default,
        BindingMode mode = BindingMode.OneWay,
        RelativeSource? relativeSource = null,
        Optional<TIn> source = default,
        object? targetNullValue = null,
        string? stringFormat = null)
            where TIn : class?
    {
        var (target, _) = CreateTargetAndExpression(
            expression,
            targetProperty,
            converter,
            converterParameter,
            dataContext,
            enableDataValidation,
            fallbackValue,
            mode,
            relativeSource,
            source,
            targetNullValue,
            stringFormat);
        return target;
    }

    protected TargetClass CreateTargetWithSource<TIn, TOut>(
        TIn source,
        Expression<Func<TIn, TOut>> expression,
        AvaloniaProperty? targetProperty = null,
        IValueConverter? converter = null,
        object? converterParameter = null,
        bool enableDataValidation = false,
        Optional<object?> fallbackValue = default,
        BindingMode mode = BindingMode.OneWay,
        RelativeSource? relativeSource = null,
        object? targetNullValue = null,
        string? stringFormat = null,
        UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.PropertyChanged)
            where TIn : class?
    {
        var (target, _) = CreateTargetAndExpression(
            expression,
            targetProperty,
            converter,
            converterParameter,
            null,
            enableDataValidation,
            fallbackValue,
            mode,
            relativeSource,
            source,
            targetNullValue,
            stringFormat,
            updateSourceTrigger);
        return target;
    }

    private protected (TargetClass, BindingExpression) CreateTargetAndExpression<TIn, TOut>(
        Expression<Func<TIn, TOut>> expression,
        AvaloniaProperty? targetProperty = null,
        IValueConverter? converter = null,
        object? converterParameter = null,
        object? dataContext = null,
        bool enableDataValidation = false,
        Optional<object?> fallbackValue = default,
        BindingMode mode = BindingMode.OneWay,
        RelativeSource? relativeSource = null,
        Optional<TIn> source = default,
        object? targetNullValue = null,
        string? stringFormat = null,
        UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.PropertyChanged)
            where TIn : class?
    {
        targetProperty ??= typeof(TOut) switch
        {
            var t when t == typeof(bool) => TargetClass.BoolProperty,
            var t when t == typeof(double) => TargetClass.DoubleProperty,
            var t when t == typeof(int) => TargetClass.IntProperty,
            var t when t == typeof(string) => TargetClass.StringProperty,
            _ => TargetClass.ObjectProperty,
        };

        return CreateTargetCore(
            expression,
            targetProperty,
            converter,
            converterParameter,
            dataContext,
            enableDataValidation,
            fallbackValue,
            mode,
            relativeSource,
            source,
            targetNullValue,
            stringFormat,
            updateSourceTrigger);
    }

    private protected abstract (TargetClass, BindingExpression) CreateTargetCore<TIn, TOut>(
        Expression<Func<TIn, TOut>> expression,
        AvaloniaProperty targetProperty,
        IValueConverter? converter,
        object? converterParameter,
        object? dataContext,
        bool enableDataValidation,
        Optional<object?> fallbackValue,
        BindingMode mode,
        RelativeSource? relativeSource,
        Optional<TIn> source,
        object? targetNullValue,
        string? stringFormat,
        UpdateSourceTrigger updateSourceTrigger)
            where TIn : class?;

    private static IDisposable StartWithFocusSupport()
    {
        return UnitTestApplication.Start(TestServices.RealFocus);
    }

    protected class ViewModel : NotifyingBase
    {
        private bool _boolValue;
        private double _doubleValue;
        private int _intValue;
        private object? _objectValue;
        private string? _stringValue;
        private ViewModel? _next;
        private IObservable<ViewModel>? _nextObservable;
        private Task<ViewModel>? _nextTask;

        public bool BoolValue
        {
            get => _boolValue;
            set { _boolValue = value; RaisePropertyChanged(); }
        }

        public int IntValue
        {
            get => _intValue;
            set { _intValue = value; RaisePropertyChanged(); }
        }

        public double DoubleValue
        {
            get => _doubleValue;
            set { _doubleValue = value; RaisePropertyChanged(); }
        }

        public object? ObjectValue
        {
            get => _objectValue;
            set { _objectValue = value; RaisePropertyChanged(); }
        }

        public string? StringValue
        {
            get => _stringValue;
            set { _stringValue = value; RaisePropertyChanged(); }
        }

        public ViewModel? Next
        {
            get => _next;
            set { _next = value; RaisePropertyChanged(); }
        }

        public IObservable<ViewModel>? NextObservable
        {
            get => _nextObservable;
            set { _nextObservable = value; RaisePropertyChanged(); }
        }

        public Task<ViewModel> NextTask
        {
            get => _nextTask!;
            set { _nextTask = value; RaisePropertyChanged(); }
        }

        public void SetStringValueWithoutRaising(string value) => _stringValue = value;
    }

    protected class PodViewModel
    {
        public string? StringValue { get; set; }
    }

    protected class AttachedProperties
    {
        public static readonly AttachedProperty<string?> AttachedStringProperty =
            AvaloniaProperty.RegisterAttached<AttachedProperties, AvaloniaObject, string?>("AttachedString");
    }

    protected class SourceControl : Control
    {
        public static readonly StyledProperty<SourceControl?> NextProperty =
            AvaloniaProperty.Register<SourceControl, SourceControl?>("Next");
        public static readonly StyledProperty<string?> StringValueProperty =
            AvaloniaProperty.Register<SourceControl, string?>("StringValue");

        public SourceControl? Next
        {
            get => GetValue(NextProperty);
            set => SetValue(NextProperty, value);
        }

        public string? StringValue
        {
            get => GetValue(StringValueProperty);
            set => SetValue(StringValueProperty, value);
        }

        public string? ClrProperty { get; set; }
    }

    protected class TargetClass : Control
    {
        public static readonly StyledProperty<bool> BoolProperty =
            AvaloniaProperty.Register<TargetClass, bool>("Bool");
        public static readonly StyledProperty<double> DoubleProperty =
            AvaloniaProperty.Register<TargetClass, double>("Double");
        public static readonly StyledProperty<int> IntProperty =
            AvaloniaProperty.Register<TargetClass, int>("Int");
        public static readonly StyledProperty<object?> ObjectProperty =
            AvaloniaProperty.Register<TargetClass, object?>("Object");
        public static readonly StyledProperty<string?> StringProperty =
            AvaloniaProperty.Register<TargetClass, string?>("String");
        public static readonly DirectProperty<TargetClass, string?> ReadOnlyStringProperty =
            AvaloniaProperty.RegisterDirect<TargetClass, string?>(
                nameof(ReadOnlyString),
                o => o.ReadOnlyString);

        private string? _readOnlyString = "readonly";

        static TargetClass()
        {
            FocusableProperty.OverrideDefaultValue<TargetClass>(true);
        }

        public bool Bool
        {
            get => GetValue(BoolProperty);
            set => SetValue(BoolProperty, value);
        }

        public double Double
        {
            get => GetValue(DoubleProperty);
            set => SetValue(DoubleProperty, value);
        }

        public int Int
        {
            get => GetValue(IntProperty);
            set => SetValue(IntProperty, value);
        }

        public object? Object
        {
            get => GetValue(ObjectProperty);
            set => SetValue(ObjectProperty, value);
        }

        public string? String
        {
            get => GetValue(StringProperty);
            set => SetValue(StringProperty, value);
        }

        public string? ReadOnlyString
        {
            get => _readOnlyString;
            private set => SetAndRaise(ReadOnlyStringProperty, ref _readOnlyString, value);
        }

        public Dictionary<AvaloniaProperty, BindingNotification> BindingNotifications { get; } = new();

        public override string ToString() => nameof(TargetClass);

        public void SetReadOnlyString(string? value) => ReadOnlyString = value;

        protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
        {
            base.UpdateDataValidation(property, state, error);

            var type = state switch
            {
                BindingValueType b when b.HasFlag(BindingValueType.BindingError) => BindingErrorType.Error,
                BindingValueType b when b.HasFlag(BindingValueType.DataValidationError) => BindingErrorType.DataValidationError,
                _ => BindingErrorType.None,
            };

            if (type == BindingErrorType.None || error is null)
                BindingNotifications.Remove(property);
            else
                BindingNotifications[property] = new BindingNotification(error, type);
        }
    }

    protected class PrefixConverter : IValueConverter
    {
        public PrefixConverter(string? prefix = null) => Prefix = prefix;

        public string? Prefix { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (targetType != typeof(string))
                return value;

            var result = value?.ToString() ?? string.Empty;
            var prefix = parameter?.ToString() ?? Prefix;

            if (prefix is not null)
                result = prefix + result;
            return result;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (targetType != typeof(string) || parameter?.ToString() is not string prefix)
                return value;

            var s = value?.ToString() ?? string.Empty;
            
            if (s.StartsWith(prefix))
                return s.Substring(prefix.Length);
            else
                return value;
        }
    }
}
