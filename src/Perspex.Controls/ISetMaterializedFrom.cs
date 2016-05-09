using Perspex.Controls.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Controls
{
    /// <summary>
    /// Enables setting which data template a control is materialized from.
    /// </summary>
    interface ISetMaterializedFrom
    {
        /// <summary>
        /// Sets which template a control is materialized from.
        /// </summary>
        /// <param name="template">The template the control is materialized from.</param>
        void SetMaterializedFrom(IDataTemplate template);
    }
}
