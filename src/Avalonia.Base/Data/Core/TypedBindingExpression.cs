using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
/// - The source and destination types must be the same, i.e. no type conversion is performed.
/// </remarks>
internal class TypedBindingExpression<TSource, TValue> : BindingExpressionBase,
    IDescription,
    IValueEntry<TValue>,
    IWeakEventSubscriber<PropertyChangedEventArgs>
    where TSource : class
{
    private readonly IPropertyInfo<TSource, TValue> _propertyInfo;
    private bool _isRunning;
    private bool _produceValue;
    private IBindingExpressionSink? _sink;
    private WeakReference<TSource?>? _source;
    private WeakReference<StyledElement>? _target;
    private Optional<TValue> _value;

    public TypedBindingExpression(
        IPropertyInfo<TSource, TValue> propertyInfo,
        BindingPriority defaultPriority)
        : base(defaultPriority)
    {
        TargetType = typeof(TValue);
        _propertyInfo = propertyInfo;
    }

    public string Description => _propertyInfo.Name;

    /// <summary>
    /// Gets the target type of the binding expression; that is, the type that values produced by
    /// the expression should be converted to.
    /// </summary>
    public Type TargetType { get; private set; }

    AvaloniaProperty IValueEntry.Property => TargetProperty ?? 
        throw new InvalidOperationException("TypedBindingExpression is not attached.");

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
        _target = new(element);
        TargetProperty = targetProperty;
        TargetType = targetProperty?.PropertyType ?? typeof(object);
        Priority = priority;
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

    bool IValueEntry.GetDataValidationState(out BindingValueType state, out Exception? error)
    {
        // TODO: Data validation support.
        state = BindingValueType.Value;
        error = null;
        return false;
    }

    object? IValueEntry.GetValue()
    {
        Start(produceValue: false);
        return _value.GetValueOrDefault();
    }

    TValue IValueEntry<TValue>.GetValue()
    {
        Start(produceValue: false);
        if (!_value.HasValue)
            throw new AvaloniaInternalException("The binding expression has no value.");
        return _value.Value;
    }

    bool IValueEntry.HasValue()
    {
        Start(produceValue: false);
        return _value.HasValue;
    }

    void IValueEntry.Unsubscribe() => StopCore();

    void IWeakEventSubscriber<PropertyChangedEventArgs>.OnEvent(object? sender, WeakEvent ev, PropertyChangedEventArgs e)
    {
        OnSourcePropertyChanged(sender, e);
    }

    private void StartCore()
    {
        if (TryGetTarget(out var target))
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

        if (source is INotifyPropertyChanged inpc)
            WeakEvents.ThreadSafePropertyChanged.Subscribe(inpc, this);

        UpdateValue(source);
    }

    private void UpdateValue()
    {
        if (TryGetSource(out var source))
            UpdateValue(source);
    }

    private void UpdateValue(TSource? source)
    {
        var oldValue = _value;

        _value = source is null ? default : new(_propertyInfo.Get(source));

        if (_produceValue && oldValue != _value)
            _sink?.OnChanged(this, true, false, _value.GetValueOrDefault(), null);
    }

    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _propertyInfo.Name)
            UpdateValue();
    }

    private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == StyledElement.DataContextProperty)
            UpdateSource(((StyledElement?)sender)?.DataContext as TSource);
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
}
