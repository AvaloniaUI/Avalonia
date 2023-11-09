using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.PropertyStore;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Data.Core;

/// <summary>
/// Base class for binding expressions which produce untyped values.
/// </summary>
internal abstract class UntypedBindingExpressionBase : BindingExpressionBase, IDisposable, IValueEntry
{
    protected static readonly object UnchangedValue = new();
    internal static readonly WeakReference<object?> _nullReference = new(null);
    private readonly bool _isDataValidationEnabled;
    private object? _defaultValue;
    private BindingError? _error;
    private bool _isDefaultValueInitialized;
    private bool _isRunning;
    private bool _produceValue;
    private IBindingExpressionSink? _sink;
    private WeakReference<AvaloniaObject?>? _target;
    private WeakReference<object?>? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="UntypedBindingExpressionBase"/> class.
    /// </summary>
    /// <param name="isDataValidationEnabled">Whether data validation is enabled.</param>
    public UntypedBindingExpressionBase(bool isDataValidationEnabled)
    {
        _isDataValidationEnabled = isDataValidationEnabled;
    }

    /// <summary>
    /// Gets the current error state of the binding expression.
    /// </summary>
    public BindingErrorType ErrorType => _error?.ErrorType ?? BindingErrorType.None;

    /// <summary>
    /// Gets a value indicating whether data validation is enabled for the binding expression.
    /// </summary>
    public bool IsDataValidationEnabled => _isDataValidationEnabled;

    /// <summary>
    /// Gets a value indicating whether the binding expression is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Gets the priority of the binding expression.
    /// </summary>
    public BindingPriority Priority { get; private set; }

    /// <summary>
    /// Gets the <see cref="AvaloniaProperty"/> which the binding expression is targeting.
    /// </summary>
    public AvaloniaProperty? TargetProperty { get; private set; }

    /// <summary>
    /// Gets the target type of the binding expression; that is, the type that values produced by
    /// the expression should be converted to.
    /// </summary>
    public Type TargetType { get; private set; } = typeof(object);

    bool IValueEntry.HasValue
    {
        get
        {
            Start(produceValue: false);
            return _value is not null;
        }
    }

    AvaloniaProperty IValueEntry.Property => TargetProperty ?? throw new Exception();

    /// <summary>
    /// Produces an observable which can be used to observe the value of the binding expression.
    /// </summary>
    /// <returns>An observable subject.</returns>
    /// <exception cref="InvalidOperationException">
    /// The binding expression is already instantiated on an AvaloniaObject.
    /// </exception>
    /// <remarks>
    /// This method is mostly here for backwards compatibility with <see cref="InstancedBinding"/>
    /// and unit testing and we may want to remove it in future. In particular its usefulness in
    /// terms of unit testing is limited in that it preserves the semantics of binding expressions
    /// as expected by unit tests, not necessarily the semantics that will be used when the
    /// expression is used as an <see cref="IValueEntry"/> instantiated in a
    /// <see cref="ValueStore"/>. Unit tests should be migrated to not test the behaviour of
    /// binding expressions through an observable, and instead test the behaviour of the binding
    /// when applied to an <see cref="AvaloniaObject"/>.
    /// 
    /// A binding expression may only act as an observable or as a binding expression targeting an
    /// AvaloniaObject, not both.
    /// </remarks>
    public IAvaloniaSubject<object?> ToObservable()
    {
        if (_sink is ObservableSink s)
            return s;
        if (_sink is not null)
            throw new InvalidOperationException(
                "Cannot call AsObservable on a to binding expression which is already " +
                "instantiated on an AvaloniaObject.");

        var o = new ObservableSink(this);
        _sink = o;
        return o;
    }

    /// <summary>
    /// Produces an observable which can be used to observe the value of the binding expression
    /// for unit testing.
    /// </summary>
    /// <param name="targetType">The <see cref="TargetType"/>.</param>
    /// <returns>An observable subject.</returns>
    /// <exception cref="InvalidOperationException">
    /// The binding expression is already instantiated on an AvaloniaObject.
    /// </exception>
    /// <remarks>
    /// This method should be considered obsolete and new unit tests should not be written to use
    /// it. For more information see <see cref="ToObservable()"/>.
    /// </remarks>
    public IAvaloniaSubject<object?> ToObservable(Type targetType)
    {
        var o = ToObservable();
        TargetType = targetType;
        return o;
    }

    /// <summary>
    /// Terminates the binding.
    /// </summary>
    public virtual void Dispose()
    {
        if (_sink is null)
            return;

        Stop();

        var sink = _sink;
        _sink = null;
        sink.OnCompleted(this);
    }

