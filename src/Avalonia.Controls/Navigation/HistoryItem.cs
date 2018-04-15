using System;
using System.Collections.Generic;

namespace Avalonia.Controls {

    public class HistoryItem {

        /// <summary>
        /// Type at user control.
        /// </summary>
        public Type Type
        {
            get;
            set;
        }

        /// <summary>
        /// Parameters.
        /// </summary>
        public IEnumerable<object> Parameters
        {
            get;
            set;
        }

    }

}