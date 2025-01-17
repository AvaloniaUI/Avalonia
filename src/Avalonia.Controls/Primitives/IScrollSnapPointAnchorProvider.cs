using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Controls.Primitives
{
    internal interface IScrollSnapPointAnchorProvider
    {
        void RegisterScrollSnapPointsInfoSource(IScrollSnapPointsInfo scrollSnapPointsInfo);
        void UnregisterScrollSnapPointsInfoSource(IScrollSnapPointsInfo scrollSnapPointsInfo);
    }
}
