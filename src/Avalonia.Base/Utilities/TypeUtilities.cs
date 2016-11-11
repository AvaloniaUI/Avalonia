// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Avalonia.Utilities
{
    /// <summary>
    /// Provides utilities for working with types at runtime.
    /// </summary>
    public static class TypeUtilities
    {
        private static readonly Dictionary<Type, List<Type>> Conversions = new Dictionary<Type, List<Type>>()
        {
            { typeof(decimal), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char) } },
            { typeof(double), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(float) } },
            { typeof(float), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(float) } },
            { typeof(ulong), new List<Type> { typeof(byte), typeof(ushort), typeof(uint), typeof(char) } },
            { typeof(long), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(char) } },
            { typeof(uint), new List<Type> { typeof(byte), typeof(ushort), typeof(char) } },
            { typeof(int), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(char) } },
            { typeof(ushort), new List<Type> { typeof(byte), typeof(char) } },
            { typeof(short), new List<Type> { typeof(byte) } }
        };

        private static readonly Type[] NumericTypes = new[]
        {
            typeof(Byte),
            typeof(Decimal),
            typeof(Double),
            typeof(Int16),
            typeof(Int32),
            typeof(Int64),
            typeof(SByte),
            typeof(Single),
            typeof(UInt16),
            typeof(UInt32),
            typeof(UInt64),
        };

        /// <summary>
        /// Returns a value indicating whether null can be assigned to the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True if the type accepts null values; otherwise false.</returns>
        public static bool AcceptsNull(Type type)
        {
            var t = type.GetTypeInfo();
            return !t.IsValueType || (t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(Nullable<>)));
        }

        /// <summary>
        /// Try to cast a value to a type, using implicit conversions if possible.
        /// </summary>
        /// <param name="to">The type to cast to.</param>
        /// <param name="value">The value to cast.</param>
        /// <param name="result">If sucessful, contains the cast value.</param>
        /// <returns>True if the cast was sucessful, otherwise false.</returns>
        public static bool TryCast(Type to, object value, out object result)
        {
            Contract.Requires<ArgumentNullException>(to != null);

            if (value == null)
            {
                result = null;
                return AcceptsNull(to);
            }

            var from = value.GetType();

            if (value == AvaloniaProperty.UnsetValue)
            {
                result = value;
                return true;
            }
            else if (to.GetTypeInfo().IsAssignableFrom(from.GetTypeInfo()))
            {
                result = value;
                return true;
            }
            else if (Conversions.ContainsKey(to) && Conversions[to].Contains(from))
            {
                result = Convert.ChangeType(value, to);
                return true;
            }
            else
            {
                var cast = from.GetRuntimeMethods()
                    .FirstOrDefault(m => m.Name == "op_Implicit" && m.ReturnType == to);

                if (cast != null)
                {
                    result = cast.Invoke(null, new[] { value });
                    return true;
                }
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Try to convert a value to a type, using <see cref="System.Convert"/> if possible,
        /// otherwise using <see cref="TryCast(Type, object, out object)"/>.
        /// </summary>
        /// <param name="to">The type to cast to.</param>
        /// <param name="value">The value to cast.</param>
        /// <param name="culture">The culture to use.</param>
        /// <param name="result">If sucessful, contains the cast value.</param>
        /// <returns>True if the cast was sucessful, otherwise false.</returns>
        public static bool TryConvert(Type to, object value, CultureInfo culture, out object result)
        {
            if (value == null)
            {
                result = null;
                return AcceptsNull(to);
            }

            var from = value.GetType();

            if (value == AvaloniaProperty.UnsetValue)
            {
                result = value;
                return true;
            }

            if (to.GetTypeInfo().IsAssignableFrom(from.GetTypeInfo()))
            {
                result = value;
                return true;
            }

            if (to == typeof(string))
            {
                result = Convert.ToString(value);
                return true;
            }

            if (to.GetTypeInfo().IsEnum && from == typeof(string))
            {
                if (Enum.IsDefined(to, (string)value))
                {
                    result = Enum.Parse(to, (string)value);
                    return true;
                }
            }

            bool containsFrom = Conversions.ContainsKey(from);
            bool containsTo = Conversions.ContainsKey(to);

            if ((containsFrom && containsTo) || (from == typeof(string) && containsTo))
            {
                try
                {
                    result = Convert.ChangeType(value, to, culture);
                    return true;
                }
                catch
                {
                    result = null;
                    return false;
                }
            }

            var cast = from.GetRuntimeMethods()
                .FirstOrDefault(m => (m.Name == "op_Implicit" || m.Name == "op_Explicit") && m.ReturnType == to);

            if (cast != null)
            {
                result = cast.Invoke(null, new[] { value });
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Casts a value to a type, returning the default for that type if the value could not be
        /// cast.
        /// </summary>
        /// <param name="value">The value to cast.</param>
        /// <param name="type">The type to cast to..</param>
        /// <returns>A value of <paramref name="type"/>.</returns>
        public static object CastOrDefault(object value, Type type)
        {
            var typeInfo = type.GetTypeInfo();
            object result;

            if (TypeUtilities.TryCast(type, value, out result))
            {
                return result;
            }
            else if (typeInfo.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the default value for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The default value.</returns>
        public static object Default(Type type)
        {
            var typeInfo = type.GetTypeInfo();

            if (typeInfo.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Determines if a type is numeric.  Nullable numeric types are considered numeric.
        /// </summary>
        /// <returns>
        /// True if the type is numberic; otherwise false.
        /// </returns>
        /// <remarks>
        /// Boolean is not considered numeric.
        /// </remarks>
        public static bool IsNumeric(Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return IsNumeric(Nullable.GetUnderlyingType(type));
            }
            else
            {
                return NumericTypes.Contains(type);
            }
        }
    }
}
