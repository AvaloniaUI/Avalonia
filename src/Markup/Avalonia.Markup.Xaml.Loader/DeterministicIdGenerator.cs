using XamlX.Transform;

namespace Avalonia.Markup.Xaml.XamlIl
{
    public class DeterministicIdGenerator : IXamlIdentifierGenerator
    {
        private int _nextId = 1;
        
        public string GenerateIdentifierPart() => (_nextId++).ToString();
    }
}
