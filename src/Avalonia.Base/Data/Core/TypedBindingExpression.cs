using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.PropertyStore;
using Avalonia.Utilities;
using static Avalonia.Rendering.Composition.Animations.PropertySetSnapshot;

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
/// - The source and destination types must be the same, i.e. no type conversion is performed.
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

    internal override void Attach(
        IBindingExpressionSink sink,
        ImmediateValueFrame? frame,
        AvaloniaObject target,
        AvaloniaProperty targetProperty,
        BindingPriority priority)
    {
        if (_sink is not null)
            throw new InvalidOperationException("TypedBindingExpression was already attached.");
        if (target is not StyledElement element)
            throw new InvalidOperationException("TypedBindingExpression may only target StyledElements");
        if (TargetProperty is not null && TargetProperty != targetProperty)
            throw new InvalidOperationException("TypedBindingExpression was already attached to a different property.");

        if (!typeof(TValue).IsAssignableTo(targetProperty.PropertyType))
        {
            throw new InvalidOperationException(
                $"TypedBindingExpression of type '{typeof(TValue)}' cannot be bound " +
                $"to a property of type '{targetProperty.PropertyType}' .");
        }

        _sink = sink;
        _frame = frame;
        _target = new(element);
        TargetProperty = targetProperty;
        Priority = priority;
    }

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
        // TODO: Data validation support.
        state = BindingValueType.Value;
        error = null;
        return false;
    }

    private protected override object? GetUntypedValue()
    {
        Start(produceValue: false);
        if (!_sourceValue.HasValue)
            throw new AvaloniaInternalException("The binding expression has no value.");
        return _sourceValue.Value;
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

    private protected override void Unsubscribe() => StopCore();

    void IWeakEventSubscriber<PropertyChangedEventArgs>.OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e)
    {
        OnSourcePropertyChanged(sender, e);
    }

    private void StartCore()
    {
        if (TryGetTarget(out var target) && TargetProperty is not null)
        {
            target.PropertyChanged += OnTargetPropertyChanged;
            UpdateSource(target.DataContext as TSource);
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

    private void UpdateSource(TSource? source)
    {
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

        _sourceValue = source is null ? default : new(_propertyInfo.Get(source));

        if (_produceValue && _mode is not BindingMode.OneWayToSource)
        {
            if (!_targetValue.HasValue || _targetValue != _sourceValue)
                _sink?.OnChanged(this, true, false);
            if (_mode is BindingMode.OneTime)
                _shouldUpdateOneTimeBindingTarget = false;
        }
    }

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _propertyInfo.Name)
            WriteSourceValueToTarget();
    }

    private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == StyledElement.DataContextProperty)
        {
            UpdateSource(((StyledElement?)sender)?.DataContext as TSource);
        }
        else if (e.Property == TargetProperty)
        {
            _targetValue = e is AvaloniaPropertyChangedEventArgs<TValue> typedArgs ?
                typedArgs.NewValue.Value : (TValue)e.NewValue!;

            if (_mode is BindingMode.TwoWay or BindingMode.OneWayToSource)
                WriteValueToSource(_targetValue.Value);
        }
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
