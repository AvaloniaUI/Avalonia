using System;
using System.IO;

namespace XamlNameReferenceGenerator
{
    internal class NameReferenceDebugger
    {
        private readonly string _path;

        public NameReferenceDebugger(string path) => _path = path;

        public string Debug(Func<string> function)
        {
            if (File.Exists(_path))
                File.Delete(_path);

            string sourceCode;
            try
            {
                sourceCode = function();
                File.WriteAllText(_path, sourceCode);
            }
            catch (Exception exception)
            {
                File.WriteAllText(_path, exception.ToString());
                sourceCode = string.Empty;
            }

            return sourceCode;
        }
    }
}