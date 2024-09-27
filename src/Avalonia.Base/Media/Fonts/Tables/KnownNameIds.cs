// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

namespace Avalonia.Media.Fonts.Tables
{
    /// <summary>
    /// Provides enumeration of common name ids
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/name#name-ids"/>
    /// </summary>
    internal enum KnownNameIds : ushort
    {
        /// <summary>
        /// The copyright notice
        /// </summary>
        CopyrightNotice = 0,

        /// <summary>
        /// The font family name; Up to four fonts can share the Font Family name, forming a font style linking
        /// group (regular, italic, bold, bold italic — as defined by OS/2.fsSelection bit settings).
        /// </summary>
        FontFamilyName = 1,

        /// <summary>
        /// The font subfamily name; The Font Subfamily name distinguishes the font in a group with the same Font Family name (name ID 1).
        /// This is assumed to address style (italic, oblique) and weight (light, bold, black, etc.). A font with no particular differences
        /// in weight or style (e.g. medium weight, not italic and fsSelection bit 6 set) should have the string "Regular" stored in this position.
        /// </summary>
        FontSubfamilyName = 2,

        /// <summary>
        /// The unique font identifier
        /// </summary>
        UniqueFontID = 3,

        /// <summary>
        /// The full font name; a combination of strings 1 and 2, or a similar human-readable variant. If string 2 is "Regular", it is sometimes omitted from name ID 4.
        /// </summary>
        FullFontName = 4,

        /// <summary>
        /// Version string. Should begin with the syntax 'Version &lt;number&gt;.&lt;number>' (upper case, lower case, or mixed, with a space between "Version" and the number).
        /// The string must contain a version number of the following form: one or more digits (0-9) of value less than 65,535, followed by a period, followed by one or more
        /// digits of value less than 65,535. Any character other than a digit will terminate the minor number. A character such as ";" is helpful to separate different pieces of version information.
        /// The first such match in the string can be used by installation software to compare font versions.
        /// Note that some installers may require the string to start with "Version ", followed by a version number as above.
        /// </summary>
        Version = 5,

        /// <summary>
        /// Postscript name for the font; Name ID 6 specifies a string which is used to invoke a PostScript language font that corresponds to this OpenType font.
        /// When translated to ASCII, the name string must be no longer than 63 characters and restricted to the printable ASCII subset, codes 33 to 126,
        /// except for the 10 characters '[', ']', '(', ')', '{', '}', '&lt;', '&gt;', '/', '%'.
        /// In a CFF OpenType font, there is no requirement that this name be the same as the font name in the CFF’s Name INDEX.
        /// Thus, the same CFF may be shared among multiple font components in a Font Collection. See the 'name' table section of
        /// Recommendations for OpenType fonts "" for additional information.
        /// </summary>
        PostscriptName = 6,

        /// <summary>
        /// Trademark; this is used to save any trademark notice/information for this font. Such information should
        /// be based on legal advice. This is distinctly separate from the copyright.
        /// </summary>
        Trademark = 7,

        /// <summary>
        /// The manufacturer
        /// </summary>
        Manufacturer = 8,

        /// <summary>
        /// Designer; name of the designer of the typeface.
        /// </summary>
        Designer = 9,

        /// <summary>
        /// Description; description of the typeface. Can contain revision information, usage recommendations, history, features, etc.
        /// </summary>
        Description = 10,

        /// <summary>
        /// URL Vendor; URL of font vendor (with protocol, e.g., http://, ftp://). If a unique serial number is embedded in
        /// the URL, it can be used to register the font.
        /// </summary>
        VendorUrl = 11,

        /// <summary>
        /// URL Designer; URL of typeface designer (with protocol, e.g., http://, ftp://).
        /// </summary>
        DesignerUrl = 12,

        /// <summary>
        /// License Description; description of how the font may be legally used, or different example scenarios for licensed use.
        /// This field should be written in plain language, not legalese.
        /// </summary>
        LicenseDescription = 13,

        /// <summary>
        /// License Info URL; URL where additional licensing information can be found.
        /// </summary>
        LicenseInfoUrl = 14,

        /// <summary>
        /// Typographic Family name: The typographic family grouping doesn't impose any constraints on the number of faces within it,
        /// in contrast with the 4-style family grouping (ID 1), which is present both for historical reasons and to express style linking groups.
        /// If name ID 16 is absent, then name ID 1 is considered to be the typographic family name.
        /// (In earlier versions of the specification, name ID 16 was known as "Preferred Family".)
        /// </summary>
        TypographicFamilyName = 16,

        /// <summary>
        /// Typographic Subfamily name: This allows font designers to specify a subfamily name within the typographic family grouping.
        /// This string must be unique within a particular typographic family. If it is absent, then name ID 2 is considered to be the
        /// typographic subfamily name. (In earlier versions of the specification, name ID 17 was known as "Preferred Subfamily".)
        /// </summary>
        TypographicSubfamilyName = 17,

        /// <summary>
        /// Sample text; This can be the font name, or any other text that the designer thinks is the best sample to display the font in.
        /// </summary>
        SampleText = 19,
    }
}
