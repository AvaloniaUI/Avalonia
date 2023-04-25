using System;
using System.IO;
using Nuke.Common.Utilities;

class Helpers
{
    public static IDisposable UseTempDir(out string dir)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        dir = path;
        return DelegateDisposable.CreateBracket(null, () =>
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch
            {
                // ignore
            }
        });
    }
}