    /// <summary>
    /// Gets the current value of the binding expression.
    /// </summary>
    /// <returns>
    /// The current value or <see cref="AvaloniaProperty.UnsetValue"/> if the binding was unable
    /// to read a value.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The binding expression has not been started.
    /// </exception>
    public object? GetValue()
    {
        if (!IsRunning)
            throw new InvalidOperationException("BindingExpression has not been started.");
        if (_value is null)
            return AvaloniaProperty.UnsetValue;
        else if (_value == _nullReference)
            return null;
        else if (_value.TryGetTarget(out var value))
            return value;
        else
            return AvaloniaProperty.UnsetValue;
    }

    /// <summary>
    /// Gets the current value of the binding expression or the default value for the target property.
    /// </summary>
    /// <returns>
    /// The current value or the target property default.
    /// </returns>
    public object? GetValueOrDefault()
    {
        var result = GetValue();
        if (result == AvaloniaProperty.UnsetValue)
            result = GetCachedDefaultValue();
        return result;
    }

    /// <summary>
    /// Attaches the binding expression to a subscriber with the specified subscriber but does not
    /// start it.
    /// </summary>
    /// <param name="sink">The subscriber.</param>
    /// <param name="target">The target object.</param>
    /// <param name="targetProperty">The target property.</param>
    /// <param name="priority">The priority of the binding.</param>
    public void Attach(
        IBindingExpressionSink sink,
        AvaloniaObject target,
        AvaloniaProperty targetProperty,
        BindingPriority priority)
    {
        if (_sink is not null)
            throw new InvalidOperationException("BindingExpression was already instantiated.");

        _sink = sink;
        _target = new(target);
        TargetProperty = targetProperty;
        TargetType = targetProperty.PropertyType;
        Priority = priority;
    }

    /// <summary>
    /// Initializes the binding expression with the specified subscriber and target property and
    /// starts it.
    /// </summary>
    /// <param name="subscriber">The subscriber.</param>
    /// <param name="target">The target object.</param>
    /// <param name="targetProperty">The target property.</param>
    /// <param name="priority">The priority of the binding.</param>
    public void Start(
        IBindingExpressionSink subscriber,
        AvaloniaObject target,
        AvaloniaProperty targetProperty,
        BindingPriority priority)
    {
        Attach(subscriber, target, targetProperty, priority);
        Start(produceValue: true);
    }

    /// <summary>
    /// When overridden in a derived class, writes the specified value to the binding source if
    /// possible.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <returns>
    /// True if the value could be written to the binding source; otherwise false.
    /// </returns>
    public abstract bool WriteValueToSource(object? value);

    bool IValueEntry.GetDataValidationState(out BindingValueType state, out Exception? error)
    {
        if (_error is not null)
        {
            state = _error.ErrorType switch
            {
                BindingErrorType.Error => BindingValueType.BindingError,
                BindingErrorType.DataValidationError => BindingValueType.DataValidationError,
                _ => throw new InvalidOperationException("Invalid BindingErrorType."),
            };
            error = _error.Exception;
        }
        else
        {
            state = BindingValueType.Value;
            error = null;
        }

        return IsDataValidationEnabled;
    }

    object? IValueEntry.GetValue()
    {
        Start(produceValue: false);
        return GetValueOrDefault();
    }

    void IValueEntry.Unsubscribe() => Stop();

    /// <summary>
    /// When overridden in a derived class, starts the binding expression.
    /// </summary>
    /// <remarks>
    /// This method should not be called directly; instead call <see cref="Start(bool)"/>.
    /// </remarks>
    protected abstract void StartCore();

    /// <summary>
    /// When overridden in a derived class, stops the binding expression.
    /// </summary>
    /// <remarks>
    /// This method should not be called directly; instead call <see cref="Stop"/>.
    /// </remarks>
    protected abstract void StopCore();

