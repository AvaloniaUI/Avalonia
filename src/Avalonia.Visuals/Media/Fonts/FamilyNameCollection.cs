// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Avalonia.Media.Fonts
{
    using System.Text;

    public class FamilyNameCollection : IEnumerable<string>
    {    
        /// <summary>
        /// Initializes a new instance of the <see cref="FamilyNameCollection"/> class.
        /// </summary>
        /// <param name="familyNames">The family names.</param>
        /// <exception cref="ArgumentException">familyNames</exception>
        public FamilyNameCollection(IEnumerable<string> familyNames)
        {
            Contract.Requires<ArgumentNullException>(familyNames != null);

            var names = new List<string>(familyNames);

            if (names.Count == 0) throw new ArgumentException($"{nameof(familyNames)} must not be empty.");

            Names = new ReadOnlyCollection<string>(names);

            PrimaryFamilyName = Names.First();

            HasFallbacks = Names.Count > 1;
        }

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
        /// Gets the internal collection of names.
        /// </summary>
        /// <value>
        /// The names.
        /// </value>
        internal ReadOnlyCollection<string> Names { get; }

        /// <inheritdoc />
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<string> GetEnumerator()
        {
            return Names.GetEnumerator();
        }

        /// <inheritdoc />
        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        /// </returns>
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
        {
            var builder = new StringBuilder();

            for (var index = 0; index < Names.Count; index++)
            {
                builder.Append(Names[index]);

                if (index == Names.Count - 1) break;

                builder.Append(", ");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (!(obj is FamilyNameCollection other)) return false;

            return other.ToString().Equals(ToString());
        }
    }
}