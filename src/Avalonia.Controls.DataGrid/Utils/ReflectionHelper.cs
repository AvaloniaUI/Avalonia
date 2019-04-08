// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Avalonia.Controls.Utils
{
    internal static class TypeHelper
    {
        internal const char LeftIndexerToken = '[';
        internal const char PropertyNameSeparator = '.';
        internal const char RightIndexerToken = ']';

        private static Type FindGenericType(Type definition, Type type)
        {
            while ((type != null) && (type != typeof(object)))
            {
                if (type.IsGenericType && (type.GetGenericTypeDefinition() == definition))
                {
                    return type;
                }
                if (definition.IsInterface)
                {
                    foreach (Type type2 in type.GetInterfaces())
                    {
                        Type type3 = FindGenericType(definition, type2);
                        if (type3 != null)
                        {
                            return type3;
                        }
                    }
                }
                type = type.BaseType;
            }
            return null;
        }

        /// <summary>
        /// Finds an int or string indexer in the specified collection of members, where int indexers take priority
        /// over string indexers.  If found, this method will return the associated PropertyInfo and set the out index
        /// argument to its appropriate value.  If not found, the return value will be null, as will the index.
        /// </summary>
        /// <param name="members">Collection of members to search through for an indexer.</param>
        /// <param name="stringIndex">String value of indexer argument.</param>
        /// <param name="index">Resultant index value.</param>
        /// <returns>Indexer PropertyInfo if found, null otherwise.</returns>
        private static PropertyInfo FindIndexerInMembers(MemberInfo[] members, string stringIndex, out object[] index)
        {
            index = null;
            ParameterInfo[] parameters;
            PropertyInfo stringIndexer = null;

            foreach (PropertyInfo pi in members)
            {
                if (pi == null)
                {
                    continue;
                }

                // Only a single parameter is supported and it must be a string or Int32 value.
                parameters = pi.GetIndexParameters();
                if (parameters.Length > 1)
                {
                    continue;
                }

                if (parameters[0].ParameterType == typeof(int))
                {
                    int intIndex = -1;
                    if (Int32.TryParse(stringIndex.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out intIndex))
                    {
                        index = new object[] { intIndex };
                        return pi;
                    }
                }

                // If string indexer is found save it, in case there is an int indexer.
                if (parameters[0].ParameterType == typeof(string))
                {
                    index = new object[] { stringIndex };
                    stringIndexer = pi;
                }
            }

            return stringIndexer;
        }

        /// <summary>
        /// Gets the default member name that is used for an indexer (e.g. "Item").
        /// </summary>
        /// <param name="type">Type to check.</param>
        /// <returns>Default member name.</returns>
        private static string GetDefaultMemberName(this Type type)
        {
            object[] attributes = type.GetCustomAttributes(typeof(DefaultMemberAttribute), true);
            if (attributes != null && attributes.Length == 1)
            {
                DefaultMemberAttribute defaultMemberAttribute = attributes[0] as DefaultMemberAttribute;
                return defaultMemberAttribute.MemberName;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Finds the PropertyInfo for the specified property path within this Type, and returns
        /// the value of GetShortName on its DisplayAttribute, if one exists. GetShortName will return
        /// the value of Name if there is no ShortName specified.
        /// </summary>
        /// <param name="type">Type to search</param>
        /// <param name="propertyPath">property path</param>
        /// <returns>DisplayAttribute.ShortName if it exists, null otherwise</returns>
        internal static string GetDisplayName(this Type type, string propertyPath)
        {
            PropertyInfo propertyInfo = type.GetNestedProperty(propertyPath);
            if (propertyInfo != null)
            {
                object[] attributes = propertyInfo.GetCustomAttributes(typeof(DisplayAttribute), true);
                if (attributes != null && attributes.Length > 0)
                {
                    Debug.Assert(attributes.Length == 1);
                    if (attributes[0] is DisplayAttribute displayAttribute)
                    {
                        return displayAttribute.GetShortName();
                    }
                }
            }
            return null;
        }

        internal static Type GetEnumerableItemType(this Type enumerableType)
        {
            Type type = FindGenericType(typeof(IEnumerable<>), enumerableType);
            if (type != null)
            {
                return type.GetGenericArguments()[0];
            }
            return enumerableType;
        }

        /// <summary>
        /// Retrieves the value and type of a property. That property can be nested and its path
        /// can include indexers. Each element of the path needs to be a public instance property.
        /// </summary>
        /// <param name="parentType">The parent Type</param>
        /// <param name="propertyPath">Property path</param>
        /// <param name="exception">Potential exception</param>
        /// <param name="item">Parent item which will be set to the property value if non-null.</param>
        /// <returns></returns>
        private static PropertyInfo GetNestedProperty(this Type parentType, string propertyPath, out Exception exception, ref object item)
        {
            exception = null;
            if (parentType == null || String.IsNullOrEmpty(propertyPath))
            {
                item = null;
                return null;
            }

            Type type = parentType;
            PropertyInfo propertyInfo = null;
            List<string> propertyNames = SplitPropertyPath(propertyPath);
            for (int i = 0; i < propertyNames.Count; i++)
            {
                // if we can't find the property or it is not of the correct type,
                // treat it as a null value
                propertyInfo = type.GetPropertyOrIndexer(propertyNames[i], out object[] index);
                if (propertyInfo == null)
                {
                    item = null;
                    return null;
                }

                if (!propertyInfo.CanRead)
                {
                    exception =
                        new InvalidOperationException(
                            $"The property named '{propertyNames[i]}' on type '{type.GetTypeName()}' cannot be read.");
                    item = null;
                    return null;
                }

                if (item != null)
                {
                    item = propertyInfo.GetValue(item, index);
                }
                type = propertyInfo.PropertyType.GetNonNullableType();
            }

            return propertyInfo;
        }

        /// <summary>
        /// Finds the leaf PropertyInfo for the specified property path, and returns its value
        /// if the item is non-null.
        /// </summary>
        /// <param name="parentType">Type to search.</param>
        /// <param name="propertyPath">Property path.</param>
        /// <param name="item">Parent item which will be set to the property value if non-null.</param>
        /// <returns>The PropertyInfo.</returns>
        internal static PropertyInfo GetNestedProperty(this Type parentType, string propertyPath, ref object item)
        {
            return parentType.GetNestedProperty(propertyPath, out Exception ex, ref item);
        }

        internal static PropertyInfo GetNestedProperty(this Type parentType, string propertyPath)
        {
            if (parentType != null)
            {
                object item = null;
                return parentType.GetNestedProperty(propertyPath, ref item);
            }
            return null;
        }

        /// <summary>
        /// Returns the friendly name for a type
        /// </summary>
        /// <param name="type">The type to get the name from</param>
        /// <returns>Textual representation of the input type</returns>
        internal static string GetTypeName(this Type type)
        {
            Type baseType = type.GetNonNullableType();
            string s = baseType.Name;
            if (type != baseType)
            {
                s += '?';
            }
            return s;
        }

        internal static Type GetNestedPropertyType(this Type parentType, string propertyPath)
        {
            if (parentType == null || String.IsNullOrEmpty(propertyPath))
            {
                return parentType;
            }

            PropertyInfo propertyInfo = parentType.GetNestedProperty(propertyPath);
            if (propertyInfo != null)
            {
                return propertyInfo.PropertyType;
            }
            return null;
        }

        /// <summary>
        /// Retrieves the value of a property. That property can be nested and its path can
        /// include indexers. Each element of the path needs to be a public instance property.
        /// The return value will either be of type propertyType or it will be null.
        /// </summary>
        /// <param name="item">Object that exposes the property</param>
        /// <param name="propertyPath">Property path</param>
        /// <param name="propertyType">Property type</param>
        /// <param name="exception">Potential exception</param>
        /// <returns>Property value</returns>
        internal static object GetNestedPropertyValue(object item, string propertyPath, Type propertyType, out Exception exception)
        {
            exception = null;

            // if the item is null, treat the property value as null
            if (item == null)
            {
                return null;
            }

            // if the propertyPath is null or empty, return the item
            if (String.IsNullOrEmpty(propertyPath))
            {
                return item;
            }

            object propertyValue = item;
            Type itemType = item.GetType();
            if (itemType != null)
            {
                PropertyInfo propertyInfo = itemType.GetNestedProperty(propertyPath, out exception, ref propertyValue);
                if (propertyInfo != null && propertyInfo.PropertyType != propertyType)
                {
                    return null;
                }
            }
            return propertyValue;
        }

        /// <summary>
        /// Gets the value of a given property path on a particular data item.
        /// </summary>
        /// <param name="item">Parent data item.</param>
        /// <param name="propertyPath">Property path.</param>
        /// <returns>Value.</returns>
        internal static object GetNestedPropertyValue(object item, string propertyPath)
        {
            if (item != null)
            {
                Type parentType = item.GetType();
                if (String.IsNullOrEmpty(propertyPath))
                {
                    return item;
                }
                else if (parentType != null)
                {
                    object nestedValue = item;
                    parentType.GetNestedProperty(propertyPath, ref nestedValue);
                    return nestedValue;
                }
            }
            return null;
        }

        internal static Type GetNonNullableType(this Type type)
        {
            if (IsNullableType(type))
            {
                return type.GetGenericArguments()[0];
            }
            return type;
        }

        /// <summary>
        /// Returns the PropertyInfo for the specified property path.  If the property path
        /// refers to an indexer (e.g. "[abc]"), then the index out parameter will be set to the value
        /// specified in the property path.  This method only supports indexers with a single parameter
        /// that is either an int or a string.  Int parameters take priority over string parameters.
        /// </summary>
        /// <param name="type">Type to search.</param>
        /// <param name="propertyPath">Property path.</param>
        /// <param name="index">Set to the index if return value is an indexer, otherwise null.</param>
        /// <returns>PropertyInfo for either a property or an indexer.</returns>
        internal static PropertyInfo GetPropertyOrIndexer(this Type type, string propertyPath, out object[] index)
        {
            index = null;
            if (string.IsNullOrEmpty(propertyPath) || propertyPath[0] != LeftIndexerToken)
            {
                // Return the default value of GetProperty if the first character is not an indexer token.
                return type.GetProperty(propertyPath);
            }

            if (propertyPath.Length < 2 || propertyPath[propertyPath.Length - 1] != RightIndexerToken)
            {
                // Return null if the indexer does not meet the standard format (i.e. "[x]").
                return null;
            }

            PropertyInfo indexer = null;
            string stringIndex = propertyPath.Substring(1, propertyPath.Length - 2);
            indexer = FindIndexerInMembers(type.GetDefaultMembers(), stringIndex, out index);
            if (indexer != null)
            {
                // We found the indexer, so return it.
                return indexer;
            }

            if (typeof(IList).IsAssignableFrom(type))
            {
                // If the object is of type IList, try to use its default indexer.
                indexer = FindIndexerInMembers(typeof(IList).GetDefaultMembers(), stringIndex, out index);
            }

            return indexer;
        }

        internal static bool IsEnumerableType(this Type enumerableType)
        {
            return (FindGenericType(typeof(IEnumerable<>), enumerableType) != null);
        }

        internal static bool IsNullableType(this Type type)
        {
            return (((type != null) && type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Nullable<>)));
        }

        internal static bool IsNullableEnum(this Type type)
        {
            return type.IsNullableType() &&
                 type.GetGenericArguments().Length == 1 &&
                 type.GetGenericArguments()[0].IsEnum;
        }

        /// <summary>
        /// If the specified property is an indexer, this method will prepend the object's
        /// default member name to it (e.g. "[foo]" returns "Item[foo]").
        /// </summary>
        /// <param name="item">Declaring data item.</param>
        /// <param name="property">Property name.</param>
        /// <returns>Property with default member name prepended, or property if unchanged.</returns>
        internal static string PrependDefaultMemberName(object item, string property)
        {
            if (item != null && !string.IsNullOrEmpty(property) && property[0] == TypeHelper.LeftIndexerToken)
            {
                // The leaf property name is an indexer, so add the default member name.
                Type declaringType = item.GetType();
                if (declaringType != null)
                {
                    string defaultMemberName = declaringType.GetNonNullableType().GetDefaultMemberName();
                    if (!string.IsNullOrEmpty(defaultMemberName))
                    {
                        return defaultMemberName + property;
                    }
                }
            }
            return property;
        }

        /// <summary>
        /// If the specified property is an indexer, this method will remove the object's
        /// default member name from it (e.g. "Item[foo]" returns "[foo]").
        /// </summary>
        /// <param name="property">Property name.</param>
        /// <returns>Property with default member name removed, or property if unchanged.</returns>
        internal static string RemoveDefaultMemberName(string property)
        {
            if (!string.IsNullOrEmpty(property) && property[property.Length - 1] == TypeHelper.RightIndexerToken)
            {
                // The property is an indexer, so remove the default member name.
                int leftIndexerToken = property.IndexOf(TypeHelper.LeftIndexerToken);
                if (leftIndexerToken >= 0)
                {
                    return property.Substring(leftIndexerToken);
                }
            }
            return property;
        }

        /// <summary>
        /// Returns a list of substrings where each one represents a single property within a nested
        /// property path which may include indexers.  For example, the string "abc.d[efg][h].ijk"
        /// would return the substrings: "abc", "d", "[efg]", "[h]", and "ijk".
        /// </summary>
        /// <param name="propertyPath">Path to split.</param>
        /// <returns>List of property substrings.</returns>
        internal static List<string> SplitPropertyPath(string propertyPath)
        {
            List<string> propertyPaths = new List<string>();
            if (!string.IsNullOrEmpty(propertyPath))
            {
                int startIndex = 0;
                for (int index = 0; index < propertyPath.Length; index++)
                {
                    if (propertyPath[index] == PropertyNameSeparator)
                    {
                        propertyPaths.Add(propertyPath.Substring(startIndex, index - startIndex));
                        startIndex = index + 1;
                    }
                    else if (startIndex != index && propertyPath[index] == LeftIndexerToken)
                    {
                        propertyPaths.Add(propertyPath.Substring(startIndex, index - startIndex));
                        startIndex = index;
                    }
                    else if (index == propertyPath.Length - 1)
                    {
                        propertyPaths.Add(propertyPath.Substring(startIndex));
                    }
                }
            }
            return propertyPaths;
        }

        /// <summary>
        /// Checks a MemberInfo object (e.g. a Type or PropertyInfo) for the ReadOnly attribute
        /// and returns the value of IsReadOnly if it exists.
        /// </summary>
        /// <param name="memberInfo">MemberInfo to check</param>
        /// <returns>true if MemberInfo is read-only, false otherwise</returns>
        internal static bool GetIsReadOnly(this MemberInfo memberInfo)
        {
            if (memberInfo != null)
            {
                // Check if ReadOnlyAttribute is defined on the member
                object[] attributes = memberInfo.GetCustomAttributes(typeof(ReadOnlyAttribute), true);
                if (attributes != null && attributes.Length > 0)
                {
                    ReadOnlyAttribute readOnlyAttribute = attributes[0] as ReadOnlyAttribute;
                    Debug.Assert(readOnlyAttribute != null);
                    return readOnlyAttribute.IsReadOnly;
                }
            }
            return false;
        }

        internal static Type GetItemType(this IEnumerable list)
        {
            Type listType = list.GetType();
            Type itemType = null;

            // if it's a generic enumerable, we get the generic type

            // Unfortunately, if data source is fed from a bare IEnumerable, TypeHelper will report an element type of object,
            // which is not particularly interesting.  We deal with it further on.
            if (listType.IsEnumerableType())
            {
                itemType = listType.GetEnumerableItemType();
            }

            // Bare IEnumerables mean that result type will be object.  In that case, we try to get something more interesting
            if (itemType == null || itemType == typeof(object))
            {
                // We haven't located a type yet.. try a different approach.
                // Does the list have anything in it?

                IEnumerator en = list.GetEnumerator();
                if (en.MoveNext() && en.Current != null)
                {
                    return en.Current.GetType();
                }
            }

            // if we're null at this point, give up
            return itemType;
        }
    }
}
