namespace Avalonia.Input.TextInput
{   
    public enum TextInputContentType
    {
        /// <summary>
        /// Default keyboard for the users configured input method.
        /// </summary>
        Normal = 0,
        
        /// <summary>
        /// Display a keyboard that only has alphabetic characters.
        /// </summary>
        Alpha,
        
        /// <summary>
        /// Display a numeric keypad only capable of numbers. i.e. Phone number
        /// </summary>
        Digits,
        
        /// <summary>
        /// Display a numeric keypad for inputting a PIN.
        /// </summary>
        Pin,
        
        /// <summary>
        /// Display a numeric keypad capable of inputting numbers including decimal seperator and sign.
        /// </summary>
        Number,
        
        /// <summary>
        /// Display a keyboard for entering an email address.
        /// </summary>
        Email,
        
        /// <summary>
        /// Display a keyboard for entering a URL.
        /// </summary>
        Url,
        
        /// <summary>
        /// Display a keyboard for entering a persons name.
        /// </summary>
        Name,
        
        /// <summary>
        /// Display a keyboard for entering sensitive data.
        /// </summary>
        Password,
        
        /// <summary>
        /// Display a keyboard suitable for #tag and @mentions.
        /// Not available on all platforms, will fallback to a suitable keyboard
        /// when not available.
        /// </summary>
        Social,
        
        /// <summary>
        /// Display a keyboard for entering a search keyword.
        /// </summary>
        Search
    }
}
