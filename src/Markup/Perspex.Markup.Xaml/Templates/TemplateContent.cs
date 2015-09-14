// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using OmniXaml;
using Perspex.Controls;
using Perspex.Markup.Xaml.Context;

namespace Perspex.Markup.Xaml.Templates
{
    public class TemplateContent
    {
        private readonly IEnumerable<XamlInstruction> _nodes;
        private readonly IWiringContext _context;

        public TemplateContent(IEnumerable<XamlInstruction> nodes, IWiringContext context)
        {
            _nodes = nodes;
            _context = context;
        }

        public Control Load()
        {
            var assembler = new PerspexObjectAssembler(_context);

            foreach (var xamlNode in _nodes)
            {
                assembler.Process(xamlNode);
            }

            return (Control)assembler.Result;
        }
    }
}