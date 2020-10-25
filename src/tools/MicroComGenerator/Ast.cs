using System.Collections.Generic;

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
    }

    public class AstEnumNode : List<AstEnumMemberNode>
    {
        public List<AstAttributeNode> Attributes { get; set; } = new List<AstAttributeNode>();
        public string Name { get; set; }
        public override string ToString() => Name;
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
        
        public override string ToString() => $"{Name} = {Value}";
    }

    public class AstStructNode : List<AstStructMemberNode>
    {
        public List<AstAttributeNode> Attributes { get; set; } = new List<AstAttributeNode>();
        public string Name { get; set; }
        public override string ToString() => Name;
    }

    public class AstTypeNode
    {
        public string Name { get; set; }
        public int PointerLevel { get; set; }

        public override string ToString() => Name + new string('*', PointerLevel);
    }

    public class AstStructMemberNode
    {
        public string Name { get; set; }
        public AstTypeNode Type { get; set; }

        public override string ToString() => $"{Type} {Name}";
    }

    public class AstInterfaceNode : List<AstInterfaceMemberNode>
    {
        public List<AstAttributeNode> Attributes { get; set; } = new List<AstAttributeNode>();
        public string Name { get; set; }
        public string Inherits { get; set; }
        
        public override string ToString()
        {
            if (Inherits == null)
                return Name;
            return $"{Name} : {Inherits}";
        }
    }

    public class AstInterfaceMemberNode : List<AstInterfaceMemberArgumentNode>
    {
        public string Name { get; set; }
        public AstTypeNode ReturnType { get; set; }
        public List<AstAttributeNode> Attributes { get; set; } = new List<AstAttributeNode>();

        public override string ToString() => $"{ReturnType} {Name} ({string.Join(", ", this)})";
    }

    public class AstInterfaceMemberArgumentNode
    {
        public string Name { get; set; }
        public AstTypeNode Type { get; set; }
        public List<AstAttributeNode> Attributes { get; set; } = new List<AstAttributeNode>();

        public override string ToString() => $"{Type} {Name}";
    }

    public class AstIdlNode
    {
        public List<AstAttributeNode> Attributes { get; set; } = new List<AstAttributeNode>();
        public List<AstEnumNode> Enums { get; set; } = new List<AstEnumNode>();
        public List<AstStructNode> Structs { get; set; } = new List<AstStructNode>();
        public List<AstInterfaceNode> Interfaces { get; set; } = new List<AstInterfaceNode>();
    }
}
