#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework.XamlTypes;

namespace DocusaurusExportPlugin.Sidebar;

public class SidebarGenerator
{
    public List<SidebarSection> Items { get; } = new List<SidebarSection>();
    private List<SidebarSection> _AllItems { get; } = new List<SidebarSection>();

    private SidebarCategory? GetParentCategory(int currentLevel)
    {
        if (currentLevel <= 1 || _AllItems.Count == 0) return null;

        for (int i = _AllItems.Count - 1; i >= 0; i--)
        {
            var item = _AllItems[i];
            
            if (item is SidebarCategory category && category.Level < currentLevel) return category;
        }
        
        return null;
    }
    
    public SidebarSection? AddItem(string id, string path, string title, int level)
    {
        var type = id.Split(':').FirstOrDefault();

        Console.WriteLine($"Adding item {id} ({level}, {type}) - {title}");
        
        SidebarSection section;
        
        var parent = GetParentCategory(level);
        
        switch (type)
        {
            case "G":
                section = new SidebarCategory
                {
                    Level = level,
                    Label = title,
                    Path = path,
                    Collapsed = false
                };
                break;
            
            case "N":
                section = new SidebarCategory
                {
                    Level = level,
                    Label = title,
                    Path = path,
                    Collapsed = true
                };
                
                break;
            
            case "T":
                section = new SidebarLink()
                {
                    Level = level,
                    Label = title,
                    Path = path,
                };
                break;
            
            default:
                return null;
        }
        
        if (parent != null)
        {
            parent.Items.Add(section);
            
            Trace.WriteLine($"Added sidebar section {section.Label} to {parent.Label}");
        }
        else
        {
            Items.Add(section);
            Trace.WriteLine($"Added sidebar category {section.Label} to Items");
        }
        _AllItems.Add(section);
                
        return section;
    }
    
    public void GenerateSidebarsJs(string sidebarFilePath)
    {
        var sb = new StringBuilder();
        
        // Add TypeScript comments
        sb.AppendLine("// @ts-check");
        sb.AppendLine();
        sb.AppendLine("/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */");
        sb.AppendLine("const sidebars = {");
        sb.AppendLine();
        sb.AppendLine("  documentationSidebar: [");

        var lastSection = Items.Last();
        foreach (var section in Items)
        {
            sb.Append(section.ToJson(4));
            if (section != lastSection)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }

        sb.AppendLine("  ],");
        sb.AppendLine("};");
        sb.AppendLine();
        sb.AppendLine("module.exports = sidebars;");

        File.WriteAllText(sidebarFilePath, sb.ToString());
    }
}