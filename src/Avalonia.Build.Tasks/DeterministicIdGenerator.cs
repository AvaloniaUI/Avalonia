using System;
using XamlX.Transform;

namespace Avalonia.Build.Tasks
{
    public class DeterministicIdGenerator : IXamlIdentifierGenerator
    {
        // Seed is a part of MD5 Hash of our repo.
        private readonly Random _randomGen = new Random(0x9b94b93);
        
        public string GenerateIdentifierPart()
        {
            var guid = new byte[16];
            _randomGen.NextBytes(guid);
            Console.WriteLine(new Guid(guid).ToString("N"));
            return new Guid(guid).ToString("N");
        }
    }
}
