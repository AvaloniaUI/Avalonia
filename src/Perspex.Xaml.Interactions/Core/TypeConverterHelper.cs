// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Perspex.Xaml.Interactions.Core
{
    /// <summary>
    /// A helper class that enables converting values specified in markup (strings) to their object representation.
    /// </summary>
    internal static class TypeConverterHelper
    {
        /// <summary>
        /// Converts string representation of a value to its object representation.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="destinationTypeFullName">The full name of the destination type.</param>
        /// <returns>Object representation of the string value.</returns>
        /// <exception cref="ArgumentNullException">destinationTypeFullName cannot be null.</exception>
        public static Object Convert(string value, string destinationTypeFullName)
        {
            if (string.IsNullOrEmpty(destinationTypeFullName))
            {
                throw new ArgumentNullException("destinationTypeFullName");
            }

            string scope = TypeConverterHelper.GetScope(destinationTypeFullName);

            // Value types in the "System" namespace must be special cased due to a bug in the xaml compiler
            if (string.Equals(scope, "System", StringComparison.Ordinal))
            {
                if (string.Equals(destinationTypeFullName, (typeof(string).FullName), StringComparison.Ordinal))
                {
                    return value;
                }
                else if (string.Equals(destinationTypeFullName, typeof(bool).FullName, StringComparison.Ordinal))
                {
                    return bool.Parse(value);
                }
                else if (string.Equals(destinationTypeFullName, typeof(int).FullName, StringComparison.Ordinal))
                {
                    return int.Parse(value, CultureInfo.InvariantCulture);
                }
                else if (string.Equals(destinationTypeFullName, typeof(double).FullName, StringComparison.Ordinal))
                {
                    return double.Parse(value, CultureInfo.CurrentCulture);
                }
            }

            return null;
        }

        private static String GetScope(string name)
        {
            int indexOfLastPeriod = name.LastIndexOf('.');
            if (indexOfLastPeriod != name.Length - 1)
            {
                return name.Substring(0, indexOfLastPeriod);
            }

            return name;
        }

        private static String GetType(string name)
        {
            int indexOfLastPeriod = name.LastIndexOf('.');
            if (indexOfLastPeriod != name.Length - 1)
            {
                return name.Substring(++indexOfLastPeriod);
            }

            return name;
        }
    }
}
