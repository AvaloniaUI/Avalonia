using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Platform
{
    public interface IMediaProviderHost
    {
        IMediaProvider MediaProvider { get; }
    }
}
