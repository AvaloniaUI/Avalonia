// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using OmniXaml;
using OmniXaml.ObjectAssembler;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Context;

namespace Avalonia.Markup.Xaml.Templates
{
    public class TemplateContent
    {
        private readonly IEnumerable<Instruction> nodes;
        private readonly IRuntimeTypeSource runtimeTypeSource;

        public TemplateContent(IEnumerable<Instruction> nodes, IRuntimeTypeSource runtimeTypeSource)
        {
            this.nodes = nodes;
            this.runtimeTypeSource = runtimeTypeSource;
        }

        public Control Load()
        {
            var assembler = new AvaloniaObjectAssembler(
                runtimeTypeSource,
                new TopDownValueContext());

            foreach (var xamlNode in nodes)
            {
                assembler.Process(xamlNode);
            }

            return (Control)assembler.Result;
        }
    }
}