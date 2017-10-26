/*
Copyright (c) 2010, Karl Seguin - http://www.openmymind.net/
All rights reserved.
 
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the <organization> nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.
 
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

Code imported from https://github.com/elaberge/Metsys.Bson without any changes

*/

using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System;
using System.Runtime.Serialization;

using Metsys.Bson.Configuration;
// ReSharper disable All

namespace Metsys.Bson
{
    internal enum Types
    {
        Double = 1,
        String = 2,
        Object = 3,
        Array = 4,
        Binary = 5,
        Undefined = 6,
        ObjectId = 7,
        Boolean = 8,
        DateTime = 9,
        Null = 10,
        Regex = 11,
        Reference = 12,
        Code = 13,
        Symbol = 14,
        ScopedCode = 15,
        Int32 = 16,
        Timestamp = 17,
        Int64 = 18,
    }
}

namespace Metsys.Bson
{

    internal class Serializer
    {
        private static readonly IDictionary<Type, Types> _typeMap = new Dictionary<Type, Types>
                                                                            {
                                                                                {typeof (int), Types.Int32},
                                                                                {typeof (long), Types.Int64},
                                                                                {typeof (bool), Types.Boolean},
                                                                                {typeof (string), Types.String},
                                                                                {typeof (double), Types.Double},
                                                                                {typeof (Guid), Types.Binary},
                                                                                {typeof (Regex), Types.Regex},
                                                                                {typeof (DateTime), Types.DateTime},
                                                                                {typeof (float), Types.Double},
                                                                                {typeof (byte[]), Types.Binary},
                                                                                {typeof (ObjectId), Types.ObjectId},
                                                                                {typeof (ScopedCode), Types.ScopedCode}
                                                                            };

        private readonly BinaryWriter _writer;
        private Document _current;

        public static byte[] Serialize<T>(T document)
        {
            var type = document.GetType();
            if (type.IsValueType ||
                (typeof(IEnumerable).IsAssignableFrom(type) && typeof(IDictionary).IsAssignableFrom(type) == false)
            )
            {
                throw new BsonException("Root type must be an object");
            }
            using (var ms = new MemoryStream(250))
            using (var writer = new BinaryWriter(ms))
            {
                new Serializer(writer).WriteDocument(document);
                return ms.ToArray();
            }
        }
        public static byte[] Serialize(object document)
        {
            var type = document.GetType();
            if (type.IsValueType ||
                (typeof(IEnumerable).IsAssignableFrom(type) && typeof(IDictionary).IsAssignableFrom(type) == false)
            )
            {
                throw new BsonException("Root type must be an object");
            }
            using (var ms = new MemoryStream(250))
            using (var writer = new BinaryWriter(ms))
            {
                new Serializer(writer).WriteDocument(document);
                return ms.ToArray();
            }
        }

        private Serializer(BinaryWriter writer)
        {
            _writer = writer;
        }

        private void NewDocument()
        {
            var old = _current;
            _current = new Document { Parent = old, Length = (int)_writer.BaseStream.Position, Digested = 4 };
            _writer.Write(0); // length placeholder
        }
        private void EndDocument(bool includeEeo)
        {
            var old = _current;
            if (includeEeo)
            {
                Written(1);
                _writer.Write((byte)0);
            }

            _writer.Seek(_current.Length, SeekOrigin.Begin);
            _writer.Write(_current.Digested); // override the document length placeholder
            _writer.Seek(0, SeekOrigin.End); // back to the end
            _current = _current.Parent;
            if (_current != null)
            {
                Written(old.Digested);
            }
        }

        private void Written(int length)
        {
            _current.Digested += length;
        }

        private void WriteDocument(object document)
        {
            NewDocument();
            WriteObject(document);
            EndDocument(true);
        }

        private void WriteObject(object document)
        {
            var asDictionary = document as IDictionary;
            if (asDictionary != null)
            {
                Write(asDictionary);
                return;
            }

            var typeHelper = TypeHelper.GetHelperForType(document.GetType());
            foreach (var property in typeHelper.GetProperties())
            {
                if (property.Ignored) { continue; }
                var name = property.Name;
                var value = property.Getter(document);
                if (value == null && property.IgnoredIfNull)
                {
                    continue;
                }
                SerializeMember(name, value);
            }
        }

