using Avalonia.Collections;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// A collection of <see cref="IDataTemplate"/>s.
    /// </summary>
    public class DataTemplates : AvaloniaList<IDataTemplate>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataTemplates"/> class.
        /// </summary>
        public DataTemplates()
        {
            ResetBehavior = ResetBehavior.Remove;
        }
    }
}