using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Controls {

    public interface IHistoryImport {

        Task<(IEnumerable<HistoryItem> items, int selected)> Import ();

    }

}