        private void SerializeMember(string name, object value)
        {
            if (value == null)
            {
                Write(Types.Null);
                WriteName(name);
                return;
            }

            var type = value.GetType();
            if (type.IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            Types storageType;
            if (!_typeMap.TryGetValue(type, out storageType))
            {
                // this isn't a simple type;
                Write(name, value);
                return;
            }

            Write(storageType);
            WriteName(name);
            switch (storageType)
            {
                case Types.Int32:
                    Written(4);
                    _writer.Write((int)value);
                    return;
                case Types.Int64:
                    Written(8);
                    _writer.Write((long)value);
                    return;
                case Types.String:
                    Write((string)value);
                    return;
                case Types.Double:
                    Written(8);
                    if (value is float)
                    {
                        _writer.Write(Convert.ToDouble((float)value));
                    }
                    else
                    {
                        _writer.Write((double)value);
                    }

                    return;
                case Types.Boolean:
                    Written(1);
                    _writer.Write((bool)value ? (byte)1 : (byte)0);
                    return;
                case Types.DateTime:
                    Written(8);
                    _writer.Write((long)((DateTime)value).ToUniversalTime().Subtract(Helper.Epoch).TotalMilliseconds);
                    return;
                case Types.Binary:
                    WriteBinnary(value);
                    return;
                case Types.ScopedCode:
                    Write((ScopedCode)value);
                    return;
                case Types.ObjectId:
                    Written(((ObjectId)value).Value.Length);
                    _writer.Write(((ObjectId)value).Value);
                    return;
                case Types.Regex:
                    Write((Regex)value);
                    break;
            }
        }

        private void Write(string name, object value)
        {
            if (value is IDictionary)
            {
                Write(Types.Object);
                WriteName(name);
                NewDocument();
                Write((IDictionary)value);
                EndDocument(true);
            }
            else if (value is IEnumerable)
            {
                Write(Types.Array);
                WriteName(name);
                NewDocument();
                Write((IEnumerable)value);
                EndDocument(true);
            }
            else
            {
                Write(Types.Object);
                WriteName(name);
                WriteDocument(value); // Write manages new/end document
            }
        }

        private void Write(IEnumerable enumerable)
        {
            var index = 0;
            foreach (var value in enumerable)
            {
                SerializeMember((index++).ToString(), value);
            }
        }

        private void Write(IDictionary dictionary)
        {
            foreach (var key in dictionary.Keys)
            {
                SerializeMember((string)key, dictionary[key]);
            }
        }

        private void WriteBinnary(object value)
        {
            if (value is byte[])
            {
                var bytes = (byte[])value;
                var length = bytes.Length;
                _writer.Write(length + 4);
                _writer.Write((byte)2);
                _writer.Write(length);
                _writer.Write(bytes);
                Written(9 + length);
            }
            else if (value is Guid)
            {
                var guid = (Guid)value;
                var bytes = guid.ToByteArray();
                _writer.Write(bytes.Length);
                _writer.Write((byte)3);
                _writer.Write(bytes);
                Written(5 + bytes.Length);
            }
        }

        private void Write(Types type)
        {
            _writer.Write((byte)type);
            Written(1);
        }

        private void WriteName(string name)
        {
            var bytes = Encoding.UTF8.GetBytes(name);
            _writer.Write(bytes);
            _writer.Write((byte)0);
            Written(bytes.Length + 1);
        }

        private void Write(string name)
        {
            var bytes = Encoding.UTF8.GetBytes(name);
            _writer.Write(bytes.Length + 1);
            _writer.Write(bytes);
            _writer.Write((byte)0);
            Written(bytes.Length + 5); // stringLength + length + null byte
        }

        private void Write(Regex regex)
        {
            WriteName(regex.ToString());

            var options = string.Empty;
            if ((regex.Options & RegexOptions.ECMAScript) == RegexOptions.ECMAScript)
            {
                options = string.Concat(options, 'e');
            }

            if ((regex.Options & RegexOptions.IgnoreCase) == RegexOptions.IgnoreCase)
            {
                options = string.Concat(options, 'i');
            }

            if ((regex.Options & RegexOptions.CultureInvariant) == RegexOptions.CultureInvariant)
            {
                options = string.Concat(options, 'l');
            }

            if ((regex.Options & RegexOptions.Multiline) == RegexOptions.Multiline)
            {
                options = string.Concat(options, 'm');
            }

            if ((regex.Options & RegexOptions.Singleline) == RegexOptions.Singleline)
            {
                options = string.Concat(options, 's');
            }

            options = string.Concat(options, 'u'); // all .net regex are unicode regex, therefore:
            if ((regex.Options & RegexOptions.IgnorePatternWhitespace) == RegexOptions.IgnorePatternWhitespace)
            {
                options = string.Concat(options, 'w');
            }

            if ((regex.Options & RegexOptions.ExplicitCapture) == RegexOptions.ExplicitCapture)
            {
                options = string.Concat(options, 'x');
            }

            WriteName(options);
        }

        private void Write(ScopedCode value)
        {
            NewDocument();
            Write(value.CodeString);
            WriteDocument(value.Scope);
            EndDocument(false);
        }
    }
}
namespace Metsys.Bson
{
    internal class ScopedCode
    {
        public string CodeString { get; set; }
        public object Scope { get; set; }
    }

    internal class ScopedCode<T> : ScopedCode
    {
        public new T Scope { get; set; }
    }
}

namespace Metsys.Bson
{
    internal static class ObjectIdGenerator
    {
        private static readonly object _inclock = new object();

        private static int _counter;
        private static readonly byte[] _machineHash = GenerateHostHash();
        private static readonly byte[] _processId = BitConverter.GetBytes(GenerateProcId());

