using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Xunit;

namespace Avalonia.Build.Tasks.UnitTest;

/// <summary>
/// This is fake BuildEngine using for testing build task
/// at moment it manage only <see cref="BuildErrorEventArgs"/> and <see cref="BuildWarningEventArgs"/>
/// other messages are ignored/>
/// </summary>
internal class UnitTestBuildEngine : IBuildEngine, IDisposable
{
    private readonly bool _treatWarningAsError;
    private readonly bool _assertOnDispose;
    private readonly List<UnitTestBuildEngineMessage> _errors = new();

    /// <summary>
    /// Start new instance of <see cref="UnitTestBuildEngine"/>
    /// </summary>
    /// <param name="continueOnError">if it is <c>false</c> immediately assert error</param>
    /// <param name="treatWarningAsError">if it is <c>true</c> treat warning as error</param>
    /// <param name="assertOnDispose">if it is <c>true</c> assert on dispose if there are any errors.</param>
    /// <returns></returns>
    public static UnitTestBuildEngine Start(bool continueOnError = false,
        bool treatWarningAsError = false,
        bool assertOnDispose = false) =>
        new UnitTestBuildEngine(continueOnError, treatWarningAsError, assertOnDispose);

    private UnitTestBuildEngine(bool continueOnError,
        bool treatWarningAsError,
        bool assertOnDispose)
    {
        ContinueOnError = continueOnError;
        _treatWarningAsError = treatWarningAsError;
        _assertOnDispose = assertOnDispose;
    }

    public bool ContinueOnError { get; }

    public int LineNumberOfTaskNode { get; }

    public int ColumnNumberOfTaskNode { get; }

    public string ProjectFileOfTaskNode { get; }

    public IReadOnlyList<UnitTestBuildEngineMessage> Errors => _errors;

    public bool BuildProjectFile(string projectFileName,
        string[] targetNames,
        IDictionary globalProperties,
        IDictionary targetOutputs)
        => throw new NotImplementedException();

    public void Dispose()
    {
        if (_assertOnDispose && _errors.Count > 0)
        {
            Assert.Fail("There is one o more errors.");
        }
    }


    public void LogCustomEvent(CustomBuildEventArgs e)
    {
    }

    public void LogMessageEvent(BuildMessageEventArgs e)
    {
    }

    public void LogErrorEvent(BuildErrorEventArgs e)
    {
        var message = UnitTestBuildEngineMessage.From(e);
        _errors.Add(message);
        if (!ContinueOnError)
        {
            Assert.Fail(message.Message);
        }
    }

    public void LogWarningEvent(BuildWarningEventArgs e)
    {
        if (_treatWarningAsError)
        {
            var message = UnitTestBuildEngineMessage.From(e);
            _errors.Add(message);
            if (!ContinueOnError)
            {
                Assert.Fail(message.Message);
            }
        }
    }
}
