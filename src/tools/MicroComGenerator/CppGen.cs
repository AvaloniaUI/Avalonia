using System;
using System.Linq;
using System.Text;
using MicroComGenerator.Ast;

namespace MicroComGenerator
{
    public class CppGen
    {
        static string ConvertType(AstTypeNode type)
        {
            var name = type.Name;
            if (name == "byte")
                name = "unsigned char";
            else if(name == "uint")
                name = "unsigned int";

            type = type.Clone();
            type.Name = name;
            return type.Format();
        }
        
        public static string GenerateCpp(AstIdlNode idl)
        {
            var sb = new StringBuilder();
            var preamble = idl.GetAttributeOrDefault("cpp-preamble");
            if (preamble != null)
                sb.AppendLine(preamble);

            foreach (var s in idl.Structs)
                sb.AppendLine("struct " + s.Name + ";");
            
            foreach (var s in idl.Interfaces)
                sb.AppendLine("struct " + s.Name + ";");

            foreach (var en in idl.Enums)
            {
                sb.Append("enum ");
                if (en.Attributes.Any(a => a.Name == "class-enum"))
                    sb.Append("class ");
                sb.AppendLine(en.Name).AppendLine("{");

                foreach (var m in en)
                {
                    sb.Append("    ").Append(m.Name);
                    if (m.Value != null)
                        sb.Append(" = ").Append(m.Value);
                    sb.AppendLine(",");
                }

                sb.AppendLine("};");
            }

            foreach (var s in idl.Structs)
            {
                sb.Append("struct ").AppendLine(s.Name).AppendLine("{");
                foreach (var m in s) 
                    sb.Append("    ").Append(ConvertType(m.Type)).Append(" ").Append(m.Name).AppendLine(";");

                sb.AppendLine("};");
            }

            foreach (var i in idl.Interfaces)
            {
                var guidString = i.GetAttribute("uuid");
                var guid = Guid.Parse(guidString).ToString().Replace("-", "");


                sb.Append("COMINTERFACE(").Append(i.Name).Append(", ")
                    .Append(guid.Substring(0, 8)).Append(", ")
                    .Append(guid.Substring(8, 4)).Append(", ")
                    .Append(guid.Substring(12, 4));
                for (var c = 0; c < 8; c++)
                {
                    sb.Append(", ").Append(guid.Substring(16 + c * 2, 2));
                }

                sb.Append(") : ");
                if (i.HasAttribute("cpp-virtual-inherits"))
                    sb.Append("virtual ");
                sb.AppendLine(i.Inherits ?? "IUnknown")
                    .AppendLine("{");

                foreach (var m in i)
                {
                    sb.Append("    ")
                        .Append("virtual ")
                        .Append(ConvertType(m.ReturnType))
                        .Append(" ").Append(m.Name).Append(" (");
                    if (m.Count == 0)
                        sb.AppendLine(") = 0;");
                    else
                    {
                        sb.AppendLine();
                        for (var c = 0; c < m.Count; c++)
                        {
                            var arg = m[c];
                            sb.Append("        ");
                            if (arg.Attributes.Any(a => a.Name == "const"))
                                sb.Append("const ");
                            sb.Append(ConvertType(arg.Type))
                                .Append(" ")
                                .Append(arg.Name);
                            if (c != m.Count - 1)
                                sb.Append(", ");
                            sb.AppendLine();
                        }

                        sb.AppendLine("    ) = 0;");
                    }
                }

                sb.AppendLine("};");
            }
            
            return sb.ToString();
        }
    }
}
