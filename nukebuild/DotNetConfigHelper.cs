using System.Globalization;
using Nuke.Common.Tools.DotNet;

public class DotNetConfigHelper
{
    public DotNetBuildSettings Build;
    public DotNetPackSettings Pack;
    public DotNetTestSettings Test;

    public DotNetConfigHelper(DotNetBuildSettings s)
    {
        Build = s;
    }

    public DotNetConfigHelper(DotNetPackSettings s)
    {
        Pack = s;
    }

    public DotNetConfigHelper(DotNetTestSettings s)
    {
        Test = s;
    }

    public DotNetConfigHelper AddProperty(string key, bool value) =>
        AddProperty(key, value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
    public DotNetConfigHelper AddProperty(string key, string value)
    {
        Build = Build?.AddProperty(key, value);
        Pack = Pack?.AddProperty(key, value);
        Test = Test?.AddProperty(key, value);

        return this;
    }

    public DotNetConfigHelper SetConfiguration(string configuration)
    {
        Build = Build?.SetConfiguration(configuration);
        Pack = Pack?.SetConfiguration(configuration);
        Test = Test?.SetConfiguration(configuration);
        return this;
    }

    public DotNetConfigHelper SetVerbosity(DotNetVerbosity verbosity)
    {
        Build = Build?.SetVerbosity(verbosity);
        Pack = Pack?.SetVerbosity(verbosity);
        Test = Test?.SetVerbosity(verbosity);
        return this;
    }

    public static implicit operator DotNetConfigHelper(DotNetBuildSettings s) => new DotNetConfigHelper(s);
    public static implicit operator DotNetConfigHelper(DotNetPackSettings s) => new DotNetConfigHelper(s);
    public static implicit operator DotNetConfigHelper(DotNetTestSettings s) => new DotNetConfigHelper(s);
}