        public static byte[] Generate()
        {
            var oid = new byte[12];
            var copyidx = 0;

            Array.Copy(BitConverter.GetBytes(GenerateTime()), 0, oid, copyidx, 4);
            copyidx += 4;

            Array.Copy(_machineHash, 0, oid, copyidx, 3);
            copyidx += 3;

            Array.Copy(_processId, 0, oid, copyidx, 2);
            copyidx += 2;

            Array.Copy(BitConverter.GetBytes(GenerateInc()), 0, oid, copyidx, 3);
            return oid;
        }

        private static int GenerateTime()
        {
            var now = DateTime.Now.ToUniversalTime();

            var nowtime = new DateTime(Helper.Epoch.Year, Helper.Epoch.Month, Helper.Epoch.Day, now.Hour, now.Minute, now.Second, now.Millisecond);
            var diff = nowtime - Helper.Epoch;
            return Convert.ToInt32(Math.Floor(diff.TotalMilliseconds));
        }

        private static int GenerateInc()
        {
            lock (_inclock)
            {
                return _counter++;
            }
        }

        private static byte[] GenerateHostHash()
        {
            using (var md5 = MD5.Create())
            {
                var host = Dns.GetHostName();
                return md5.ComputeHash(Encoding.Default.GetBytes(host));
            }
        }
        private static int GenerateProcId()
        {
            var proc = Process.GetCurrentProcess();
            return proc.Id;
        }
    }
}

namespace Metsys.Bson
{
    internal class ObjectId
    {
        private string _string;

        public ObjectId()
        {
        }

        public ObjectId(string value) : this(DecodeHex(value))
        {
        }

        internal ObjectId(byte[] value)
        {
            Value = value;
        }

        public static ObjectId Empty
        {
            get { return new ObjectId("000000000000000000000000"); }
        }

        public byte[] Value { get; private set; }

        public static ObjectId NewObjectId()
        {
            // TODO: generate random-ish bits.
            return new ObjectId { Value = ObjectIdGenerator.Generate() };
        }

