using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Utilities;
using Mono.Cecil;

namespace Avalonia.Build.Tasks
{
    public static partial class XamlCompilerTaskExecutor
    {
        interface IResource
        {
            string Uri { get; }
            string Name { get; }
            byte[] GetData();
            string FilePath { get; }
            void Remove();

        }

        interface IResourceGroup
        {
            string Name { get; }
            IEnumerable<IResource> Resources { get; }
        }
        
        class EmbeddedResources : IResourceGroup
        {
            private readonly AssemblyDefinition _asm;
            public string Name => "EmbeddedResource";

            public IEnumerable<IResource> Resources => _asm.MainModule.Resources.OfType<EmbeddedResource>()
                .Select(r => new WrappedResource(_asm, r)).ToList();

            public EmbeddedResources(AssemblyDefinition asm)
            {
                _asm = asm;
            }
            class WrappedResource : IResource
            {
                private readonly AssemblyDefinition _asm;
                private readonly EmbeddedResource _res;

                public WrappedResource(AssemblyDefinition asm, EmbeddedResource res)
                {
                    _asm = asm;
                    _res = res;
                }

                public string Uri => $"resm:{Name}?assembly={_asm.Name}";
                public string Name => _res.Name;
                public byte[] GetData() => _res.GetResourceData();
                public string FilePath => Name;

                public void Remove() => _asm.MainModule.Resources.Remove(_res);
            }
        }

        class AvaloniaResources : IResourceGroup
        {
            private readonly AssemblyDefinition _asm;
            Dictionary<string, AvaloniaResource> _resources = new Dictionary<string, AvaloniaResource>();
            private EmbeddedResource _embedded;
            public AvaloniaResources(AssemblyDefinition asm, string projectDir)
            {
                _asm = asm;
                _embedded = ((EmbeddedResource)asm.MainModule.Resources.FirstOrDefault(r =>
                    r.ResourceType == ResourceType.Embedded && r.Name == "!AvaloniaResources"));
                if (_embedded == null)
                    return;
                using (var stream = _embedded.GetResourceStream())
                {
                    var br = new BinaryReader(stream);
                    var index = AvaloniaResourcesIndexReaderWriter.Read(new MemoryStream(br.ReadBytes(br.ReadInt32())));
                    var baseOffset = stream.Position;
                    foreach (var e in index)
                    {
                        stream.Position = e.Offset + baseOffset;
                        _resources[e.Path] = new AvaloniaResource(this, projectDir, e.Path, br.ReadBytes(e.Size));
                    }
                }
            }

            public void Save()
            {
                if (_embedded != null)
                {
                    _asm.MainModule.Resources.Remove(_embedded);
                    _embedded = null;
                }

                if (_resources.Count == 0)
                    return;

                _embedded = new EmbeddedResource("!AvaloniaResources", ManifestResourceAttributes.Public,
                    AvaloniaResourcesIndexReaderWriter.Create(_resources.ToDictionary(x => x.Key,
                        x => x.Value.GetData())));
                _asm.MainModule.Resources.Add(_embedded);
            }

            public string Name => "AvaloniaResources";
            public IEnumerable<IResource> Resources => _resources.Values.ToList();

            class AvaloniaResource : IResource
            {
                private readonly AvaloniaResources _grp;
                private readonly byte[] _data;

                public AvaloniaResource(AvaloniaResources grp,
                    string projectDir,
                    string name, byte[] data)
                {
                    _grp = grp;
                    _data = data;
                    Name = name;
                    FilePath = Path.Combine(projectDir, name.TrimStart('/'));
                    Uri = $"avares://{grp._asm.Name}/{name.TrimStart('/')}";
                }
                public string Uri { get; }
                public string Name { get; }
                public byte[] GetData() => _data;
                public string FilePath { get; }

                public void Remove() => _grp._resources.Remove(Name);
            }
        }
    }
 
}
