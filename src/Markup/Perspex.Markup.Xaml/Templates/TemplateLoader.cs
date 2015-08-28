// -----------------------------------------------------------------------
// <copyright file="TemplateLoader.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Markup.Xaml.Templates
{
    using System.Collections.Generic;
    using OmniXaml;

    public class TemplateLoader : IDeferredLoader
    {
        public object Load(IEnumerable<XamlInstruction> nodes, IWiringContext context)
        {
            return new TemplateContent(nodes, context);
        }
    }
}