        public static bool TryParse(string value, out ObjectId id)
        {
            id = Empty;
            if (value == null || value.Length != 24)
            {
                return false;
            }

            try
            {
                id = new ObjectId(value);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static bool operator ==(ObjectId a, ObjectId b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(ObjectId a, ObjectId b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return Value != null ? ToString().GetHashCode() : 0;
        }

        public override string ToString()
        {
            if (_string == null && Value != null)
            {
                _string = BitConverter.ToString(Value).Replace("-", string.Empty).ToLower();
            }

            return _string;
        }

        public override bool Equals(object o)
        {
            var other = o as ObjectId;
            return Equals(other);
        }

        public bool Equals(ObjectId other)
        {
            return other != null && ToString() == other.ToString();
        }

        protected static byte[] DecodeHex(string val)
        {
            var chars = val.ToCharArray();
            var numberChars = chars.Length;
            var bytes = new byte[numberChars / 2];

            for (var i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(new string(chars, i, 2), 16);
            }

            return bytes;
        }

        public static implicit operator string(ObjectId oid)
        {
            return oid == null ? null : oid.ToString();
        }
        public static implicit operator ObjectId(string oidString)
        {
            return new ObjectId(oidString);
        }
    }
}

namespace Metsys.Bson
{
    public interface IExpando
    {
        IDictionary<string, object> Expando { get; }
    }
}

namespace Metsys.Bson
{
    internal class MagicProperty
    {
        private readonly PropertyInfo _property;
        private readonly string _name;
        private readonly bool _ignored;
        public readonly bool _ignoredIfNull;

        public Type Type
        {
            get { return _property.PropertyType; }
        }
        public string Name
        {
            get { return _name; }
        }
        public bool Ignored
        {
            get { return _ignored; }
        }
        public bool IgnoredIfNull
        {
            get { return _ignoredIfNull; }
        }

        public Action<object, object> Setter { get; private set; }

        public Func<object, object> Getter { get; private set; }

        public MagicProperty(PropertyInfo property, string name, bool ignored, bool ignoredIfNull)
        {
            _property = property;
            _name = name;
            _ignored = ignored;
            _ignoredIfNull = ignoredIfNull;
            Getter = CreateGetterMethod(property);
            Setter = CreateSetterMethod(property);
        }

        private static Action<object, object> CreateSetterMethod(PropertyInfo property)
        {
            var genericHelper = typeof(MagicProperty).GetMethod("SetterMethod", BindingFlags.Static | BindingFlags.NonPublic);
            var constructedHelper = genericHelper.MakeGenericMethod(property.DeclaringType, property.PropertyType);
            return (Action<object, object>)constructedHelper.Invoke(null, new object[] { property });
        }

        private static Func<object, object> CreateGetterMethod(PropertyInfo property)
        {
            var genericHelper = typeof(MagicProperty).GetMethod("GetterMethod", BindingFlags.Static | BindingFlags.NonPublic);
            var constructedHelper = genericHelper.MakeGenericMethod(property.DeclaringType, property.PropertyType);
            return (Func<object, object>)constructedHelper.Invoke(null, new object[] { property });
        }

        //called via reflection       
        private static Action<object, object> SetterMethod<TTarget, TParam>(PropertyInfo method) where TTarget : class
        {
            var m = method.GetSetMethod(true);
            if (m == null) { return null; } //no setter
            var func = (Action<TTarget, TParam>)Delegate.CreateDelegate(typeof(Action<TTarget, TParam>), m);
            return (target, param) => func((TTarget)target, (TParam)param);
        }

        //called via reflection
        private static Func<object, object> GetterMethod<TTarget, TParam>(PropertyInfo method) where TTarget : class
        {
            var m = method.GetGetMethod(true);
            var func = (Func<TTarget, TParam>)Delegate.CreateDelegate(typeof(Func<TTarget, TParam>), m);
            return target => func((TTarget)target);
        }
    }
}

namespace Metsys.Bson
{
    internal class TypeHelper
    {
        private static readonly IDictionary<Type, TypeHelper> _cachedTypeLookup = new Dictionary<Type, TypeHelper>();
        private static readonly BsonConfiguration _configuration = BsonConfiguration.Instance;

        private readonly IDictionary<string, MagicProperty> _properties;

        private TypeHelper(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            _properties = LoadMagicProperties(type, properties);
            if (typeof(IExpando).IsAssignableFrom(type))
            {
                Expando = _properties["Expando"];
            }
        }

        public MagicProperty Expando { get; private set; }

        public ICollection<MagicProperty> GetProperties()
        {
            return _properties.Values;
        }

        public MagicProperty FindProperty(string name)
        {
            return _properties.ContainsKey(name) ? _properties[name] : null;
        }

        public static TypeHelper GetHelperForType(Type type)
        {
            TypeHelper helper;
            if (!_cachedTypeLookup.TryGetValue(type, out helper))
            {
                helper = new TypeHelper(type);
                _cachedTypeLookup[type] = helper;
            }
            return helper;
        }

        public static string FindProperty(LambdaExpression lambdaExpression)
        {
            Expression expressionToCheck = lambdaExpression;

            var done = false;
            while (!done)
            {
                switch (expressionToCheck.NodeType)
                {
                    case ExpressionType.Convert:
                        expressionToCheck = ((UnaryExpression)expressionToCheck).Operand;
                        break;

                    case ExpressionType.Lambda:
                        expressionToCheck = ((LambdaExpression)expressionToCheck).Body;
                        break;

                    case ExpressionType.MemberAccess:
                        var memberExpression = (MemberExpression)expressionToCheck;

                        if (memberExpression.Expression.NodeType != ExpressionType.Parameter && memberExpression.Expression.NodeType != ExpressionType.Convert)
                        {
                            throw new ArgumentException(string.Format("Expression '{0}' must resolve to top-level member.", lambdaExpression), "lambdaExpression");
                        }
                        return memberExpression.Member.Name;
                    default:
                        done = true;
                        break;
                }
            }

            return null;
        }

        public static PropertyInfo FindProperty(Type type, string name)
        {
            return type.GetProperties().Where(p => p.Name == name).First();
        }

        private static IDictionary<string, MagicProperty> LoadMagicProperties(Type type, IEnumerable<PropertyInfo> properties)
        {
            var magic = new Dictionary<string, MagicProperty>(StringComparer.CurrentCultureIgnoreCase);
            foreach (var property in properties)
            {
                if (property.GetIndexParameters().Length > 0)
                {
                    continue;
                }
                var name = _configuration.AliasFor(type, property.Name);
                var ignored = _configuration.IsIgnored(type, property.Name);
                var ignoredIfNull = _configuration.IsIgnoredIfNull(type, property.Name);
                magic.Add(name, new MagicProperty(property, name, ignored, ignoredIfNull));
            }
            return magic;
        }
    }
}

namespace Metsys.Bson
{
    internal class ListWrapper : BaseWrapper
    {
        private IList _list;

        public override object Collection
        {
            get { return _list; }
        }
        public override void Add(object value)
        {
            _list.Add(value);
        }

        protected override object CreateContainer(Type type, Type itemType)
        {
            if (type.IsInterface)
            {
                return Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
            }
            if (type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null) != null)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
        protected override void SetContainer(object container)
        {
            _list = container == null ? new ArrayList() : (IList)container;
        }
    }
}

namespace Metsys.Bson
{
    internal static class ListHelper
    {
        public static Type GetListItemType(Type enumerableType)
        {
            if (enumerableType.IsArray)
            {
                return enumerableType.GetElementType();
            }
            return enumerableType.IsGenericType ? enumerableType.GetGenericArguments()[0] : typeof(object);
        }

        public static Type GetDictionarKeyType(Type enumerableType)
        {
            return enumerableType.IsGenericType
                ? enumerableType.GetGenericArguments()[0]
                : typeof(object);
        }

        public static Type GetDictionarValueType(Type enumerableType)
        {
            return enumerableType.IsGenericType
                ? enumerableType.GetGenericArguments()[1]
                : typeof(object);
        }

        public static IDictionary CreateDictionary(Type dictionaryType, Type keyType, Type valueType)
        {
            if (dictionaryType.IsInterface)
            {
                return (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType));
            }

            if (dictionaryType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, new Type[0], null) != null)
            {
                return (IDictionary)Activator.CreateInstance(dictionaryType);
            }

            return new Dictionary<object, object>();
        }
    }
}

namespace Metsys.Bson
{
    internal class CollectionWrapper<T> : BaseWrapper
    {
        private ICollection<T> _list;

