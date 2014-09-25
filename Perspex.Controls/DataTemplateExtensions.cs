// -----------------------------------------------------------------------
// <copyright file="DataTemplateExtensions.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System.Linq;
    using Splat;

    public static class DataTemplateExtensions
    {
        public static Control ApplyDataTemplate(this Control control, object data)
        {
            DataTemplate result = control.FindDataTemplate(data);

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

        public static DataTemplate FindDataTemplate(this Control control, object data)
        {
            // TODO: This needs to traverse the logical tree, not the visual.
            foreach (var i in control.GetSelfAndVisualAncestors().OfType<Control>())
            {
                foreach (DataTemplate dt in i.DataTemplates.Reverse())
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
                foreach (DataTemplate dt in global.DataTemplates.Reverse())
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
