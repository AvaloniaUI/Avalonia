using System;
using System.Linq;
using Avalonia.Collections;

namespace RenderDemo.Pages
{
    public class DataRepeaterHeaderDescriptors : AvaloniaList<DataRepeaterHeaderDescriptor>
    {
        internal delegate void SortHeaderHandler(DataRepeaterHeaderDescriptor target);
        internal event SortHeaderHandler SortHeader;

        internal void SortDescriptor(DataRepeaterHeaderDescriptor target)
        {
            foreach (var desc in this.Where(p => p != target))
            {
                desc.InternalSortState = DataRepeaterHeaderDescriptor.SortState.None;
            }

            switch(target.InternalSortState)
            {
                case DataRepeaterHeaderDescriptor.SortState.None:
                    target.InternalSortState = DataRepeaterHeaderDescriptor.SortState.Ascending;
                    break;
                case DataRepeaterHeaderDescriptor.SortState.Ascending:
                    target.InternalSortState = DataRepeaterHeaderDescriptor.SortState.Descending;
                    break;
                case DataRepeaterHeaderDescriptor.SortState.Descending:
                    target.InternalSortState = DataRepeaterHeaderDescriptor.SortState.Ascending;
                    break;
            }

            SortHeader?.Invoke(target);
        }
    }
}
