using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    internal class SandboxBookmarkFactory : ISandboxBookmarkFactory
    {
        private readonly IAvaloniaNativeFactory _factory;

        public SandboxBookmarkFactory(IAvaloniaNativeFactory factory)
        {
            _factory = factory;
        }

        public unsafe ISandboxBookmark Create(byte[] bookmarkData)
        {
            fixed (byte* pBookmarkData = bookmarkData)
            {
                var nativeObject = _factory.CreateSandboxBookmark(pBookmarkData, bookmarkData.Length);
                return new SandboxBookmark(nativeObject);
            }
        }
    }
}
