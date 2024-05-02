using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl;
using TinyJson;

namespace Avalonia.Designer.HostApp;

[RequiresUnreferencedCode(XamlX.TrimmingMessages.DynamicXamlReference)]
class DesignXamlLoader : AvaloniaXamlLoader.IRuntimeXamlLoader
{
    public object Load(RuntimeXamlLoaderDocument document, RuntimeXamlLoaderConfiguration configuration)
    {
        PreloadDepsAssemblies(configuration.LocalAssembly ?? Assembly.GetEntryAssembly());
        
        return AvaloniaXamlIlRuntimeCompiler.Load(document, configuration);
    }

    private void PreloadDepsAssemblies(Assembly targetAssembly)
    {
        // Assemblies loaded in memory (e.g. single file) return empty string from Location.
        // In these cases, don't try probing next to the assembly.
        var assemblyLocation = targetAssembly.Location;
        if (string.IsNullOrEmpty(assemblyLocation))
        {
            return;
        }

        var depsJsonFile = Path.ChangeExtension(assemblyLocation, ".deps.json");
        if (!File.Exists(depsJsonFile))
        {
            var sameDir = Path.GetDirectoryName(depsJsonFile);
            var fallbackDepsFiles = Directory.GetFiles(sameDir, "*.deps.json");
            if (fallbackDepsFiles.Length == 1)
            {
                depsJsonFile = fallbackDepsFiles[0];
            }
            else
            {
                Console.WriteLine($".deps.json file \"{depsJsonFile}\" doesn't exist, it might affect previewer stability.");
                return;   
            }
        }

        using var stream = File.OpenRead(depsJsonFile);

        /*
         We can't use any references in the Avalonia.Designer.HostApp. Including even json.
         Ideally we would prefer Microsoft.Extensions.DependencyModel package, but can't use it here.
         So, instead we need to fallback to some JSON parsing with copy-paste tiny json.
         
         Json part example:
"Avalonia.Xaml.Interactions/11.0.0-preview5": {
  "dependencies": {
    "Avalonia": "11.0.999",
    "Avalonia.Xaml.Interactivity": "11.0.0-preview5"
  },
  "runtime": {
    "lib/net6.0/Avalonia.Xaml.Interactions.dll": {
      "assemblyVersion": "11.0.0.0",
      "fileVersion": "11.0.0.0"
    }
  }
},
        We want to extract "lib/net6.0/Avalonia.Xaml.Interactions.dll" from here.
        No need to resolve real path of ref assemblies.
        No need to handle special cases with .NET Framework and GAC.
         */
        var text = new StreamReader(stream).ReadToEnd();
        var deps = ParseRuntimeDeps(text);

        foreach (var dependencyRuntimeLibs in deps)
        {
            foreach (var runtimeLib in dependencyRuntimeLibs)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(runtimeLib);
                try
                {
                    _ = Assembly.Load(new AssemblyName(assemblyName));
                }
                catch
                {
                }   
            }
        }
    }

    private static List<IEnumerable<string>> ParseRuntimeDeps(string text)
    {
        var runtimeDeps = new List<IEnumerable<string>>();
        try
        {
            var value = JSONParser.FromJson<Dictionary<string, object>>(text);
            if (value?.TryGetValue("targets", out var targetsObj) == true
                && targetsObj is Dictionary<string, object> targets)
            {
                foreach (var target in targets)
                {
                    if (target.Value is Dictionary<string, object> libraries)
                    {
                        foreach (var library in libraries)
                        {
                            if ((library.Value as Dictionary<string, object>)?.TryGetValue("runtime", out var runtimeObj) == true
                                && runtimeObj is Dictionary<string, object> runtime)
                            {
                                runtimeDeps.Add(runtime.Keys);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(".deps.json file parsing failed, it might affect previewer stability.\r\n" + ex);
        }
        return runtimeDeps;
    }
}
