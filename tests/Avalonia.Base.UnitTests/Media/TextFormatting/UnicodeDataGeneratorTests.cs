using System.IO;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    public class UnicodeDataGeneratorTests
    {
        /// <summary>
        ///     This test is used to generate all Unicode related types.
        ///     We only need to run this when the Unicode spec changes.
        /// </summary>
        [Fact(Skip = "Only run when we update the trie.")]
        public void Should_Generate_Data()
        {
            if (!Directory.Exists("Generated"))
            {
                Directory.CreateDirectory("Generated");
            }

            var unicodeDataTrie = UnicodeDataGenerator.GenerateUnicodeDataTrie(out var unicodeDataEntries, out var unicodeData);

            foreach (var value in unicodeData.Values)
            {
                var data = unicodeDataTrie.Get((uint)value.Codepoint);
                
                Assert.Equal(value.GeneralCategory, GetValue(data, 0, UnicodeData.CATEGORY_MASK));
                
                Assert.Equal(value.Script, GetValue(data, UnicodeData.SCRIPT_SHIFT, UnicodeData.SCRIPT_MASK));
                
                Assert.Equal(value.LineBreakClass, GetValue(data, UnicodeData.LINEBREAK_SHIFT, UnicodeData.LINEBREAK_MASK));
            }
            
            var biDiTrie = UnicodeDataGenerator.GenerateBiDiTrie(out var biDiDataEntries, out var biDiData);

            foreach (var value in biDiData.Values)
            {
                var data = biDiTrie.Get((uint)value.Codepoint);
                
                Assert.Equal(value.Bracket, GetValue(data, 0, UnicodeData.BIDIPAIREDBRACKED_MASK));
                
                Assert.Equal(value.BracketType, GetValue(data, UnicodeData.BIDIPAIREDBRACKEDTYPE_SHIFT, UnicodeData.BIDIPAIREDBRACKEDTYPE_MASK));
                
                Assert.Equal(value.BiDiClass, GetValue(data, UnicodeData.BIDICLASS_SHIFT, UnicodeData.BIDICLASS_MASK));
            }
            
            UnicodeEnumsGenerator.CreatePropertyValueAliasHelper(unicodeDataEntries, biDiDataEntries);
        }

        private static int GetValue(uint value, int shift, int mask)
        {
            return (int)((value >> shift) & mask);
        }
    }
}
