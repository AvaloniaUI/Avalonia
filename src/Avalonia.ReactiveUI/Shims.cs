using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.Serialization
{
    class IgnoreDataMemberAttribute : Attribute
    {
    }

    class DataMemberAttribute : Attribute
    {
    }
    class OnDeserializedAttribute : Attribute
    {
    }

    class DataContractAttribute : Attribute
    {
    }

    class StreamingContext { }
}

namespace System.Diagnostics.Contracts
{
    static class Contract
    {
        public static void Requires(bool condition)
        {

        }
    }
}
