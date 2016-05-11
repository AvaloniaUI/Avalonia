using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Designer.Metadata;

namespace Avalonia.Designer.Comm
{
    [Serializable]
    public class UpdateMetadataMessage
    {
        public UpdateMetadataMessage(AvaloniaDesignerMetadata metadata)
        {
            Metadata = metadata;
        }

        public AvaloniaDesignerMetadata Metadata { get; }
    }
}
