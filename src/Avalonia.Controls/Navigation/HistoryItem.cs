using System;
using System.Collections.Generic;

namespace Avalonia.Controls {

    public class HistoryItem
    {

        public Type Type { get; set; }

        public IEnumerable<object> Parameters { get; set; }

    }

}