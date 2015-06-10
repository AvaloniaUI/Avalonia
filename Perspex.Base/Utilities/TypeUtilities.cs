// -----------------------------------------------------------------------
// <copyright file="TypeUtilities.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Provides utilities for working with types at runtime.
    /// </summary>
    internal static class TypeUtilities
    {
        private static readonly Dictionary<Type, List<Type>> Conversions = new Dictionary<Type, List<Type>>() {
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
        /// <returns>True if the cast was sucessful, otherwise false.</returns>
        public static bool TryCast(Type to, object value, out object result)
        {
            Contract.Requires<NullReferenceException>(to != null);

            if (value == null)
            {
                var t = to.GetTypeInfo();
                result = null;
                return !t.IsValueType || (t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(Nullable<>)));
            }

            var from = value.GetType();

            if (value == PerspexProperty.UnsetValue)
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
    }
}
