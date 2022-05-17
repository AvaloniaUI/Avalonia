using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Moq;

namespace Avalonia.Markup.UnitTests.Data;

class DynamicReflectableType : IReflectableType, INotifyPropertyChanged, IEnumerable<KeyValuePair<string, object>>
{
    private Dictionary<string, object> _dic = new();

    public TypeInfo GetTypeInfo()
    {
        return new FakeTypeInfo();
    }

    public void Add(string key, object value)
    {
        _dic.Add(key, value);
        
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
    }

    public object this[string key]
    {
        get => _dic[key];
        set
        {
            _dic[key] = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
        }
    }
    
    public event PropertyChangedEventHandler PropertyChanged;
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        return _dic.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_dic).GetEnumerator();
    }


    class FakeTypeInfo : TypeInfo
    {
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types,
            ParameterModifier[] modifiers)
        {
            var propInfo = new Mock<PropertyInfo>();
            propInfo.SetupGet(x => x.Name).Returns(name);
            propInfo.SetupGet(x => x.PropertyType).Returns(typeof(object));
            propInfo.SetupGet(x => x.CanWrite).Returns(true);
            propInfo.Setup(x => x.GetValue(It.IsAny<object>(), It.IsAny<object[]>()))
                .Returns((object target, object [] _) => ((DynamicReflectableType)target)._dic.GetValueOrDefault(name));
            propInfo.Setup(x => x.SetValue(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<object[]>()))
                .Callback((object target, object value, object [] _) =>
                {
                    ((DynamicReflectableType)target)._dic[name] = value;
                });
            return propInfo.Object;
        }

        #region NotSupported

        
        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override Module Module { get; }
        public override string Namespace { get; }
        public override string Name { get; }
        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            throw new NotSupportedException();
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention,
            Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotSupportedException();
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            throw new NotSupportedException();
        }

        public override Type GetElementType()
        {
            throw new NotSupportedException();
        }

        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException();
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            throw new NotSupportedException();
        }

        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException();
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            throw new NotSupportedException();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw new NotSupportedException();
        }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention,
            Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotSupportedException();
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            throw new NotSupportedException();
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            throw new NotSupportedException();
        }

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args,
            ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            throw new NotSupportedException();
        }

        public override Type UnderlyingSystemType { get; }

        protected override bool IsArrayImpl()
        {
            throw new NotSupportedException();
        }

        protected override bool IsByRefImpl()
        {
            throw new NotSupportedException();
        }

        protected override bool IsCOMObjectImpl()
        {
            throw new NotSupportedException();
        }

        protected override bool IsPointerImpl()
        {
            throw new NotSupportedException();
        }

        protected override bool IsPrimitiveImpl()
        {
            throw new NotSupportedException();
        }

        public override Assembly Assembly { get; }
        public override string AssemblyQualifiedName { get; }
        public override Type BaseType { get; }
        public override string FullName { get; }
        public override Guid GUID { get; }

        

        protected override bool HasElementTypeImpl()
        {
            throw new NotSupportedException();
        }

        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            throw new NotSupportedException();
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            throw new NotSupportedException();
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw new NotSupportedException();
        }

        public override Type[] GetInterfaces()
        {
            throw new NotSupportedException();
        }
        

        #endregion

    }
}