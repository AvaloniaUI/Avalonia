namespace Avalonia.Data
{
    /// <summary>
    /// Interfacce that receives typed notifications about property changes on an
    /// <see cref="IAvaloniaObject"/>.
    /// </summary>
    public interface IAvaloniaPropertyListener
    {
        /// <summary>
        /// Called when a property changes.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="change">The property change details.</param>
        void PropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change);
    }
}
