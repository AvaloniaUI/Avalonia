namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Creates a control based on a parameter.
    /// </summary>
    /// <typeparam name="TParam">The type of the parameter.</typeparam>
    /// <typeparam name="TControl">The type of control.</typeparam>
    public interface ITemplate<TParam, TControl>
    {
        /// <summary>
        /// Creates the control.
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <returns>
        /// The created control.
        /// </returns>
        TControl Build(TParam param);
    }
}
