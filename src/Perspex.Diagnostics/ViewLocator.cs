// -----------------------------------------------------------------------
// <copyright file="ViewLocator.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics
{
    using System;
    using Perspex.Controls;
    using Perspex.Controls.Templates;

    internal class ViewLocator<TViewModel> : IDataTemplate
    {
        public IControl Build(object data)
        {
            var name = data.GetType().FullName.Replace("ViewModel", "View");
            var type = Type.GetType(name);

            if (type != null)
            {
                return (Control)Activator.CreateInstance(type);
            }
            else
            {
                return new TextBlock { Text = name };
            }
        }

        public bool Match(object data)
        {
            return data is TViewModel;
        }
    }
}