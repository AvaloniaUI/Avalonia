using System;
using System.IO;

namespace XamlNameReferenceGenerator.Infrastructure
{
    internal class PhysicalFileDebugger
    {
        private const string DefaultPath = @"C:\Users\prizr\Documents\GitHub\XamlNameReferenceGenerator\debug.txt";
        private readonly string _path;

        public PhysicalFileDebugger(string path = DefaultPath) => _path = path;

        public string Debug(Func<string> function)
        {
            if (File.Exists(_path))
                File.Delete(_path);

            try
            {
                var sourceCode = function();
                File.WriteAllText(_path, sourceCode);
                return sourceCode;
            }
            catch (Exception exception)
            {
                File.WriteAllText(_path, exception.ToString());
                throw;
            }
        }
    }
}