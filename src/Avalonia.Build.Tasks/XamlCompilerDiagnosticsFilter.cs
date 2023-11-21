using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using XamlX;

namespace Avalonia.Build.Tasks;

// With MSBuild, we don't need to read for TreatWarningsAsErrors/WarningsAsErrors/WarningsNotAsErrors/NoWarn properties.
// Just by reporting them with LogWarning MSBuild will do the rest for us.
// But we still need to read EditorConfig manually.
public class XamlCompilerDiagnosticsFilter
{
    private static readonly Regex s_editorConfigRegex =
        new("""avalonia_xaml_diagnostic\.([\w\d]+)\.severity\s*=\s*(\w*)""");

    private readonly Lazy<Dictionary<string, string>> _lazyEditorConfig;

    public XamlCompilerDiagnosticsFilter(
        ITaskItem[]? analyzerConfigFiles)
    {
        _lazyEditorConfig = new Lazy<Dictionary<string, string>>(() => ParseEditorConfigFiles(analyzerConfigFiles));
    }

    internal XamlDiagnosticSeverity Handle(XamlDiagnostic diagnostic)
    {
        return Handle(diagnostic.Severity, diagnostic.Code);
    }

    internal XamlDiagnosticSeverity Handle(XamlDiagnosticSeverity currentSeverity, string diagnosticCode)
    {
        if (_lazyEditorConfig.Value.TryGetValue(diagnosticCode, out var severity))
        {
            return severity.ToLowerInvariant() switch
            {
                "default" => currentSeverity,
                "error" => XamlDiagnosticSeverity.Error,
                "warning" => XamlDiagnosticSeverity.Warning,
                _ => XamlDiagnosticSeverity.None // "suggestion", "silent", "none"
            };
        }

        return currentSeverity;
    }

    private Dictionary<string, string> ParseEditorConfigFiles(ITaskItem[]? analyzerConfigFiles)
    {
        // Very naive EditorConfig parser, supporting minimal properties set via regex:
        var severities = new Dictionary<string, string>();
        if (analyzerConfigFiles is not null)
        {
            foreach (var fileItem in analyzerConfigFiles)
            {
                if (File.Exists(fileItem.ItemSpec))
                {
                    var fileContent = File.ReadAllText(fileItem.ItemSpec);
                    var matches = s_editorConfigRegex.Matches(fileContent);
                    foreach (Match match in matches)
                    {
                        severities[match.Groups[1].Value] = match.Groups[2].Value;
                    }
                }
            }
        }

        return severities;
    }
}
