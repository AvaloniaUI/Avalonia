using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.Visuals.UnitTests.Media.TextFormatting
{
    public class BiDiClassTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public BiDiClassTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }
    
        [Fact(Skip = "Only run when the Unicode spec changes.")]
        public void Should_Resolve()
        {
            var generator = new BiDiClassTestDataGenerator();

            foreach (var testData in generator)
            {
                Assert.True(Run(testData));
            }
        }

        private bool Run(BiDiClassData t)
        {
            var bidi = new BidiAlgorithm();
            var bidiData = new BidiData { ParagraphEmbeddingLevel = t.ParagraphLevel };
        
            var text = Encoding.UTF32.GetString(MemoryMarshal.Cast<int, byte>(t.CodePoints).ToArray());

            // Append
            bidiData.Append(text);

            // Act
            for (int i = 0; i < 10; i++)
            {
                bidi.Process(bidiData);
            }

            var resultLevels = bidi.ResolvedLevels;
            var resultParagraphLevel = bidi.ResolvedParagraphEmbeddingLevel;

            // Assert
            var passed = true;

            if (t.ResolvedParagraphLevel != resultParagraphLevel)
            {
                return false;
            }

            for (var i = 0; i < t.ResolvedLevels.Length; i++)
            {
                if (t.ResolvedLevels[i] == -1)
                {
                    continue;
                }

                if (t.ResolvedLevels[i] != resultLevels[i])
                {
                    passed = false;
                    break;
                }
            }

            if (passed)
            {
                return true;
            }
        
            _outputHelper.WriteLine($"Failed line {t.LineNumber}");

            _outputHelper.WriteLine(
                $"             Code Points: {string.Join(" ", t.CodePoints.Select(x => x.ToString("X4")))}");

            _outputHelper.WriteLine(
                $"      Pair Bracket Types: {string.Join(" ", bidiData.PairedBracketTypes.Select(x => " " + x.ToString()))}");

            _outputHelper.WriteLine(
                $"     Pair Bracket Values: {string.Join(" ", bidiData.PairedBracketValues.Select(x => x.ToString("X4")))}");
            _outputHelper.WriteLine($"             Embed Level: {t.ParagraphLevel}");
            _outputHelper.WriteLine($"    Expected Embed Level: {t.ResolvedParagraphLevel}");
            _outputHelper.WriteLine($"      Actual Embed Level: {resultParagraphLevel}");
            _outputHelper.WriteLine($"          Directionality: {string.Join(" ", bidiData.Classes)}");
            _outputHelper.WriteLine($"         Expected Levels: {string.Join(" ", t.ResolvedLevels)}");
            _outputHelper.WriteLine($"           Actual Levels: {string.Join(" ", resultLevels)}");
        
            return false;
        }
    }
}
