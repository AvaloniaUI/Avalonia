// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Perspex.Xaml.Interactions.Core
{
    using System;
    using System.Globalization;
    using Interactivity;

    /// <summary>
    /// A helper class that enables converting values specified in markup (strings) to their object representation.
    /// </summary>
    internal static class TypeConverterHelper
    {
        // TODO:
        //private const string ContentControlFormatString = "<ContentControl xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:c='using:{0}'><c:{1}>{2}</c:{1}></ContentControl>";

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

            // TODO:
            /*
            string type = TypeConverterHelper.GetType(destinationTypeFullName);
            string contentControlXaml = string.Format(CultureInfo.InvariantCulture, TypeConverterHelper.ContentControlFormatString, scope, type, value);

            ContentControl contentControl = XamlReader.Load(contentControlXaml) as ContentControl;
            if (contentControl != null)
            {
                return contentControl.Content;
            }
            */

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
