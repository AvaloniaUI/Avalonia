namespace Avalonia.Collections;

internal interface IAvaloniaListItemValidator<T>
{
    void Validate(T item);
}
