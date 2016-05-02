// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.LogicalTree;

namespace Perspex.Controls.Templates
{
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
            if (data == null)
            {
                return null;
            }
            else
            {
                var asControl = data as IControl;

                if (asControl != null)
                {
                    return asControl;
                }
                else
                {
                    IDataTemplate template = control.FindDataTemplate(data);
                    IControl result;

                    if (template != null)
                    {
                        result = template.Build(data);
                    }
                    else
                    {
                        result = FuncDataTemplate.Default.Build(data);
                    }

                    if (result is ISetMaterializedFrom)
                    {
                        ((ISetMaterializedFrom)result).SetMaterializedFrom(template);
                    }

                    if (result is Control)
                    {
                        NameScope.SetNameScope((Control)result, new NameScope());
                    }

                    return result;
                }
            }
        }

        /// <summary>
        /// Find a data template that matches a piece of data.
        /// </summary>
        /// <param name="control">The control searching for the data template.</param>
        /// <param name="data">The data.</param>
        /// <returns>The data template or null if no matching data template was found.</returns>
        public static IDataTemplate FindDataTemplate(this IControl control, object data)
        {
            var templatesMaterializedFrom = control.GetSelfAndLogicalAncestors().OfType<IControl>()
                                                    .Select(c => c.MaterializedFrom).Where(dt => dt != null);
            foreach (var i in control.GetSelfAndLogicalAncestors().OfType<IControl>())
            {
                foreach (IDataTemplate dt in i.DataTemplates.Except(templatesMaterializedFrom))
                {
                    if (dt.Match(data))
                    {
                        return dt;
                    }
                }
            }

            IGlobalDataTemplates global = PerspexLocator.Current.GetService<IGlobalDataTemplates>();

            if (global != null)
            {
                foreach (IDataTemplate dt in global.DataTemplates.Except(templatesMaterializedFrom))
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
