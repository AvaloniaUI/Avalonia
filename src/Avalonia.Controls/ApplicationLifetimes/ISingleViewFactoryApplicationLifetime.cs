using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Controls.ApplicationLifetimes
{
    [NotClientImplementable]
    public interface ISingleViewFactoryApplicationLifetime : IApplicationLifetime
    {
        Func<Control>? MainViewFactory { get; set; }
    }
}
