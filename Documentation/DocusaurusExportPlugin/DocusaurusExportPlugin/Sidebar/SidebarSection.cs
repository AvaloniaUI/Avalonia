namespace DocusaurusExportPlugin.Sidebar;

public abstract class SidebarSection
{
    public abstract string ToJson(int indentLevel);
    protected string GetIndentation(int level) => new string(' ', level * 2);
    
    public int Level { get; set; }
    public string Label { get; set; }
}