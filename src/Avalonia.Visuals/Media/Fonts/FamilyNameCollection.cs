// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Avalonia.Media.Fonts
{
    internal class FamilyNameCollection : IEnumerable<string>
    {
        private readonly ReadOnlyCollection<string> _familyNames;

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

            _familyNames = new ReadOnlyCollection<string>(names);

            PrimaryFamilyName = _familyNames.First();

            HasFallbacks = _familyNames.Count > 1;
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

        /// <inheritdoc />
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<string> GetEnumerator()
        {
            return _familyNames.GetEnumerator();
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
    }
}