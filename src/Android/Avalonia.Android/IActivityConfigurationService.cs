using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Android
{
    public interface IActivityConfigurationService
    {
        event EventHandler? ConfigurationChanged;
    }
}
