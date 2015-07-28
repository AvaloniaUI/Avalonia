// -----------------------------------------------------------------------
// <copyright file="DataTemplateExtensions.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Templates
{
    using System.Linq;
    using Perspex.LogicalTree;
    using Splat;

    /// <summary>
    /// Defines extension methods for working with <see cref="IDataTemplate"/>s.
    /// </summary>
    public static class DataTemplateExtensions
    {
        /// <summary>
        /// Materializes a piece of data based on a data template.
        /// </summary>
        /// <param name="control">The control materializing the data template.</param>
        /// <param name="data">The data.</param>
        /// <returns>The data materialized as a control.</returns>
        public static IControl MaterializeDataTemplate(this IControl control, object data)
        {
            IDataTemplate template = control.FindDataTemplate(data);
            IControl result;

            if (template != null)
            {
                result = template.Build(data);

                if (result != null && result.DataContext == null)
                {
                    result.DataContext = data;
                }
            }
            else if (data is IControl)
            {
                result = (IControl)data;
            }
            else
            {
                result = DataTemplate.Default.Build(data);
            }

            return result;
        }

        /// <summary>
        /// Find a data template that matches a piece of data.
        /// </summary>
        /// <param name="control">The control searching for the data template.</param>
        /// <param name="data">The data.</param>
        /// <returns>The data template or null if no matching data template was found.</returns>
        public static IDataTemplate FindDataTemplate(this IControl control, object data)
        {
            foreach (var i in control.GetSelfAndLogicalAncestors().OfType<IControl>())
            {
                foreach (IDataTemplate dt in i.DataTemplates.Reverse())
                {
                    if (dt.Match(data))
                    {
                        return dt;
                    }
                }
            }

            IGlobalDataTemplates global = Locator.Current.GetService<IGlobalDataTemplates>();

            if (global != null)
            {
                foreach (IDataTemplate dt in global.DataTemplates.Reverse())
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
