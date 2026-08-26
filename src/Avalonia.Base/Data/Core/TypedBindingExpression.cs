using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Logging;
using Avalonia.PropertyStore;
using Avalonia.Utilities;

namespace Avalonia.Data.Core;

/// <summary>
/// A binding expression which does not box.
/// </summary>
/// <typeparam name="TSource">The type of the source object.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
/// <remarks>
/// A typed binding expression has the following limitations:
/// 
/// - It must be a DataContext binding
/// - It can only be used with a single source property, i.e. `{Binding Foo}` can be represented as
///   a typed binding but `{Binding Foo.Bar}` cannot.
/// - It cannot have a Converter, Delay, FallbackValue. StringFormat, TargetNullValue or
///   UpdateSourceTrigger != PropertyChanged.
/// - The value must be directly assignable to the target property, i.e. no type conversion is
///   performed.
/// - The target property must not enable data validation.
/// </remarks>
internal class TypedBindingExpression<TSource, TValue> : BindingExpressionBase,
    IDescription,
    IValueEntry<TValue>,
    IWeakEventSubscriber<PropertyChangedEventArgs>
    where TSource : class
{
    private readonly IPropertyInfo<TSource, TValue> _propertyInfo;
    private readonly BindingMode _mode;
    private bool _isRunning;
    private bool _produceValue;
    private bool _writingValueToTarget;
    private IBindingExpressionSink? _sink;
    private ImmediateValueFrame? _frame;
    private WeakReference<TSource?>? _source;
    private WeakReference<StyledElement>? _target;
    private Optional<TValue> _sourceValue;
    private Optional<TValue> _targetValue;
    private bool _shouldUpdateOneTimeBindingTarget;

    public TypedBindingExpression(
        IPropertyInfo<TSource, TValue> propertyInfo,
        BindingMode mode,
        BindingPriority defaultPriority)
        : base(defaultPriority)
    {
        _propertyInfo = propertyInfo;
        _mode = mode;
        _shouldUpdateOneTimeBindingTarget = mode is BindingMode.OneTime;
    }

    public string Description => _propertyInfo.Name;

    // The whole method body is factored out into AttachCore, which takes the value type as a
    // Type parameter instead of reading it from TValue. Because AttachCore doesn't reference the
    // generic parameters, its code is shared across all generic instantiations instead of being
    // duplicated for each one, which is a meaningful NativeAOT size saving.
    internal override void Attach(
        IBindingExpressionSink sink,
        ImmediateValueFrame? frame,
        AvaloniaObject target,
        AvaloniaProperty targetProperty,
        BindingPriority priority)
        => AttachCore(sink, frame, target, targetProperty, priority, typeof(TValue));

    public override void Dispose()
    {
        if (_sink is null)
            return;

        // Null the sink before stopping so that the unsubscribe doesn't push a final value to a
        // value store that's about to clear this entry anyway.
        var sink = _sink;
        var frame = _frame;
        _sink = null;
        _frame = null;

        StopCore();

        sink.OnCompleted(this);
        frame?.OnEntryDisposed(this);
    }

    internal override void Start(bool produceValue)
    {
        if (_isRunning)
            return;

        _isRunning = true;

        try
        {
            _produceValue = produceValue;
            StartCore();
        }
        finally
        {
            _produceValue = true;
        }
    }

    private protected override bool GetDataValidationState(out BindingValueType state, out Exception? error)
    {
        // Data validation is not supported by the typed expression: bindings whose target
        // property enables it are excluded in CompiledBinding.CanUseTypedBindingExpression and
        // use the untyped BindingExpression instead. Could be implemented here as a follow-up.
        state = BindingValueType.Value;
        error = null;
        return false;
    }

    private protected override object? GetUntypedValue()
    {
        Start(produceValue: false);
        if (!_sourceValue.HasValue)
            throw new AvaloniaInternalException("The binding expression has no value.");
        return Box(_sourceValue.Value);
    }

    TValue IValueEntry<TValue>.GetValue()
    {
        Start(produceValue: false);
        if (!_sourceValue.HasValue)
            throw new AvaloniaInternalException("The binding expression has no value.");
        return _sourceValue.Value;
    }

    private protected override bool HasValue()
    {
        Start(produceValue: false);
        return _sourceValue.HasValue;
    }

    private protected override void Unsubscribe()
    {
        // Reset _isRunning so the expression can be restarted (and re-subscribe to its source) if
        // the value store reactivates this entry later.
        StopCore();
        _isRunning = false;
    }

    void IWeakEventSubscriber<PropertyChangedEventArgs>.OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e)
    {
        OnSourcePropertyChanged(sender, e);
    }

    private void AttachCore(
        IBindingExpressionSink sink,
        ImmediateValueFrame? frame,
        AvaloniaObject target,
        AvaloniaProperty targetProperty,
        BindingPriority priority,
        Type valueType)
    {
        if (_sink is not null)
            throw new InvalidOperationException("TypedBindingExpression was already attached.");
        if (target is not StyledElement element)
            throw new InvalidOperationException("TypedBindingExpression may only target StyledElements");
        if (TargetProperty is not null && TargetProperty != targetProperty)
            throw new InvalidOperationException("TypedBindingExpression was already attached to a different property.");

        if (!valueType.IsAssignableTo(targetProperty.PropertyType))
        {
            throw new InvalidOperationException(
                $"TypedBindingExpression of type '{valueType}' cannot be bound " +
                $"to a property of type '{targetProperty.PropertyType}'.");
        }

        _sink = sink;
        _frame = frame;
        _target = new(element);
        TargetProperty = targetProperty;
        Priority = priority;
    }

    private void StartCore()
    {
        if (TryGetTarget(out var target) && TargetProperty is not null)
        {
            target.PropertyChanged += OnTargetPropertyChanged;
            UpdateSource(target.DataContext);
        }
    }

    private void StopCore()
    {
        if (TryGetTarget(out var target))
        {
            target.PropertyChanged -= OnTargetPropertyChanged;
            UpdateSource(null);
        }
    }

    private void UpdateSource(object? dataContext)
    {
        var source = dataContext as TSource;

        if (dataContext is not null && source is null)
        {
            Log($"Could not convert DataContext of type '{dataContext.GetType()}' " +
                $"to '{typeof(TSource)}'.");
        }

        if (TryGetSource(out var oldSource))
        {
            if (oldSource is INotifyPropertyChanged oldInpc)
                WeakEvents.ThreadSafePropertyChanged.Unsubscribe(oldInpc, this);
        }

        _source = new(source);
        _shouldUpdateOneTimeBindingTarget = true;

        if (source is INotifyPropertyChanged inpc)
            WeakEvents.ThreadSafePropertyChanged.Subscribe(inpc, this);

        if (_mode is BindingMode.OneWayToSource)
        {
            if (TryGetTargetValue(out var value))
                WriteValueToSource(value!);
        }
        else
        {
            WriteSourceValueToTarget(source);
        }
    }

    private void WriteValueToSource(TValue value)
    {
        if (TargetProperty is not null && TryGetTarget(out var target))
        {
            if (TryGetSource(out var source))
                _propertyInfo.Set(source, value);
        }
    }

    private void WriteSourceValueToTarget()
    {
        if (TryGetSource(out var source))
            WriteSourceValueToTarget(source);
    }

    private void WriteSourceValueToTarget(TSource? source)
    {
        if (_mode is BindingMode.OneTime && !_shouldUpdateOneTimeBindingTarget)
            return;

        var oldValue = _sourceValue;

        if (source is null)
        {
            _sourceValue = default;
        }
        else
        {
            try
            {
                _sourceValue = new(_propertyInfo.Get(source));
            }
            catch (Exception e)
            {
                // Getter exceptions must not escape into the source's PropertyChanged event and
                // crash the UI thread, so log the error and clear the value, as the untyped
                // binding path does.
                Log($"Error getting '{_propertyInfo.Name}': {e.Message}");
                _sourceValue = default;
            }
        }

        if (_produceValue && _mode is not BindingMode.OneWayToSource)
        {
            // An expression which has no value, and had no value before, must not notify: doing so
            // would push the target property's default value, overriding values from styles or
            // property inheritance. Otherwise always notify, even if the value is unchanged, as
            // the target may hold an uncommitted value written by SetCurrentValue.
            if (oldValue.HasValue || _sourceValue.HasValue)
            {
                // Flag that we're pushing the source value to the target so that the resulting
                // target PropertyChanged isn't echoed straight back to the source in TwoWay mode.
                _writingValueToTarget = true;
                try
                {
                    _sink?.OnChanged(this, true, false);
                }
                finally
                {
                    _writingValueToTarget = false;
                }
            }
            if (_mode is BindingMode.OneTime)
                _shouldUpdateOneTimeBindingTarget = false;
        }
    }

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // A null or empty PropertyName means "all properties changed" per the
        // INotifyPropertyChanged contract, so we must re-read the source value in that case too.
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == _propertyInfo.Name)
            WriteSourceValueToTarget();
    }

    private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == StyledElement.DataContextProperty)
        {
            UpdateSource(((StyledElement?)sender)?.DataContext);
        }
        else if (e.Property == TargetProperty)
        {
            _targetValue = ReadTargetValue(e);

            // Don't write back to the source if this change is the binding pushing the source
            // value to the target; that would be a redundant round-trip.
            if (_targetValue.HasValue &&
                !_writingValueToTarget &&
                _mode is BindingMode.TwoWay or BindingMode.OneWayToSource)
            {
                WriteValueToSource(_targetValue.Value);
            }
        }
    }

    private static Optional<TValue> ReadTargetValue(AvaloniaPropertyChangedEventArgs e)
    {
        // The binding value type only needs to be assignable to the target property type, so the
        // target property can hold values which cannot be represented as a TValue; for example a
        // string binding on an object-typed property whose value is set to an int. Such values
        // are reported as absent rather than throwing.
        if (e is AvaloniaPropertyChangedEventArgs<TValue> typedArgs)
            return typedArgs.NewValue.Value;
        if (e.NewValue is TValue value)
            return value;
        if (e.NewValue is null && default(TValue) is null)
            return new Optional<TValue>(default!);
        return default;
    }

    /// <summary>
    /// Converts a value to <see cref="object"/>, using cached boxes for booleans so that reading
    /// a boolean-valued binding into an object-typed target property does not allocate on every
    /// read (#21065).
    /// </summary>
    private static object? Box(TValue value)
    {
        if (typeof(TValue) == typeof(bool))
            return BooleanBoxes.Box(Unsafe.As<TValue, bool>(ref value));
        return value;
    }

    private void Log(string error, LogEventLevel level = LogEventLevel.Warning)
    {
        if (!Logger.TryGet(level, LogArea.Binding, out var log) || !TryGetTarget(out var target))
            return;

        log.Log(
            target,
            "An error occurred binding {Property} to {Expression}: {Message}",
            (object?)TargetProperty ?? "(unknown)",
            Description,
            error);
    }

    private bool TryGetSource([NotNullWhen(true)] out TSource? source)
    {
        if (_source?.TryGetTarget(out source) == true)
            return true;
        source = null;
        return false;
    }

    private bool TryGetTarget([NotNullWhen(true)] out StyledElement? target)
    {
        if (_target?.TryGetTarget(out target) == true)
            return true;
        target = null;
        return false;
    }

    private bool TryGetTargetValue(out TValue? value)
    {
        if (TargetProperty is not null && TryGetTarget(out var target))
        {
            value = TargetProperty switch
            {
                StyledProperty<TValue> s => target.GetValue(s),
                DirectPropertyBase<TValue> d => target.GetValue(d),
                _ => (TValue)target.GetValue(TargetProperty)!
            };
            return true;
        }

        value = default;
        return false;
    }
}
