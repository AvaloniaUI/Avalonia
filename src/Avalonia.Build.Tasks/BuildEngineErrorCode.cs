namespace Avalonia.Build.Tasks
{
    public enum BuildEngineErrorCode
    {
        InvalidXAML = 1,
        DuplicateXClass = 2,
        LegacyResmScheme = 3,
        TransformError = 4,
        EmitError = 4,
        Loader = 5,

        Unknown = 9999
    }
}