        public override object Collection
        {
            get { return _list; }
        }

        public override void Add(object value)
        {
            _list.Add((T)value);
        }

        protected override object CreateContainer(Type type, Type itemType)
        {
            return Activator.CreateInstance(type);
        }
        protected override void SetContainer(object container)
        {
            _list = (ICollection<T>)container;
        }
    }
}

namespace Metsys.Bson
{
    internal abstract class BaseWrapper
    {
        public static BaseWrapper Create(Type type, Type itemType, object existingContainer)
        {
            var instance = CreateWrapperFromType(existingContainer == null ? type : existingContainer.GetType(), itemType);
            instance.SetContainer(existingContainer ?? instance.CreateContainer(type, itemType));
            return instance;
        }

        private static BaseWrapper CreateWrapperFromType(Type type, Type itemType)
        {
            if (type.IsArray)
            {
                return (BaseWrapper)Activator.CreateInstance(typeof(ArrayWrapper<>).MakeGenericType(itemType));
            }

            var isCollection = false;
            var types = new List<Type>(type.GetInterfaces().Select(h => h.IsGenericType ? h.GetGenericTypeDefinition() : h));
            types.Insert(0, type.IsGenericType ? type.GetGenericTypeDefinition() : type);
            foreach (var @interface in types)
            {
                if (typeof(IList<>).IsAssignableFrom(@interface) || typeof(IList).IsAssignableFrom(@interface))
                {
                    return new ListWrapper();
                }
                if (typeof(ICollection<>).IsAssignableFrom(@interface))
                {
                    isCollection = true;
                }
            }
            if (isCollection)
            {
                return (BaseWrapper)Activator.CreateInstance(typeof(CollectionWrapper<>).MakeGenericType(itemType));
            }

            //a last-ditch pass
            foreach (var @interface in types)
            {
                if (typeof(IEnumerable<>).IsAssignableFrom(@interface) || typeof(IEnumerable).IsAssignableFrom(@interface))
                {
                    return new ListWrapper();
                }
            }
            throw new BsonException(string.Format("Collection of type {0} cannot be deserialized", type.FullName));
        }

        public abstract void Add(object value);
        public abstract object Collection { get; }

        protected abstract object CreateContainer(Type type, Type itemType);
        protected abstract void SetContainer(object container);
    }
}

namespace Metsys.Bson
{

    internal class ArrayWrapper<T> : BaseWrapper
    {
        private readonly List<T> _list = new List<T>();

        public override void Add(object value)
        {
            _list.Add((T)value);
        }

        protected override object CreateContainer(Type type, Type itemType)
        {
            return null;
        }

        protected override void SetContainer(object container)
        {
            if (container != null)
            {
                throw new BsonException("An container cannot exist when trying to deserialize an array");
            }
        }

        public override object Collection
        {
            get
            {
                return _list.ToArray();
            }
        }
    }
}

namespace Metsys.Bson
{
    public static class Helper
    {
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
namespace Metsys.Bson
{
    internal class Document
    {
        public int Length;
        public Document Parent;
        public int Digested;
    }
}

namespace Metsys.Bson
{
    internal class Deserializer
    {
        internal class Options
        {
            public bool LongIntegers { get; set; }
            public bool StringDates { get; set; }
        }

        private readonly static IDictionary<Types, Type> _typeMap = new Dictionary<Types, Type>
        {
            {Types.Int32, typeof(int)},
            {Types.Int64, typeof (long)},
            {Types.Boolean, typeof (bool)},
            {Types.String, typeof (string)},
            {Types.Double, typeof(double)},
            {Types.Binary, typeof (byte[])},
            {Types.Regex, typeof (Regex)},
            {Types.DateTime, typeof (DateTime)},
            {Types.ObjectId, typeof(ObjectId)},
            {Types.Array, typeof(List<object>)},
            {Types.Object, typeof(Dictionary<string, object>)},
            {Types.Null, null},
        };
        private readonly BinaryReader _reader;
        private Document _current;

        private Deserializer(BinaryReader reader)
        {
            _reader = reader;
        }

        public static T Deserialize<T>(byte[] objectData, Options options = null) where T : class
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(objectData, 0, objectData.Length);
                ms.Position = 0;
                return Deserialize<T>(new BinaryReader(ms), options ?? new Options());
            }
        }

        private static T Deserialize<T>(BinaryReader stream, Options options)
        {
            return new Deserializer(stream).Read<T>(options);
        }

        private T Read<T>(Options options)
        {
            NewDocument(_reader.ReadInt32());
            var @object = (T)DeserializeValue(typeof(T), Types.Object, options);
            return @object;
        }

        public static object Deserialize(BinaryReader stream, Type t, Options options = null)
        {
            return new Deserializer(stream).Read(t, options ?? new Options());
        }

