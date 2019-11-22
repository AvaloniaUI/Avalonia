namespace Avalonia
{
    public interface IDataContext : IAvaloniaObject
    {
        /// <summary>
        /// Gets or sets the control's data context.
        /// </summary>
        object DataContext { get; set; }
    }
}
