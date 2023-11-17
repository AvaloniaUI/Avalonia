using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Utilities;

namespace Avalonia.Media.Fonts
{
    public sealed class FamilyNameCollection : IReadOnlyList<string>
    {
        private readonly string[] _names;

        /// <summary>
        /// Initializes a new instance of the <see cref="FamilyNameCollection"/> class.
        /// </summary>
        /// <param name="familyNames">The family names.</param>
        /// <exception cref="ArgumentException">familyNames</exception>
        public FamilyNameCollection(string familyNames)
        {
            if (familyNames == null)
            {
                throw new ArgumentNullException(nameof(familyNames));
            }

            _names = SplitNames(familyNames);

            PrimaryFamilyName = _names[0];

            HasFallbacks = _names.Length > 1;
        }

        internal FamilyNameCollection(FrugalStructList<FontSourceIdentifier> fontSources) 
        { 
            _names = new string[fontSources.Count];

            for (int i = 0; i < fontSources.Count; i++)
            {
                _names[i] = fontSources[i].Name;
            }

            PrimaryFamilyName = _names[0];

            HasFallbacks = _names.Length > 1;
        }

        private static string[] SplitNames(string names)
#if NET6_0_OR_GREATER
            => names.Split(',', StringSplitOptions.TrimEntries);
#else
            => Array.ConvertAll(names.Split(','), p => p.Trim());
#endif

        /// <summary>
        /// Gets the primary family name.
        /// </summary>
        /// <value>
        /// The primary family name.
        /// </value>
        public string PrimaryFamilyName { get; }

        /// <summary>
        /// Gets a value indicating whether fallbacks are defined.
        /// </summary>
        /// <value>
        ///   <c>true</c> if fallbacks are defined; otherwise, <c>false</c>.
        /// </value>
        public bool HasFallbacks { get; }

        /// <summary>
        /// Returns an enumerator for the name collection.
        /// </summary>
        public ImmutableReadOnlyListStructEnumerator<string> GetEnumerator()
        {
            return new ImmutableReadOnlyListStructEnumerator<string>(this);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
            => String.Join(", ", _names);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            if (_names.Length == 0)
            {
                return 0;
            }

            unchecked
            {
                int hash = 17;

                for (var i = 0; i < _names.Length; i++)
                {
                    string name = _names[i];

                    hash = hash * 23 + name.GetHashCode();
                }

                return hash;
            }
        }

        public static bool operator !=(FamilyNameCollection? a, FamilyNameCollection? b)
        {
            return !(a == b);
        }

        public static bool operator ==(FamilyNameCollection? a, FamilyNameCollection? b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            return !(a is null) && a.Equals(b);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object? obj)
            => obj is FamilyNameCollection other && _names.AsSpan().SequenceEqual(other._names);

        public int Count => _names.Length;

        public string this[int index] => _names[index];
    }
}