        object Read(Type t, Options options)
        {
            NewDocument(_reader.ReadInt32());
            return DeserializeValue(t, Types.Object, options);
        }

        private void Read(int read)
        {
            _current.Digested += read;
        }

        private bool IsDone()
        {
            var isDone = _current.Digested + 1 == _current.Length;
            if (isDone)
            {
                _reader.ReadByte(); // EOO
                var old = _current;
                _current = old.Parent;
                if (_current != null) { Read(old.Length); }
            }
            return isDone;
        }

        private void NewDocument(int length)
        {
            var old = _current;
            _current = new Document { Length = length, Parent = old, Digested = 4 };
        }

        private object DeserializeValue(Type type, Types storedType, Options options)
        {
            return DeserializeValue(type, storedType, null, options);
        }

        private object DeserializeValue(Type type, Types storedType, object container, Options options)
        {
            if (storedType == Types.Null)
            {
                return null;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }
            if (type == typeof(string))
            {
                return ReadString();
            }
            if (type == typeof(int))
            {
                var val = ReadInt(storedType);
                return options.LongIntegers ? (object)(long)val : (object)val;
            }
            if (type.IsEnum)
            {
                return ReadEnum(type, storedType);
            }
            if (type == typeof(float))
            {
                Read(8);
                return (float)_reader.ReadDouble();
            }
            if (storedType == Types.Binary)
            {
                return ReadBinary();
            }
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return ReadList(type, container, options);
            }
            if (type == typeof(bool))
            {
                Read(1);
                return _reader.ReadBoolean();
            }
            if (type == typeof(DateTime))
            {
                var value = Helper.Epoch.AddMilliseconds(ReadLong(Types.Int64));
                return options.StringDates ? value.ToString("s", System.Globalization.CultureInfo.InvariantCulture) : (object)value;
            }
            if (type == typeof(ObjectId))
            {
                Read(12);
                return new ObjectId(_reader.ReadBytes(12));
            }
            if (type == typeof(long))
            {
                return ReadLong(storedType);
            }
            if (type == typeof(double))
            {
                Read(8);
                return _reader.ReadDouble();
            }
            if (type == typeof(Regex))
            {
                return ReadRegularExpression();
            }
            if (type == typeof(ScopedCode))
            {
                return ReadScopedCode(options);
            }
            return ReadObject(type, options);
        }

        private object ReadObject(Type type, Options options)
        {
            var instance = Activator.CreateInstance(type, true);
            var typeHelper = TypeHelper.GetHelperForType(type);
            while (true)
            {
                var storageType = ReadType();
                var name = ReadName();
                var isNull = false;
                if (storageType == Types.Object)
                {
                    var length = _reader.ReadInt32();
                    if (length == 5)
                    {
                        _reader.ReadByte(); //eoo
                        Read(5);
                        isNull = true;
                    }
                    else
                    {
                        NewDocument(length);
                    }
                }
                object container = null;
                var property = typeHelper.FindProperty(name);
                var propertyType = property != null ? property.Type : _typeMap.ContainsKey(storageType) ? _typeMap[storageType] : typeof(object);
                if (property == null && typeHelper.Expando == null)
                {
                    throw new BsonException(string.Format("Deserialization failed: type {0} does not have a property named {1}", type.FullName, name));
                }
                if (property != null && property.Setter == null)
                {
                    container = property.Getter(instance);
                }
                var value = isNull ? null : DeserializeValue(propertyType, storageType, container, options);
                if (property == null)
                {
                    ((IDictionary<string, object>)typeHelper.Expando.Getter(instance))[name] = value;
                }
                else if (container == null && value != null && !property.Ignored)
                {
                    property.Setter(instance, value);
                }
                if (IsDone())
                {
                    break;
                }
            }
            return instance;
        }

        private object ReadList(Type listType, object existingContainer, Options options)
        {
            if (IsDictionary(listType))
            {
                return ReadDictionary(listType, existingContainer, options);
            }

            NewDocument(_reader.ReadInt32());
            var itemType = ListHelper.GetListItemType(listType);
            var isObject = typeof(object) == itemType;
            var wrapper = BaseWrapper.Create(listType, itemType, existingContainer);

            while (!IsDone())
            {
                var storageType = ReadType();
                ReadName();
                if (storageType == Types.Object)
                {
                    NewDocument(_reader.ReadInt32());
                }
                var specificItemType = isObject ? _typeMap[storageType] : itemType;
                var value = DeserializeValue(specificItemType, storageType, options);
                wrapper.Add(value);
            }
            return wrapper.Collection;
        }

        private static bool IsDictionary(Type type)
        {
            var types = new List<Type>(type.GetInterfaces());
            types.Insert(0, type);
            foreach (var interfaceType in types)
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    return true;
                }
            }
            return false;
        }

        private object ReadDictionary(Type listType, object existingContainer, Options options)
        {
            var valueType = ListHelper.GetDictionarValueType(listType);
            var isObject = typeof(object) == valueType;
            var container = existingContainer == null ? ListHelper.CreateDictionary(listType, ListHelper.GetDictionarKeyType(listType), valueType) : (IDictionary)existingContainer;

