using System;
using System.Collections.Generic;

namespace Avalonia.Media.Fonts.Tables
{
    /// <summary>
    /// Reads the script tags declared in a font's OpenType Layout (<c>GSUB</c>/<c>GPOS</c>)
    /// ScriptList. A declared script tag signals that the font carries shaping rules for that
    /// script — the signal used to decide whether a font can actually <em>shape</em> a complex
    /// script rather than merely map its codepoints through cmap.
    /// <see href="https://learn.microsoft.com/typography/opentype/spec/chapter2#script-list-table-and-script-record"/>
    /// </summary>
    internal static class ScriptListTable
    {
        private static readonly OpenTypeTag s_gsub = OpenTypeTag.Parse("GSUB");
        private static readonly OpenTypeTag s_gpos = OpenTypeTag.Parse("GPOS");

        /// <summary>
        /// Adds the GSUB and GPOS script tags declared by <paramref name="glyphTypeface"/> to
        /// <paramref name="scriptTags"/>.
        /// </summary>
        /// <returns>
        /// <c>false</c> if a layout table is present but could not be parsed — the caller should
        /// then treat shaping capability as unknown rather than unsupported (don't reject the font
        /// on the strength of an empty set). An absent table is not a failure.
        /// </returns>
        public static bool TryReadScriptTags(GlyphTypeface glyphTypeface, HashSet<OpenTypeTag> scriptTags)
        {
            // Non-short-circuiting '&' so both tables are always attempted.
            return TryReadScriptList(glyphTypeface, s_gsub, scriptTags)
                 & TryReadScriptList(glyphTypeface, s_gpos, scriptTags);
        }

        private static bool TryReadScriptList(GlyphTypeface glyphTypeface, OpenTypeTag tableTag,
            HashSet<OpenTypeTag> scriptTags)
        {
            if (!glyphTypeface.PlatformTypeface.TryGetTable(tableTag, out var table))
            {
                // An absent table simply contributes no scripts
                return true;
            }

            try
            {
                var reader = new BigEndianBinaryReader(table.Span);

                // GSUB/GPOS header: majorVersion, minorVersion, scriptListOffset, featureListOffset,
                // lookupListOffset.
                reader.ReadUInt16();
                reader.ReadUInt16();
                var scriptListOffset = reader.ReadOffset16();

                // ScriptList: scriptCount, then scriptCount ScriptRecords of (Tag, Offset16).
                reader.Seek(scriptListOffset);

                var scriptCount = reader.ReadUInt16();

                for (var i = 0; i < scriptCount; i++)
                {
                    scriptTags.Add(new OpenTypeTag(reader.ReadUInt32()));
                    reader.ReadOffset16(); // scriptOffset — not needed
                }

                return true;
            }
            catch (Exception)
            {
                // Malformed layout table — capability unknown.
                return false;
            }
        }
    }
}
