using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Shared.PlatformSupport
{
    internal partial class StandardRuntimePlatform
    {
        public Assembly[] GetLoadedAssemblies()
        {

            var rv = new List<Assembly>();
            var entry = Assembly.GetEntryAssembly();
            rv.Add(entry);
            var queue = new Queue<AssemblyName>(entry.GetReferencedAssemblies());
            var aset = new HashSet<string>(queue.Select(r => r.ToString()));

            while (queue.Count > 0)
            {
                Assembly asm;
                try
                {
                    asm = Assembly.Load(queue.Dequeue());
                }
                catch (Exception e)
                {
                    Debug.Write(e.ToString());
                    continue;
                }
                rv.Add(asm);
                foreach (var r in asm.GetReferencedAssemblies())
                {
                    if (aset.Add(r.ToString()))
                        queue.Enqueue(r);
                }
            }
            return rv.Distinct().ToArray();
        }
    }
}