            while (!IsDone())
            {
                var storageType = ReadType();

                var key = ReadName();
                if (storageType == Types.Object)
                {
                    NewDocument(_reader.ReadInt32());
                }
                var specificItemType = isObject ? _typeMap[storageType] : valueType;
                var value = DeserializeValue(specificItemType, storageType, options);
                container.Add(key, value);
            }
            return container;
        }

        private object ReadBinary()
        {
            var length = _reader.ReadInt32();
            var subType = _reader.ReadByte();
            Read(5 + length);
            if (subType == 2)
            {
                return _reader.ReadBytes(_reader.ReadInt32());
            }
            if (subType == 3)
            {
                return new Guid(_reader.ReadBytes(length));
            }
            throw new BsonException("No support for binary type: " + subType);
        }

        private string ReadName()
        {
            var buffer = new List<byte>(128); //todo: use a pool to prevent fragmentation
            byte b;
            while ((b = _reader.ReadByte()) > 0)
            {
                buffer.Add(b);
            }
            Read(buffer.Count + 1);
            return Encoding.UTF8.GetString(buffer.ToArray());
        }

        private string ReadString()
        {
            var length = _reader.ReadInt32();
            var buffer = _reader.ReadBytes(length - 1); //todo: again, look at fragementation prevention
            _reader.ReadByte(); //null;
            Read(4 + length);

            return Encoding.UTF8.GetString(buffer);
        }

        private int ReadInt(Types storedType)
        {
            switch (storedType)
            {
                case Types.Int32:
                    Read(4);
                    return _reader.ReadInt32();
                case Types.Int64:
                    Read(8);
                    return (int)_reader.ReadInt64();
                case Types.Double:
                    Read(8);
                    return (int)_reader.ReadDouble();
                default:
                    throw new BsonException("Could not create an int from " + storedType);
            }
        }

        private long ReadLong(Types storedType)
        {
            switch (storedType)
            {
                case Types.Int32:
                    Read(4);
                    return _reader.ReadInt32();
                case Types.Int64:
                    Read(8);
                    return _reader.ReadInt64();
                case Types.Double:
                    Read(8);
                    return (long)_reader.ReadDouble();
                default:
                    throw new BsonException("Could not create an int64 from " + storedType);
            }
        }

        private object ReadEnum(Type type, Types storedType)
        {
            if (storedType == Types.Int64)
            {
                return Enum.Parse(type, ReadLong(storedType).ToString(), false);
            }
            return Enum.Parse(type, ReadInt(storedType).ToString(), false);
        }

        private object ReadRegularExpression()
        {
            var pattern = ReadName();
            var optionsString = ReadName();

            var options = RegexOptions.None;
            if (optionsString.Contains("e")) options = options | RegexOptions.ECMAScript;
            if (optionsString.Contains("i")) options = options | RegexOptions.IgnoreCase;
            if (optionsString.Contains("l")) options = options | RegexOptions.CultureInvariant;
            if (optionsString.Contains("m")) options = options | RegexOptions.Multiline;
            if (optionsString.Contains("s")) options = options | RegexOptions.Singleline;
            if (optionsString.Contains("w")) options = options | RegexOptions.IgnorePatternWhitespace;
            if (optionsString.Contains("x")) options = options | RegexOptions.ExplicitCapture;

            return new Regex(pattern, options);
        }

        private Types ReadType()
        {
            Read(1);
            return (Types)_reader.ReadByte();
        }

        private ScopedCode ReadScopedCode(Options options)
        {
            _reader.ReadInt32(); //length
            Read(4);
            var name = ReadString();
            NewDocument(_reader.ReadInt32());
            return new ScopedCode { CodeString = name, Scope = DeserializeValue(typeof(object), Types.Object, options) };
        }
    }
}

namespace Metsys.Bson.Configuration
{
    public interface ITypeConfiguration<T>
    {
        ITypeConfiguration<T> UseAlias(Expression<Func<T, object>> expression, string alias);
        ITypeConfiguration<T> Ignore(Expression<Func<T, object>> expression);
        ITypeConfiguration<T> Ignore(string name);
        ITypeConfiguration<T> IgnoreIfNull(Expression<Func<T, object>> expression);
    }

    internal class TypeConfiguration<T> : ITypeConfiguration<T>
    {
        private readonly BsonConfiguration _configuration;

        internal TypeConfiguration(BsonConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ITypeConfiguration<T> UseAlias(Expression<Func<T, object>> expression, string alias)
        {
            var member = expression.GetMemberExpression();
            _configuration.AddMap<T>(member.GetName(), alias);
            return this;
        }

        public ITypeConfiguration<T> Ignore(Expression<Func<T, object>> expression)
        {
            var member = expression.GetMemberExpression();
            return Ignore(member.GetName());
        }

        public ITypeConfiguration<T> Ignore(string name)
        {
            _configuration.AddIgnore<T>(name);
            return this;
        }

        public ITypeConfiguration<T> IgnoreIfNull(Expression<Func<T, object>> expression)
        {
            var member = expression.GetMemberExpression();
            _configuration.AddIgnoreIfNull<T>(member.GetName());
            return this;
        }
    }
}

namespace Metsys.Bson.Configuration
{
    public static class ExpressionHelper
    {
        public static string GetName(this MemberExpression expression)
        {
            return new ExpressionNameVisitor().Visit(expression);
        }

