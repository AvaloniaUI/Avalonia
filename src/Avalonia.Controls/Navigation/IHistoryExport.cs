using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Controls {

    public interface IHistoryExport {

        Task Export ( IEnumerable<HistoryItem> items , int selected );

    }

}