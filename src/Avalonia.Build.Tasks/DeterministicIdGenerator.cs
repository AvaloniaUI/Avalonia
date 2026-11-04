using System;
using System.Collections.Generic;
using System.Globalization;
using XamlX.Transform;

namespace Avalonia.Build.Tasks
{
    public class DeterministicIdGenerator : IXamlIdentifierGenerator
    {
        private readonly bool _compactNames;
        private long _nextId = 1;
        private long _carrierGroup = -1;
        private string? _carrier;

        public DeterministicIdGenerator(bool compactNames = false) => _compactNames = compactNames;

        private string NextReadableIdentifierPart() => (_nextId++).ToString(CultureInfo.InvariantCulture);

        GeneratedIdentifier IXamlIdentifierGenerator.CreateIdentifierFromPrefix(string prefix)
            => _compactNames
                ? new CompactGeneratedIdentifier(NextCompactName)
                : new ReadableGeneratedIdentifier(prefix + NextReadableIdentifierPart());

        GeneratedIdentifier IXamlIdentifierGenerator.CreateIdentifierFromPreferredName(string name, int nameUseCount)
            => _compactNames
                ? new CompactGeneratedIdentifier(NextCompactName)
                : new ReadableGeneratedIdentifier(nameUseCount == 0
                    ? name
                    : name + "_" + NextReadableIdentifierPart());

        private string NextCompactName()
        {
            const int carrierLength = 64;
            const int namesPerCarrier = carrierLength - 4;
            var index = _nextId++ - 1;
            var group = index / namesPerCarrier;
            if (_carrierGroup != group)
            {
                var chars = new char[carrierLength];
                for (var i = 0; i < namesPerCarrier; i++)
                    chars[i] = '_';
                for (var digit = carrierLength - 1; digit >= namesPerCarrier; digit--)
                {
                    chars[digit] = (char)(1 + group % 127);
                    group /= 127;
                }
                if (group != 0)
                    throw new InvalidOperationException("Too many generated XAML identifiers.");
                _carrier = new string(chars);
                _carrierGroup = index / namesPerCarrier;
            }

            var length = 5 + (int)(index % namesPerCarrier);
            return _carrier!.Substring(carrierLength - length);
        }

        private sealed class CompactGeneratedIdentifier : GeneratedIdentifier
        {
            private readonly Func<string> _nextName;
            private string? _name;
            private Dictionary<string, string>? _suffixes;

            public CompactGeneratedIdentifier(Func<string> nextName) => _nextName = nextName;

            public override string Name => _name ??= _nextName();

            public override string AppendSuffix(string suffix)
            {
                _suffixes ??= new Dictionary<string, string>(StringComparer.Ordinal);
                if (!_suffixes.TryGetValue(suffix, out var name))
                    _suffixes.Add(suffix, name = _nextName());
                return name;
            }
        }
    }
}
