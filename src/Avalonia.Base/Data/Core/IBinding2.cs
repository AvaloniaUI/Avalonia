namespace Avalonia.Data.Core;

/// <summary>
/// Internal interface for instancing bindings on an <see cref="AvaloniaObject"/>.
/// </summary>
/// <remarks>
/// TODO12: The presence of this interface is a hack needed because we can't break our API until
/// 12.0. The Instance method would ideally be located as an internal method on a BindingBase
/// class, but we already have a BindingBase in 11.x which is not suitable for this as it contains
/// extra members that are not needed on all of the binding types. The current BindingBase should
/// be renamed to something like BindingMarkupExtensionBase and a new BindingBase created with the
/// Instance method from this interface. This interface should then be removed.
/// </remarks>
internal interface IBinding2 : IBinding
{
    BindingExpressionBase Instance(
        AvaloniaObject target,
        AvaloniaProperty targetProperty,
        object? anchor);
}
