// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2010 Alan McGovern <alan.mcgovern@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;

namespace Tmds.DBus
{
    /// <summary>
    /// Path to D-Bus object.
    /// </summary>
    public struct ObjectPath2 : IComparable, IComparable<ObjectPath2>, IEquatable<ObjectPath2>
    {
        /// <summary>
        /// Root path (<c>"/"</c>).
        /// </summary>
        public static readonly ObjectPath2 Root = new ObjectPath2("/");

        internal readonly string Value;

        /// <summary>
        /// Creates a new ObjectPath.
        /// </summary>
        /// <param name="value">path.</param>
        public ObjectPath2(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            Validate(value);

            this.Value = value;
        }

        static void Validate(string value)
        {
            if (!value.StartsWith("/", StringComparison.Ordinal))
                throw new ArgumentException("value");
            if (value.EndsWith("/", StringComparison.Ordinal) && value.Length > 1)
                throw new ArgumentException("ObjectPath cannot end in '/'");

            bool multipleSlash = false;

            foreach (char c in value)
            {
                bool valid = (c >= 'a' && c <= 'z')
                    || (c >= 'A' && c <= 'Z')
                    || (c >= '0' && c <= '9')
                    || c == '_'
                    || (!multipleSlash && c == '/');

                if (!valid)
                {
                    var message = string.Format("'{0}' is not a valid character in an ObjectPath", c);
                    throw new ArgumentException(message, "value");
                }

                multipleSlash = c == '/';
            }

        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that
        /// indicates whether the current instance precedes, follows, or occurs in the same position in
        /// the sort order as the other object.
        /// </summary>
        public int CompareTo(ObjectPath2 other)
        {
            return Value.CompareTo(other.Value);
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that
        /// indicates whether the current instance precedes, follows, or occurs in the same position in
        /// the sort order as the other object.
        /// </summary>
        public int CompareTo(object otherObject)
        {
            var other = otherObject as ObjectPath2?;

            if (other == null)
                return 1;

            return Value.CompareTo(other.Value.Value);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public bool Equals(ObjectPath2 other)
        {
            return Value == other.Value;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object o)
        {
            var b = o as ObjectPath2?;

            if (b == null)
                return false;

            return Value.Equals(b.Value.Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether two specified ObjectPaths have the same value.
        /// </summary>
        public static bool operator==(ObjectPath2 a, ObjectPath2 b)
        {
            return a.Value == b.Value;
        }

        /// <summary>
        /// Determines whether two specified ObjectPaths have different values.
        /// </summary>
        public static bool operator!=(ObjectPath2 a, ObjectPath2 b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Returns the hash code for this ObjectPath.
        /// </summary>
        public override int GetHashCode()
        {
            if (Value == null)
            {
                return 0;
            }
            return Value.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Creates the ObjectPath that is represented by the string value.
        /// </summary>
        /// <param name="value">path.</param>
        public static implicit operator ObjectPath2(string value)
        {
            return new ObjectPath2(value);
        }

        //this may or may not prove useful
        internal string[] Decomposed
        {
            get
            {
                return Value.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        internal ObjectPath2 Parent
        {
            get
            {
                if (Value == Root.Value)
                    return null;

                string par = Value.Substring(0, Value.LastIndexOf('/'));
                if (par == String.Empty)
                    par = "/";

                return new ObjectPath2(par);
            }
        }
    }
}
