using System;

namespace Avalonia.Data.Core;

internal class IndexerBindingExpression : UntypedBindingExpressionBase
{
    private readonly AvaloniaObject _source;
    private readonly AvaloniaProperty _sourceProperty;
    private readonly AvaloniaObject _target;
    private readonly AvaloniaProperty? _targetProperty;
    private readonly BindingMode _mode;

    public IndexerBindingExpression(
        AvaloniaObject source,
        AvaloniaProperty sourceProperty,
        AvaloniaObject target,
        AvaloniaProperty? targetProperty,
        BindingMode mode)
        : base(BindingPriority.LocalValue)
    {
        _source = source;
        _sourceProperty = sourceProperty;
        _target = target;
        _targetProperty = targetProperty;
        _mode = mode;
    }

    public override string Description => $"IndexerBinding {_sourceProperty})";

    internal override bool WriteValueToSource(object? value)
    {
        _source.SetValue(_sourceProperty, value);
        return true;
    }

    protected override void StartCore()
    {
        if (_mode is BindingMode.TwoWay or BindingMode.OneWayToSource && _targetProperty is not null)
            _target.PropertyChanged += OnTargetPropertyChanged;

        if (_mode is not BindingMode.OneWayToSource)
        {
            _source.PropertyChanged += OnSourcePropertyChanged;
            PublishValue(_source.GetValue(_sourceProperty));
        }

        if (_mode is BindingMode.OneTime)
            Stop();
    }

    protected override void StopCore()
    {
        _source.PropertyChanged -= OnSourcePropertyChanged;
        _target.PropertyChanged -= OnTargetPropertyChanged;
    }

    private void OnSourcePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == _sourceProperty)
            PublishValue(_source.GetValue(_sourceProperty));
    }

    private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == _targetProperty)
            WriteValueToSource(e.NewValue);
    }
}