    /// <summary>
    /// Publishes a new value and/or error state to the target.
    /// </summary>
    /// <param name="value">The new value, or <see cref="UnchangedValue"/>.</param>
    /// <param name="error">The new binding or data validation error.</param>
    protected void PublishValue(object? value, BindingError? error = null)
    {
        // When binding to DataContext and the expression results in a binding error, the binding
        // expression should produce null rather than UnsetValue in order to not propagate
        // incorrect DataContexts from parent controls while things are being set up.
        if (TargetProperty == StyledElement.DataContextProperty &&
            value == AvaloniaProperty.UnsetValue &&
            error?.ErrorType == BindingErrorType.Error)
        {
            value = null;
        }

        var hasValueChanged = value != UnchangedValue && !TypeUtilities.IdentityEquals(value, GetValue(), TargetType);
        var hasErrorChanged = error is not null || _error is not null;

        if (hasValueChanged)
            _value = value is null ? _nullReference : new(value);
        _error = error;

        if (!_produceValue || _sink is null)
            return;

        if (Dispatcher.UIThread.CheckAccess())
        {
            _sink.OnChanged(this, hasValueChanged, hasErrorChanged, GetValueOrDefault(), _error);
        }
        else
        {
            // To avoid allocating closure in the outer scope we need to capture variables
            // locally. This allows us to skip most of the allocations when on UI thread.
            var sink = _sink;
            var vc = hasValueChanged;
            var ec = hasErrorChanged;
            var v = GetValueOrDefault();
            var e = _error;
            Dispatcher.UIThread.Post(() => sink.OnChanged(this, vc, ec, v, e));
        }
    }

    /// <summary>
    /// Starts the binding expression by calling <see cref="StartCore"/>.
    /// </summary>
    /// <param name="produceValue">
    /// Indicates whether the binding expression should produce an initial value.
    /// </param>
    protected void Start(bool produceValue)
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

    /// <summary>
    /// Stops the binding expression by calling <see cref="StopCore"/>.
    /// </summary>
    protected void Stop()
    {
        StopCore();
        _isRunning = false;
        _value = null;
    }

    /// <summary>
    /// Tries to retrieve the target for the binding expression.
    /// </summary>
    /// <param name="target">
    /// When this method returns, contains the target object, if it is available.
    /// </param>
    /// <returns>true if the target was retrieved; otherwise, false.</returns>
    protected bool TryGetTarget([NotNullWhen(true)] out AvaloniaObject? target)
    {
        if (_target is not null)
            return _target.TryGetTarget(out target);
        target = null!;
        return false;
    }

    private object? GetCachedDefaultValue()
    {
        if (_isDefaultValueInitialized == true)
            return _defaultValue;

        if (TargetProperty is not null && _target?.TryGetTarget(out var target) == true)
        {
            if (TargetProperty.IsDirect)
                _defaultValue = ((IDirectPropertyAccessor)TargetProperty).GetUnsetValue(target.GetType());
            else
                _defaultValue = ((IStyledPropertyAccessor)TargetProperty).GetDefaultValue(target.GetType());

            _isDefaultValueInitialized = true;
            return _defaultValue;
        }

        return AvaloniaProperty.UnsetValue;
    }

    private sealed class ObservableSink : LightweightObservableBase<object?>,
        IBindingExpressionSink,
        IAvaloniaSubject<object?>
    {
        private readonly UntypedBindingExpressionBase _expression;
        private WeakReference<object?>? _value;

        public ObservableSink(UntypedBindingExpressionBase expression) => _expression = expression;

        void IBindingExpressionSink.OnChanged(
            UntypedBindingExpressionBase instance,
            bool hasValueChanged,
            bool hasErrorChanged,
            object? value,
            BindingError? error)
        {
            if (instance.IsDataValidationEnabled || error is not null)
            {
                BindingNotification notification;

                if (error?.ErrorType == BindingErrorType.Error)
                    notification = new(error.Exception, BindingErrorType.Error, value);
                else if (error?.ErrorType == BindingErrorType.DataValidationError)
                    notification = new(error.Exception, BindingErrorType.DataValidationError, value);
                else
                    notification = new(value);

                PublishNext(notification);
            }
            else if (hasValueChanged)
            {
                PublishNext(value);
            }
        }

        void IBindingExpressionSink.OnCompleted(UntypedBindingExpressionBase instance) => PublishCompleted();

        void IObserver<object?>.OnCompleted() { }
        void IObserver<object?>.OnError(Exception error) { }
        void IObserver<object?>.OnNext(object? value) => _expression.WriteValueToSource(value);

        protected override void Initialize() => _expression.Start(produceValue: true);
        protected override void Deinitialize() => _expression.Stop();

        protected override void Subscribed(IObserver<object> observer, bool first)
        {
            if (!first && _value is not null)
            {
                if (_value == _nullReference)
                    base.PublishNext(null);
                else if (_value.TryGetTarget(out var value))
                    base.PublishNext(value);
            }
        }

        private new void PublishNext(object? value)
        {
            _value = (value is null) ? _nullReference : new(value);
            base.PublishNext(value);
        }
    }
}
