using System;
using System.Collections.Generic;
using System.Text;
#if !OMNIXAML
namespace OmniXaml
{
    interface ITypeProvider
    {
    }

    class TypeNotFoundException : Exception
    {
        public TypeNotFoundException(string m) : base(m)
        {

        }
    }
}
namespace OmniXaml.Typing
{
    class Stub
    {
    }
}
namespace OmniXaml.TypeConversion
{
    class Stub
    {
    }
}
namespace OmniXaml.Builder
{
    class Stub
    {
    }
}
namespace OmniXaml.ObjectAssembler.Commands
{
    class Stub
    {
    }
}
namespace OmniXaml.Parsers.ProtoParser
{
    class Stub
    {
    }
}
namespace OmniXaml.Parsers.Parser
{
    class Stub
    {
    }
}
namespace OmniXaml.Attributes
{
    class Stub
    {
    }
}
namespace Glass
{
    class Stub
    {
    }
}
namespace Glass.Core
{
    class Stub
    {
    }
}
#endif