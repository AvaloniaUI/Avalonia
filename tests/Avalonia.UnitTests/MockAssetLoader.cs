using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.UnitTests
{
    public class MockAssetLoader : IAssetLoader
    {
        private Dictionary<Uri, string> _assets;

        public MockAssetLoader(params (string, string)[] assets)
        {
            _assets = assets.ToDictionary(x => new Uri(x.Item1, UriKind.RelativeOrAbsolute), x => x.Item2);
        }

        public bool Exists(Uri uri, Uri baseUri = null)
        {
            return _assets.ContainsKey(uri);
        }

        public Stream Open(Uri uri, Uri baseUri = null)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(_assets[uri]));
        }

        public void SetDefaultAssembly(Assembly asm)
        {
            throw new NotImplementedException();
        }
    }
}
