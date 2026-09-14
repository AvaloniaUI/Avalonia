using Avalonia.Input.TextInput;
using NWayland.Protocols.TextInputUnstableV3;

namespace Avalonia.Wayland;

/// <summary>
/// Maps Avalonia <see cref="TextInputOptions"/> onto the
/// <c>zwp_text_input_v3</c> <c>(content_hint, content_purpose)</c> pair.
/// </summary>
internal static class TextInputOptionsConverter
{
    public static (ZwpTextInputV3.ContentHintEnum hint, ZwpTextInputV3.ContentPurposeEnum purpose)
        Convert(TextInputOptions options)
    {
        var hint = ZwpTextInputV3.ContentHintEnum.None;
        var purpose = ZwpTextInputV3.ContentPurposeEnum.Normal;

        switch (options.ContentType)
        {
            case TextInputContentType.Normal:
                break;
            case TextInputContentType.Alpha:
                hint |= ZwpTextInputV3.ContentHintEnum.Latin;
                break;
            case TextInputContentType.Digits:
                purpose = ZwpTextInputV3.ContentPurposeEnum.Digits;
                break;
            case TextInputContentType.Pin:
                purpose = ZwpTextInputV3.ContentPurposeEnum.Pin;
                break;
            case TextInputContentType.Number:
                purpose = ZwpTextInputV3.ContentPurposeEnum.Number;
                break;
            case TextInputContentType.Email:
                purpose = ZwpTextInputV3.ContentPurposeEnum.Email;
                break;
            case TextInputContentType.Url:
                purpose = ZwpTextInputV3.ContentPurposeEnum.Url;
                break;
            case TextInputContentType.Name:
                purpose = ZwpTextInputV3.ContentPurposeEnum.Name;
                break;
            case TextInputContentType.Password:
                purpose = ZwpTextInputV3.ContentPurposeEnum.Password;
                hint |= ZwpTextInputV3.ContentHintEnum.HiddenText
                      | ZwpTextInputV3.ContentHintEnum.SensitiveData;
                break;
            case TextInputContentType.Social:
            case TextInputContentType.Search:
                break;
        }

        if (options.Multiline)
            hint |= ZwpTextInputV3.ContentHintEnum.Multiline;
        if (options.IsSensitive)
            hint |= ZwpTextInputV3.ContentHintEnum.SensitiveData;
        if (options.Lowercase)
            hint |= ZwpTextInputV3.ContentHintEnum.Lowercase;
        if (options.Uppercase)
            hint |= ZwpTextInputV3.ContentHintEnum.Uppercase;
        if (options.AutoCapitalization)
            hint |= ZwpTextInputV3.ContentHintEnum.AutoCapitalization;

        if (options.ShowSuggestions == true)
            hint |= ZwpTextInputV3.ContentHintEnum.Completion
                  | ZwpTextInputV3.ContentHintEnum.Spellcheck;

        // Password always overrides: even with ShowSuggestions=true, never reveal text.
        if (options.ContentType == TextInputContentType.Password)
        {
            hint &= ~(ZwpTextInputV3.ContentHintEnum.Completion
                    | ZwpTextInputV3.ContentHintEnum.Spellcheck);
            hint |= ZwpTextInputV3.ContentHintEnum.HiddenText
                  | ZwpTextInputV3.ContentHintEnum.SensitiveData;
        }

        return (hint, purpose);
    }
}
