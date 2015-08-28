// -----------------------------------------------------------------------
// <copyright file="TemplateContent.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Markup.Xaml.Templates
{
    using System.Collections.Generic;
    using Context;
    using OmniXaml;
    using Perspex.Controls;

    public class TemplateContent
    {
        private readonly IEnumerable<XamlInstruction> nodes;
        private readonly IWiringContext context;

        public TemplateContent(IEnumerable<XamlInstruction> nodes, IWiringContext context)
        {
            this.nodes = nodes;
            this.context = context;
        }

        public Control Load()
        {
            var assembler = new PerspexObjectAssembler(this.context);

            foreach (var xamlNode in this.nodes)
            {
                assembler.Process(xamlNode);
            }

            return (Control)assembler.Result;
        }
    }
}