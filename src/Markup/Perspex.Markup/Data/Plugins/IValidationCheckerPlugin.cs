using Perspex.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Markup.Data.Plugins
{
    public interface IValidationCheckerPlugin
    {
        ValidationCheckerBase Start(WeakReference reference, string name, IPropertyAccessor accessor, Action<ValidationStatus> callback);
    }
}
