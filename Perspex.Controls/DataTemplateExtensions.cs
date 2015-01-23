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
        public static Control ApplyDataTemplate(this Control control, object data)
        {
            IDataTemplate result = control.FindDataTemplate(data);

            if (result != null)
            {
                return result.Build(data);
            }
            else if (data is Control)
            {
                return (Control)data;
            }
            else
            {
                return DataTemplate.Default.Build(data);
            }
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
