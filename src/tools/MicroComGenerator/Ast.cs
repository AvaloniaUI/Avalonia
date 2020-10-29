using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroComGenerator.Ast
{
    public class AstAttributeNode
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public AstAttributeNode(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public override string ToString() => $"{Name} = {Value}";
        public AstAttributeNode Clone() => new AstAttributeNode(Name, Value);
    }

    public class AstAttributes : List<AstAttributeNode>
    {
        public bool HasAttribute(string a) => this.Any(x => x.Name == a);
        
        public AstAttributes Clone()
        {
            var rv= new AstAttributes();
            rv.AddRange(this.Select(x => x.Clone()));
            return rv;
        }
    }

    public interface IAstNodeWithAttributes
    {
        public AstAttributes Attributes { get; set; }
    }
    
    public class AstEnumNode : List<AstEnumMemberNode>, IAstNodeWithAttributes
    {
        public AstAttributes Attributes { get; set; } = new AstAttributes();
        public string Name { get; set; }
        public override string ToString() => "Enum " + Name;

        public AstEnumNode Clone()
        {
            var rv = new AstEnumNode { Name = Name, Attributes = Attributes.Clone() };
            rv.AddRange(this.Select(x => x.Clone()));
            return rv;
        }
    }

    public class AstEnumMemberNode
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public AstEnumMemberNode(string name, string value)
        {
            Name = name;
            Value = value;
        }
        
        public override string ToString() => $"Enum member {Name} = {Value}";
        public AstEnumMemberNode Clone() => new AstEnumMemberNode(Name, Value);
    }

    public class AstStructNode : List<AstStructMemberNode>, IAstNodeWithAttributes
    {
        public AstAttributes Attributes { get; set; } = new AstAttributes();
        public string Name { get; set; }
        public override string ToString() => "Struct " + Name;
        
        public AstStructNode Clone()
        {
            var rv = new AstStructNode { Name = Name, Attributes = Attributes.Clone() };
            rv.AddRange(this.Select(x => x.Clone()));
            return rv;
        }
    }

    public class AstTypeNode
    {
        public string Name { get; set; }
        public int PointerLevel { get; set; }
        public bool IsLink { get; set; }

        public string Format() => Name + new string('*', PointerLevel)
                                       + (IsLink ? "&" : "");
        public override string ToString() => Format();
        public AstTypeNode Clone() => new AstTypeNode() { 
            Name = Name,
            PointerLevel = PointerLevel,
            IsLink = IsLink
        };
    }

    public class AstStructMemberNode : IAstNodeWithAttributes
    {
        public string Name { get; set; }
        public AstTypeNode Type { get; set; }

        public override string ToString() => $"Struct member {Type.Format()} {Name}";
        public AstStructMemberNode Clone() => new AstStructMemberNode() { Name = Name, Type = Type.Clone() };
        public AstAttributes Attributes { get; set; } = new AstAttributes();
    }

    public class AstInterfaceNode : List<AstInterfaceMemberNode>, IAstNodeWithAttributes
    {
        public AstAttributes Attributes { get; set; } = new AstAttributes();
        public string Name { get; set; }
        public string Inherits { get; set; }
        
        public override string ToString()
        {
            if (Inherits == null)
                return Name;
            return $"Interface {Name} : {Inherits}";
        }
        public AstInterfaceNode Clone()
        {
            var rv = new AstInterfaceNode { Name = Name, Inherits = Inherits, Attributes = Attributes.Clone() };
            rv.AddRange(this.Select(x => x.Clone()));
            return rv;
        }
    }

    public class AstInterfaceMemberNode : List<AstInterfaceMemberArgumentNode>, IAstNodeWithAttributes
    {
        public string Name { get; set; }
        public AstTypeNode ReturnType { get; set; }
        public AstAttributes Attributes { get; set; } = new AstAttributes();

        public AstInterfaceMemberNode Clone()
        {
            var rv = new AstInterfaceMemberNode()
            {
                Name = Name, Attributes = Attributes.Clone(), ReturnType = ReturnType
            };
            rv.AddRange(this.Select(x => x.Clone()));
            return rv;
        }

        public override string ToString() =>
            $"Interface member {ReturnType.Format()} {Name} ({string.Join(", ", this.Select(x => x.Format()))})";
    }

    public class AstInterfaceMemberArgumentNode : IAstNodeWithAttributes
    {
        public string Name { get; set; }
        public AstTypeNode Type { get; set; }
        public AstAttributes Attributes { get; set; } = new AstAttributes();

        
        public  string Format() => $"{Type.Format()} {Name}";
        public override string ToString() => "Argument " + Format();

        public AstInterfaceMemberArgumentNode Clone() => new AstInterfaceMemberArgumentNode
        {
            Name = Name, Type = Type.Clone(), Attributes = Attributes.Clone()
        };
    }

    public static class AstExtensions
    {
        public static bool HasAttribute(this IAstNodeWithAttributes node, string s) => node.Attributes.HasAttribute(s);

        public static string GetAttribute(this IAstNodeWithAttributes node, string s)
        {
            var value = node.Attributes.FirstOrDefault(a => a.Name == s)?.Value;
            if (value == null)
                throw new CodeGenException("Expected attribute " + s + " for node " + node);
            return value;
        }
        
        public static string GetAttributeOrDefault(this IAstNodeWithAttributes node, string s) 
            => node.Attributes.FirstOrDefault(a => a.Name == s)?.Value;
    }

    class AstVisitor
    {
        protected virtual void VisitType(AstTypeNode type)
        {
        }
        
        protected virtual void VisitArgument(AstInterfaceMemberArgumentNode argument)
        {
            VisitType(argument.Type);
        }

        protected virtual void VisitInterfaceMember(AstInterfaceMemberNode member)
        {
            foreach(var a in member)
                VisitArgument(a);
            VisitType(member.ReturnType);
        }

        protected virtual void VisitInterface(AstInterfaceNode iface)
        {
            foreach(var m in iface)
                VisitInterfaceMember(m);
        }

        protected virtual void VisitStructMember(AstStructMemberNode member)
        {
            VisitType(member.Type);
        }

        protected virtual void VisitStruct(AstStructNode node)
        {
            foreach(var m in node)
                VisitStructMember(m);
        }
        
        public virtual void VisitAst(AstIdlNode ast)
        {
            foreach(var iface in ast.Interfaces)
                VisitInterface(iface);
            foreach (var s in ast.Structs)
                VisitStruct(s);
        }
        
        
    }

    public class AstIdlNode : IAstNodeWithAttributes
    {
        public AstAttributes Attributes { get; set; } = new AstAttributes();
        public List<AstEnumNode> Enums { get; set; } = new List<AstEnumNode>();
        public List<AstStructNode> Structs { get; set; } = new List<AstStructNode>();
        public List<AstInterfaceNode> Interfaces { get; set; } = new List<AstInterfaceNode>();

        public AstIdlNode Clone() => new AstIdlNode()
        {
            Attributes = Attributes.Clone(),
            Enums = Enums.Select(x => x.Clone()).ToList(),
            Structs = Structs.Select(x => x.Clone()).ToList(),
            Interfaces = Interfaces.Select(x => x.Clone()).ToList()
        };
    }
}
