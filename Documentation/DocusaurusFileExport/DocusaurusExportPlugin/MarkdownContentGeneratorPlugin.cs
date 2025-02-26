using System.Xml.Linq;
using SandcastleBuilder.Utils.BuildComponent;
using SandcastleBuilder.Utils.BuildEngine;
using ExecutionContext = SandcastleBuilder.Utils.BuildComponent.ExecutionContext;

namespace DocusaurusExportPlugin;

[HelpFileBuilderPlugInExport("DocusaurusExportPlugin", Version = "1.0", Description = "DocusaurusExportPlugin") ]
public class MarkdownContentGeneratorPlugin : IPlugIn
{
    private BuildProcess? _buildProcess;
    
    public MarkdownContentGeneratorPlugin()
    {
        ExecutionPoints = new[] { new ExecutionPoint(BuildStep.CompilingHelpFile, ExecutionBehaviors.InsteadOf) };
    }
    
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void Initialize(BuildProcess buildProcess, XElement configuration)
    {
        _buildProcess = buildProcess;
    }

    public void Execute(ExecutionContext context)
    {
        if (context.BuildStep == BuildStep.CompilingHelpFile)
        {
            new MarkdownContentGenerator(_buildProcess!).Execute();
        }
    }

    public IEnumerable<ExecutionPoint> ExecutionPoints { get; }
}
