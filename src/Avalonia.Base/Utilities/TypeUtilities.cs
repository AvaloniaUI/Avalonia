// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
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
        private static readonly int[] Conversions =
        {
            0b101111111111101, // Boolean
            0b100001111111110, // Char
            0b101111111111111, // SByte
            0b101111111111111, // Byte
            0b101111111111111, // Int16
            0b101111111111111, // UInt16
            0b101111111111111, // Int32
            0b101111111111111, // UInt32
            0b101111111111111, // Int64
            0b101111111111111, // UInt64
            0b101111111111101, // Single
            0b101111111111101, // Double
            0b101111111111101, // Decimal
            0b110000000000000, // DateTime
            0b111111111111111, // String
        };

        private static readonly int[] ImplicitConversions =
        {
            0b000000000000001, // Boolean
            0b001110111100010, // Char
            0b001110101010100, // SByte
            0b001111111111000, // Byte
            0b001110101010000, // Int16
            0b001111111100000, // UInt16
            0b001110101000000, // Int32
            0b001111110000000, // UInt32
            0b001110100000000, // Int64
            0b001111000000000, // UInt64
            0b000110000000000, // Single
            0b000100000000000, // Double
            0b001000000000000, // Decimal
            0b010000000000000, // DateTime
            0b100000000000000, // String
        };

        private static readonly Type[] InbuiltTypes =
        {
            typeof(Boolean),
            typeof(Char),
            typeof(SByte),
            typeof(Byte),
            typeof(Int16),
            typeof(UInt16),
            typeof(Int32),
            typeof(UInt32),
            typeof(Int64),
            typeof(UInt64),
            typeof(Single),
            typeof(Double),
            typeof(Decimal),
            typeof(DateTime),
            typeof(String),
        };

        private static readonly Type[] NumericTypes =
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
        /// Try to convert a value to a type by any means possible.
        /// </summary>
        /// <param name="to">The type to cast to.</param>
        /// <param name="value">The value to cast.</param>
        /// <param name="culture">The culture to use.</param>
        /// <param name="result">If successful, contains the cast value.</param>
        /// <returns>True if the cast was successful, otherwise false.</returns>
        public static bool TryConvert(Type to, object value, CultureInfo culture, out object result)
        {
            if (value == null)
            {
                result = null;
                return AcceptsNull(to);
            }

            if (value == AvaloniaProperty.UnsetValue)
            {
                result = value;
                return true;
            }

            var from = value.GetType();
            var fromTypeInfo = from.GetTypeInfo();
            var toTypeInfo = to.GetTypeInfo();

            if (toTypeInfo.IsAssignableFrom(fromTypeInfo))
            {
                result = value;
                return true;
            }

            if (to == typeof(string))
            {
                result = Convert.ToString(value);
                return true;
            }

            if (toTypeInfo.IsEnum && from == typeof(string))
            {
                if (Enum.IsDefined(to, (string)value))
                {
                    result = Enum.Parse(to, (string)value);
                    return true;
                }
            }

            if (!fromTypeInfo.IsEnum && toTypeInfo.IsEnum)
            {
                result = null;

                if (TryConvert(Enum.GetUnderlyingType(to), value, culture, out object enumValue))
                {
                    result = Enum.ToObject(to, enumValue);
                    return true;
                }
            }

            if (fromTypeInfo.IsEnum && IsNumeric(to))
            {
                try
                {
                    result = Convert.ChangeType((int)value, to, culture);
                    return true;
                }
                catch
                {
                    result = null;
                    return false;
                }
            }

            var convertableFrom = Array.IndexOf(InbuiltTypes, from);
            var convertableTo = Array.IndexOf(InbuiltTypes, to);

            if (convertableFrom != -1 && convertableTo != -1)
            {
                if ((Conversions[convertableFrom] & 1 << convertableTo) != 0)
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
            }

            var cast = FindTypeConversionOperatorMethod(from, to, OperatorType.Implicit | OperatorType.Explicit);

            if (cast != null)
            {
                result = cast.Invoke(null, new[] { value });
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Try to convert a value to a type using the implicit conversions allowed by the C#
        /// language.
        /// </summary>
        /// <param name="to">The type to cast to.</param>
        /// <param name="value">The value to cast.</param>
        /// <param name="result">If successful, contains the cast value.</param>
        /// <returns>True if the cast was successful, otherwise false.</returns>
        public static bool TryConvertImplicit(Type to, object value, out object result)
        {
            if (value == null)
            {
                result = null;
                return AcceptsNull(to);
            }

            if (value == AvaloniaProperty.UnsetValue)
            {
                result = value;
                return true;
            }

            var from = value.GetType();
            var fromTypeInfo = from.GetTypeInfo();
            var toTypeInfo = to.GetTypeInfo();

            if (toTypeInfo.IsAssignableFrom(fromTypeInfo))
            {
                result = value;
                return true;
            }

            var convertableFrom = Array.IndexOf(InbuiltTypes, from);
            var convertableTo = Array.IndexOf(InbuiltTypes, to);

            if (convertableFrom != -1 && convertableTo != -1)
            {
                if ((ImplicitConversions[convertableFrom] & 1 << convertableTo) != 0)
                {
                    try
                    {
                        result = Convert.ChangeType(value, to, CultureInfo.InvariantCulture);
                        return true;
                    }
                    catch
                    {
                        result = null;
                        return false;
                    }
                }
            }

            var cast = FindTypeConversionOperatorMethod(from, to, OperatorType.Implicit);

            if (cast != null)
            {
                result = cast.Invoke(null, new[] { value });
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Convert a value to a type by any means possible, returning the default for that type
        /// if the value could not be converted.
        /// </summary>
        /// <param name="value">The value to cast.</param>
        /// <param name="type">The type to cast to..</param>
        /// <param name="culture">The culture to use.</param>
        /// <returns>A value of <paramref name="type"/>.</returns>
        public static object ConvertOrDefault(object value, Type type, CultureInfo culture)
        {
            return TryConvert(type, value, culture, out object result) ? result : Default(type);
        }

        /// <summary>
        /// Convert a value to a type using the implicit conversions allowed by the C# language or
        /// return the default for the type if the value could not be converted.
        /// </summary>
        /// <param name="value">The value to cast.</param>
        /// <param name="type">The type to cast to..</param>
        /// <returns>A value of <paramref name="type"/>.</returns>
        public static object ConvertImplicitOrDefault(object value, Type type)
        {
            return TryConvertImplicit(type, value, out object result) ? result : Default(type);
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
        /// True if the type is numeric; otherwise false.
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

        [Flags]
        private enum OperatorType
        {
            Implicit = 1,
            Explicit = 2
        }

        private static MethodInfo FindTypeConversionOperatorMethod(Type fromType, Type toType, OperatorType operatorType)
        {
            const string implicitName = "op_Implicit";
            const string explicitName = "op_Explicit";

            bool allowImplicit = (operatorType & OperatorType.Implicit) != 0;
            bool allowExplicit = (operatorType & OperatorType.Explicit) != 0;

            foreach (MethodInfo method in fromType.GetMethods())
            {
                if (!method.IsSpecialName || method.ReturnType != toType)
                {
                    continue;
                }

                if (allowImplicit && method.Name == implicitName)
                {
                    return method;
                }

                if (allowExplicit && method.Name == explicitName)
                {
                    return method;
                }
            }

            return null;
        }
    }
}
