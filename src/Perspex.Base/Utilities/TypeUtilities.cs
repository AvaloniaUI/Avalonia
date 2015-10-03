// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Perspex.Utilities
{
    /// <summary>
    /// Provides utilities for working with types at runtime.
    /// </summary>
    internal static class TypeUtilities
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

        /// <summary>
        /// Try to cast a value to a type, using implicit conversions if possible.
        /// </summary>
        /// <param name="to">The type to cast to.</param>
        /// <param name="value">The value to cast.</param>
        /// <param name="result">If sucessful, contains the cast value.</param>
        /// <param name="allowUnset">Allow <see cref="PerspexProperty.UnsetValue"/>.</param>
        /// <returns>True if the cast was sucessful, otherwise false.</returns>
        public static bool TryCast(Type to, object value, out object result, bool allowUnset = true)
        {
            Contract.Requires<ArgumentNullException>(to != null);

            if (value == null)
            {
                var t = to.GetTypeInfo();
                result = null;
                return !t.IsValueType || (t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(Nullable<>)));
            }

            var from = value.GetType();

            if (allowUnset && value == PerspexProperty.UnsetValue)
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
                var cast = from.GetTypeInfo()
                    .GetDeclaredMethods("op_Implicit")
                    .FirstOrDefault(m => m.ReturnType == to);

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
        /// Casts a value to a type, returning the default for that type if the value could not be
        /// cast.
        /// </summary>
        /// <param name="value">The value to cast.</param>
        /// <param name="type">The type to cast to..</param>
        /// <param name="allowUnset">Allow <see cref="PerspexProperty.UnsetValue"/>.</param>
        /// <returns>A value of <paramref name="type"/>.</returns>
        public static object CastOrDefault(object value, Type type, bool allowUnset = true)
        {
            var typeInfo = type.GetTypeInfo();
            object result;

            if (TypeUtilities.TryCast(type, value, out result, allowUnset))
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
    }
}
