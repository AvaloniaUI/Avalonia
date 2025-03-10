using System.Text;

namespace DocusaurusExportPlugin.Sidebar;

public class SidebarLink : SidebarSection
{
    public string Path { get; set; }
    
    public override string ToJson(int indentLevel)
    {
        var indent = GetIndentation(indentLevel);
        
        var sb = new StringBuilder();
        
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}  'type': 'doc',");
        sb.AppendLine($"{indent}  'label': '{Label}',");
        sb.AppendLine($"{indent}  'id': '{Path}',");
        sb.AppendLine($"{indent}}}");
        
        return sb.ToString();
    }
}