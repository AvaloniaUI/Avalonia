// RichTextKit
// Copyright © 2019 Topten Software. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may 
// not use this product except in compliance with the License. You may obtain 
// a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
// License for the specific language governing permissions and limitations 
// under the License.
// Copied from: https://github.com/toptensoftware/RichTextKit

namespace Avalonia.Media.Text.Unicode
{
    /// <summary>
    /// Helper for looking up unicode character class information
    /// </summary>
    internal static class UnicodeClasses
    {
        static UnicodeClasses()
        {
            // Load trie resources
            s_bidiTrie = new UnicodeTrie(typeof(UnicodeClasses).Assembly.GetManifestResourceStream("Avalonia.Assets.BidiData.trie"));
            s_classesTrie = new UnicodeTrie(typeof(UnicodeClasses).Assembly.GetManifestResourceStream("Avalonia.Assets.LineBreakClasses.trie"));
        }

        private static readonly UnicodeTrie s_bidiTrie;
        private static readonly UnicodeTrie s_classesTrie;

        /// <summary>
        /// Get the directionality of a Unicode Code Point
        /// </summary>
        /// <param name="codePoint">The code point in question</param>
        /// <returns>The code point's directionality</returns>
        public static Directionality Directionality(int codePoint)
        {
            return (Directionality)(s_bidiTrie.Get(codePoint) >> 24);
        }

        /// <summary>
        /// Get the directionality of a Unicode Code Point
        /// </summary>
        /// <param name="codePoint">The code point in question</param>
        /// <returns>The code point's directionality</returns>
        public static uint BidiData(int codePoint)
        {
            return s_bidiTrie.Get(codePoint);
        }

        /// <summary>
        /// Get the bracket type for a Unicode Code Point
        /// </summary>
        /// <param name="codePoint">The code point in question</param>
        /// <returns>The code point's paired bracked type</returns>
        public static PairedBracketType PairedBracketType(int codePoint)
        {
            return (PairedBracketType)((s_bidiTrie.Get(codePoint) >> 16) & 0xFF);
        }

        /// <summary>
        /// Get the associated bracket type for a Unicode Code Point
        /// </summary>
        /// <param name="codePoint">The code point in question</param>
        /// <returns>The code point's opposite bracket, or 0 if not a bracket</returns>
        public static int AssociatedBracket(int codePoint)
        {
            return (int)(s_bidiTrie.Get(codePoint) & 0xFFFF);
        }

        /// <summary>
        /// Get the line break class for a Unicode Code Point
        /// </summary>
        /// <param name="codePoint">The code point in question</param>
        /// <returns>The code point's line break class</returns>
        public static LineBreakClass LineBreakClass(int codePoint)
        {
            return (LineBreakClass)s_classesTrie.Get(codePoint);
        }
    }
}
