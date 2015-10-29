using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Perspex.Controls;
using Perspex.Metadata;

namespace Perspex.Markup.Xaml.Templates
{
    public class ItemsPanelTemplate : ITemplate<IPanel>
    {
        [Content]
        public TemplateContent Content { get; set; }

        public IPanel Build()
        {
            return (IPanel)Content.Load();
        }
    }
}
