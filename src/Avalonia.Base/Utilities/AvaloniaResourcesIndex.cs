using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Avalonia.Utilities
{
#if !BUILDTASK
    public
#endif
    static class AvaloniaResourcesIndexReaderWriter
    {
        private const int XmlLegacyVersion = 1;
        private const int BinaryCurrentVersion = 2;

        public static List<AvaloniaResourcesIndexEntry> ReadIndex(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            var version = reader.ReadInt32();
            return version switch
            {
                XmlLegacyVersion => ReadXmlIndex(),
                BinaryCurrentVersion => ReadBinaryIndex(reader),
                _ => throw new Exception($"Unknown resources index format version {version}")
            };
        }

        private static List<AvaloniaResourcesIndexEntry> ReadXmlIndex()
            => throw new NotSupportedException("Found legacy resources index format: please recompile your XAML files");

        private static List<AvaloniaResourcesIndexEntry> ReadBinaryIndex(BinaryReader reader)
        {
            var entryCount = reader.ReadInt32();
            var entries = new List<AvaloniaResourcesIndexEntry>(entryCount);

            for (var i = 0; i < entryCount; ++i)
            {
                entries.Add(new AvaloniaResourcesIndexEntry {
                    Path = reader.ReadString(),
                    Offset = reader.ReadInt32(),
                    Size = reader.ReadInt32()
                });
            }

            return entries;
        }

        public static void WriteIndex(Stream output, List<AvaloniaResourcesIndexEntry> entries)
        {
            using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);

            WriteIndex(writer, entries);
        }

        private static void WriteIndex(BinaryWriter writer, List<AvaloniaResourcesIndexEntry> entries)
        {
            writer.Write(BinaryCurrentVersion);
            writer.Write(entries.Count);

            foreach (var entry in entries)
            {
                writer.Write(entry.Path ?? string.Empty);
                writer.Write(entry.Offset);
                writer.Write(entry.Size);
            }
        }

        [Obsolete]
        public static void WriteResources(Stream output, List<(string Path, int Size, Func<Stream> Open)> resources)
        {
            WriteResources(output,
                resources.Select(r => new AvaloniaResourcesEntry { Path = r.Path, Open = r.Open, Size = r.Size })
                    .ToList());
        }

        public static void WriteResources(Stream output, IReadOnlyList<AvaloniaResourcesEntry> resources)
        {
            var entries = new List<AvaloniaResourcesIndexEntry>();
            var index = new Dictionary<string, (AvaloniaResourcesIndexEntry entry, Func<Stream> open)>();
            var offset = 0;

            foreach (var resource in resources)
            {
                // Try to combine resources with the same system path, if present.
                if (!string.IsNullOrEmpty(resource.SystemPath)
                    && index.TryGetValue(resource.SystemPath, out var existingResource))
                {
                    entries.Add(new AvaloniaResourcesIndexEntry
                    {
                        Path = resource.Path,
                        Offset = existingResource.entry.Offset,
                        Size = existingResource.entry.Size
                    });
                }
                else
                {
                    var entry = new AvaloniaResourcesIndexEntry
                    {
                        Path = resource.Path,
                        Offset = offset,
                        Size = resource.Size
                    };
                    index[resource.SystemPath ?? offset.ToString()] = (entry, resource.Open!);
                    entries.Add(entry);
                    offset += resource.Size;
                }
            }

            using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
            writer.Write(0); // index size placeholder, overwritten below

            var posBeforeEntries = output.Position;
            WriteIndex(writer, entries);

            var posAfterEntries = output.Position;
            var indexSize = (int) (posAfterEntries - posBeforeEntries);
            output.Position = 0L;
            writer.Write(indexSize);
            output.Position = posAfterEntries;

            foreach (var pair in index)
            {
                using var resourceStream = pair.Value.open();
                resourceStream.CopyTo(output);
            }
        }
    }

#if !BUILDTASK
    public
#endif
    class AvaloniaResourcesIndexEntry
    {
        public string? Path { get; set; }

        public int Offset { get; set; }

        public int Size { get; set; }
    }

#if !BUILDTASK
    public
#endif
    class AvaloniaResourcesEntry
    {
        public string? Path { get; init; }
        public Func<Stream>? Open { get; init; }
        public int Size { get; init; }
        public string? SystemPath { get; init; }
    }
}
