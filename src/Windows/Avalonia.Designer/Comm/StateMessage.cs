using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Designer.Comm
{
    [Serializable]
    class StateMessage
    {
        public StateMessage(string state)
        {
            State = state;
        }

        public string State { get; private set; }
    }
}
