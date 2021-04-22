using System;
using XamlX.Transform;

namespace Avalonia.Build.Tasks
{
    public class DeterministicIdGenerator : IXamlIdentifierGenerator
    {
        private int _nextId = 1;
        
        public string GenerateIdentifierPart() => (_nextId++).ToString();
    }
}
