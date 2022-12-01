using Avalonia.LogicalTree;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Defines extension methods for working with <see cref="IDataTemplate"/>s.
    /// </summary>
    public static class DataTemplateExtensions
    {
        /// <summary>
        /// Find a data template that matches a piece of data.
        /// </summary>
        /// <param name="control">The control searching for the data template.</param>
        /// <param name="data">The data.</param>
        /// <param name="primary">
        /// An optional primary template that can will be tried before the DataTemplates in the
        /// tree are searched.
        /// </param>
        /// <returns>The data template or null if no matching data template was found.</returns>
        public static IDataTemplate? FindDataTemplate(
            this Control control,
            object? data,
            IDataTemplate? primary = null)
        {
            if (primary?.Match(data) == true)
            {
                return primary;
            }

            var currentTemplateHost = control as ILogical;

            while (currentTemplateHost != null)
            {
                if (currentTemplateHost is IDataTemplateHost hostCandidate && hostCandidate.IsDataTemplatesInitialized)
                {
                    foreach (IDataTemplate dt in hostCandidate.DataTemplates)
                    {
                        if (dt.Match(data))
                        {
                            return dt;
                        }
                    }
                }

                currentTemplateHost = currentTemplateHost.LogicalParent;
            }

            var global = AvaloniaLocator.Current.GetService<IGlobalDataTemplates>();

            if (global != null && global.IsDataTemplatesInitialized)
            {
                foreach (IDataTemplate dt in global.DataTemplates)
                {
                    if (dt.Match(data))
                    {
                        return dt;
                    }
                }
            }

            return null;
        }
    }
}
