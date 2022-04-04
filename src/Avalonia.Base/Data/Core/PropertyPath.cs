using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Data.Core
{
    public class PropertyPath
    {
        public IReadOnlyList<IPropertyPathElement> Elements { get; }

        public PropertyPath(IEnumerable<IPropertyPathElement> elements)
        {
            Elements = elements.ToArray();
        }
    }

    public class PropertyPathBuilder
    {
        readonly List<IPropertyPathElement> _elements = new List<IPropertyPathElement>();
        
        public PropertyPathBuilder Property(IPropertyInfo property)
        {
            _elements.Add(new PropertyPropertyPathElement(property));
            return this;
        }
        

        public PropertyPathBuilder ChildTraversal()
        {
            _elements.Add(new ChildTraversalPropertyPathElement());
            return this;
        }

        public PropertyPathBuilder EnsureType(Type type)
        {
            _elements.Add(new EnsureTypePropertyPathElement(type));
            return this;
        }

        public PropertyPathBuilder Cast(Type type)
        {
            _elements.Add(new CastTypePropertyPathElement(type));
            return this;
        }

        public PropertyPath Build()
        {
            return new PropertyPath(_elements);
        }
    }

    public interface IPropertyPathElement
    {
        
    }

    public class PropertyPropertyPathElement : IPropertyPathElement
    {
        public IPropertyInfo Property { get; }

        public PropertyPropertyPathElement(IPropertyInfo property)
        {
            Property = property;
        }
    }

    public class ChildTraversalPropertyPathElement : IPropertyPathElement
    {
        
    }

    public class EnsureTypePropertyPathElement : IPropertyPathElement
    {
        public Type Type { get; }

        public EnsureTypePropertyPathElement(Type type)
        {
            Type = type;
        }
    }

    public class CastTypePropertyPathElement : IPropertyPathElement
    {
        public CastTypePropertyPathElement(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
    }
}
