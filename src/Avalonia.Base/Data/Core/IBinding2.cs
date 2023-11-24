namespace Avalonia.Data.Core;

internal interface IBinding2 : IBinding
{
    BindingExpressionBase Instance(AvaloniaObject target, AvaloniaProperty targetProperty);
}
