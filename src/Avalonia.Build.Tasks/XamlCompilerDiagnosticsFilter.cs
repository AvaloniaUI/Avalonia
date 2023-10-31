using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using XamlX;

namespace Avalonia.Build.Tasks;

public class XamlCompilerDiagnosticsFilter
{
    private static readonly Regex s_editorConfigRegex =
        new("""avalonia_xaml_diagnostic\.([\w\d]+)\.severity\s*=\s*(\w*)""");

    private readonly bool _treatWarningsAsErrors;
    private readonly HashSet<string> _warningsAsErrors;
    private readonly HashSet<string> _warningsNotAsErrors;
    private readonly HashSet<string> _noWarn;
    private readonly Lazy<Dictionary<string, string>> _lazyEditorConfig;

    public XamlCompilerDiagnosticsFilter(
        bool treatWarningsAsErrors,
        string? warningsAsErrors,
        string? warningsNotAsErrors,
        string? noWarn,
        ITaskItem[]? analyzerConfigFiles)
    {
        _treatWarningsAsErrors = treatWarningsAsErrors;
        var msbuildSeparators = new[] { ',', ';' };
        _warningsAsErrors = new HashSet<string>(warningsAsErrors?.Split(msbuildSeparators, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>());
        _warningsNotAsErrors = new HashSet<string>(warningsNotAsErrors?.Split(msbuildSeparators, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>());
        _noWarn = new HashSet<string>(noWarn?.Split(msbuildSeparators, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>());
        _lazyEditorConfig = new Lazy<Dictionary<string, string>>(() => ParseEditorConfigFiles(analyzerConfigFiles));
    }

    internal XamlDiagnosticSeverity Handle(XamlDiagnostic diagnostic)
    {
        var currentSeverity = diagnostic.Severity;
        
        if (_lazyEditorConfig.Value.TryGetValue(diagnostic.Code, out var severity))
        {
            currentSeverity =  severity.ToLowerInvariant() switch
            {
                "default" => diagnostic.Severity,
                "error" => XamlDiagnosticSeverity.Error,
                "warning" => XamlDiagnosticSeverity.Warning,
                _ => XamlDiagnosticSeverity.None // "suggestion", "silent", "none"
            };
        }
    
        var treatAsError = currentSeverity == XamlDiagnosticSeverity.Warning
                           && !_noWarn.Contains(diagnostic.Code)
                           && (_warningsAsErrors.Contains(diagnostic.Code) || _treatWarningsAsErrors)
                           && !_warningsNotAsErrors.Contains(diagnostic.Code);

        return treatAsError ? XamlDiagnosticSeverity.Error : currentSeverity;
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
