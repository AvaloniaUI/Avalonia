namespace Avalonia.Styling
{
    /// <summary>
    /// Represents the base class for value setters.
    /// </summary>
    public abstract class SetterBase
    {
        internal abstract ISetterInstance Instance(
            IStyleInstance styleInstance,
            StyledElement target);
    }
}
