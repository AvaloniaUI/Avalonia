namespace Avalonia.Input.TextInput;

// Input that is never spell checked or autocorrected, by the framework or by native keyboards.
internal static class SpellCheckPolicy
{
    public static bool IsAllowed(TextInputContentType contentType, bool isSensitive, bool hasPasswordChar = false)
    {
        return !isSensitive &&
            !hasPasswordChar &&
            contentType is not (
                TextInputContentType.Digits or
                TextInputContentType.Number or
                TextInputContentType.Password or
                TextInputContentType.Pin or
                TextInputContentType.Url or
                TextInputContentType.Email);
    }

    public static bool IsAllowed(TextInputOptions options) => IsAllowed(options.ContentType, options.IsSensitive);
}
