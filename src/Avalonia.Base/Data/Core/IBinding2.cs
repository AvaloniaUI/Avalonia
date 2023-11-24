namespace Avalonia.Data.Core;

internal interface IBinding2 : IBinding
{
    IBindingExpression Instance(AvaloniaObject target, AvaloniaProperty targetProperty);
}