        public static MemberExpression GetMemberExpression<T, TValue>(this Expression<Func<T, TValue>> expression)
        {
            if (expression == null)
            {
                return null;
            }
            if (expression.Body is MemberExpression)
            {
                return (MemberExpression)expression.Body;
            }
            if (expression.Body is UnaryExpression)
            {
                var operand = ((UnaryExpression)expression.Body).Operand;
                if (operand is MemberExpression)
                {
                    return (MemberExpression)operand;
                }
                if (operand is MethodCallExpression)
                {
                    return ((MethodCallExpression)operand).Object as MemberExpression;
                }
            }
            return null;
        }

        private class ExpressionNameVisitor
        {
            public string Visit(Expression expression)
            {
                if (expression is UnaryExpression)
                {
                    expression = ((UnaryExpression)expression).Operand;
                }
                if (expression is MethodCallExpression)
                {
                    return Visit((MethodCallExpression)expression);
                }
                if (expression is MemberExpression)
                {
                    return Visit((MemberExpression)expression);
                }
                if (expression is BinaryExpression && expression.NodeType == ExpressionType.ArrayIndex)
                {
                    return Visit((BinaryExpression)expression);
                }
                return null;
            }

            private string Visit(BinaryExpression expression)
            {
                string result = null;
                if (expression.Left is MemberExpression)
                {
                    result = Visit((MemberExpression)expression.Left);
                }
                var index = Expression.Lambda(expression.Right).Compile().DynamicInvoke();
                return result + string.Format("[{0}]", index);
            }

            private string Visit(MemberExpression expression)
            {
                var name = expression.Member.Name;
                var ancestorName = Visit(expression.Expression);
                if (ancestorName != null)
                {
                    name = ancestorName + "." + name;
                }
                return name;
            }

            private string Visit(MethodCallExpression expression)
            {
                string name = null;
                if (expression.Object is MemberExpression)
                {
                    name = Visit((MemberExpression)expression.Object);
                }

                //TODO: Is there a more certain way to determine if this is an indexed property?
                if (expression.Method.Name == "get_Item" && expression.Arguments.Count == 1)
                {
                    var index = Expression.Lambda(expression.Arguments[0]).Compile().DynamicInvoke();
                    name += string.Format("[{0}]", index);
                }
                return name;
            }
        }
    }
}

namespace Metsys.Bson.Configuration
{

    internal class BsonConfiguration
    {
        private readonly IDictionary<Type, IDictionary<string, string>> _aliasMap = new Dictionary<Type, IDictionary<string, string>>();
        private readonly IDictionary<Type, HashSet<string>> _ignored = new Dictionary<Type, HashSet<string>>();
        private readonly IDictionary<Type, HashSet<string>> _ignoredIfNull = new Dictionary<Type, HashSet<string>>();

        //not thread safe
        private static BsonConfiguration _instance;
        internal static BsonConfiguration Instance
        {
            get
            {
                if (_instance == null) { _instance = new BsonConfiguration(); }
                return _instance;
            }
        }

        private BsonConfiguration() { }

        public static void ForType<T>(Action<ITypeConfiguration<T>> action)
        {
            action(new TypeConfiguration<T>(Instance));
        }

        internal void AddMap<T>(string property, string alias)
        {
            var type = typeof(T);
            if (!_aliasMap.ContainsKey(type))
            {
                _aliasMap[type] = new Dictionary<string, string>();
            }
            _aliasMap[type][property] = alias;
        }
        internal string AliasFor(Type type, string property)
        {
            IDictionary<string, string> map;
            if (!_aliasMap.TryGetValue(type, out map))
            {
                return property;
            }
            return map.ContainsKey(property) ? map[property] : property;
        }

        public void AddIgnore<T>(string name)
        {
            var type = typeof(T);
            if (!_ignored.ContainsKey(type))
            {
                _ignored[type] = new HashSet<string>();
            }
            _ignored[type].Add(name);
        }
        public bool IsIgnored(Type type, string name)
        {
            HashSet<string> list;
            return _ignored.TryGetValue(type, out list) && list.Contains(name);
        }

        public void AddIgnoreIfNull<T>(string name)
        {
            var type = typeof(T);
            if (!_ignoredIfNull.ContainsKey(type))
            {
                _ignoredIfNull[type] = new HashSet<string>();
            }
            _ignoredIfNull[type].Add(name);
        }
        public bool IsIgnoredIfNull(Type type, string name)
        {
            HashSet<string> list;
            return _ignoredIfNull.TryGetValue(type, out list) && list.Contains(name);
        }
    }
}
namespace Metsys.Bson
{

    internal class BsonException : Exception
    {
        public BsonException() { }
        public BsonException(string message) : base(message) { }
        public BsonException(string message, Exception innerException) : base(message, innerException) { }
        protected BsonException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
