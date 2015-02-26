// -----------------------------------------------------------------------
// <copyright file="DataTemplateExtensions.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System.Linq;
    using Perspex.LogicalTree;
    using Splat;

    public static class DataTemplateExtensions
    {
        public static Control MaterializeDataTemplate(this Control control, object data)
        {
            IDataTemplate template = control.FindDataTemplate(data);
            Control result;

            if (template != null)
            {
                result = template.Build(data);
            }
            else if (data is Control)
            {
                result = (Control)data;
            }
            else
            {
                result = DataTemplate.Default.Build(data);
            }

            result.DataContext = data;

            return result;
        }

        public static IDataTemplate FindDataTemplate(this Control control, object data)
        {
            foreach (var i in control.GetSelfAndLogicalAncestors().OfType<Control>())
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
