using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Controls
{
    public class ItemsPanelTemplate
    {
        public ItemsPanelTemplate(Func<Panel> build)
        {
            Contract.Requires<ArgumentNullException>(build != null);

            this.Build = build;
        }

        public Func<Panel> Build { get; private set; }
    }
}
