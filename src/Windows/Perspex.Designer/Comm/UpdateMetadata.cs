using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Designer.Metadata;

namespace Perspex.Designer.Comm
{
    [Serializable]
    public class UpdateMetadataMessage
    {
        public UpdateMetadataMessage(PerspexDesignerMetadata metadata)
        {
            Metadata = metadata;
        }

        public PerspexDesignerMetadata Metadata { get; }
    }
}
