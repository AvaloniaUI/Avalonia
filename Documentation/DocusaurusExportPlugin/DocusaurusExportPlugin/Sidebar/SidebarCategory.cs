using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DocusaurusExportPlugin.Sidebar;

public class SidebarCategory : SidebarSection
{
    public bool Collapsed { get; set; }
    public List<SidebarSection> Items { get; set; } = new();
    
    public string Path { get; set; }
    
    public override string ToJson(int indentLevel)
    {
        var indent = GetIndentation(indentLevel);
        var sb = new StringBuilder();
        
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}  'type': 'category',");
        sb.AppendLine($"{indent}  'label': '{Label}',");
        
        // Only include collapsed if it's true (to match desired format)
        if (Collapsed)
        {
            sb.AppendLine($"{indent}  'collapsed': {Collapsed.ToString().ToLower()},");
        }

        if (!string.IsNullOrWhiteSpace(Path))
        {
            sb.AppendLine($"{indent}  'link': {{type: 'doc', id: '{Path}'}},");
        }
        
        sb.AppendLine($"{indent}  'items': [");

        var lastItem = Items.Last();
        foreach (var item in Items)
        {
            sb.Append(item.ToJson(indentLevel + 2));
            if (item != lastItem)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }

        sb.Append($"{indent}  ]");
        sb.Append($"\n{indent}}}");

        return sb.ToString();
    }
}